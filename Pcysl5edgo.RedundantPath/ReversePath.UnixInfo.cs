using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    private ref struct UnixInfo : IDisposable
    {
        private readonly ReadOnlySpan<char> textSpan;
        private Span<(int Offset, int Length)> segmentSpan;
        private long[]? rentalArray;
        private int segmentCount;
        private readonly bool startsWithSeparator;
        private readonly bool endsWithSeparator;
        private int parentSegmentCount;
        private bool hasLeadingCurrentSegment;
        private readonly ref (int Offset, int Length) LastSegment => ref segmentSpan[segmentCount - 1];
        public readonly bool IsSlashOnly => startsWithSeparator && segmentCount == 0;

        public UnixInfo(ReadOnlySpan<char> textSpan, Span<ValueTuple<int, int>> segmentSpan, bool startsWithSeparator, bool endsWithSepartor)
        {
            this.textSpan = textSpan;
            this.segmentSpan = segmentSpan;
            this.startsWithSeparator = startsWithSeparator;
            endsWithSeparator = endsWithSepartor;
            segmentCount = 0;
            parentSegmentCount = 0;
            hasLeadingCurrentSegment = false;
        }

        private void AddSegment(int offset, int length)
        {
            if (++segmentCount > segmentSpan.Length)
            {
                EnsureStackCapacity();
            }

            segmentSpan[segmentCount - 1] = new(offset, length);
        }

        private void EnsureStackCapacity()
        {
            if (rentalArray is null)
            {
                rentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
                var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(rentalArray.AsSpan());
                segmentSpan.CopyTo(temp);
                segmentSpan = temp;
            }
            else
            {
                var tempRentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
                var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(tempRentalArray.AsSpan());
                segmentSpan.CopyTo(temp);
                ArrayPool<long>.Shared.Return(rentalArray);
                rentalArray = tempRentalArray;
                segmentSpan = temp;
            }
        }

        private int AddOrUniteSegment(int offset, int length, int expectedOffset)
        {
            hasLeadingCurrentSegment = false;
            if (segmentCount > 0)
            {
                ref var last = ref segmentSpan[segmentCount - 1];
                if (last.Offset == expectedOffset)
                {
                    last.Offset = offset;
                    last.Length += ++length;
                    return length;
                }
            }

            AddSegment(offset, length);
            return length;
        }

        public void Dispose()
        {
            if (rentalArray is not null)
            {
                ArrayPool<long>.Shared.Return(rentalArray);
                rentalArray = default;
            }
        }

        // a/./a
        public static int CalculateMaxSegmentCount(int charCount) => (charCount + 3) >>> 2;

        public int Initialize32()
        {
            if (textSpan.IsEmpty)
            {
                return CalculateLength(0);
            }
            else if (textSpan.Length < 16)
            {
                return InitializeEach();
            }
            else if (textSpan.Length <= 32)
            {
                return InitializeSimdLTE32();
            }
            else
            {
                return InitializeSimdGT32();
            }
        }

        public int Initialize64()
        {
            if (textSpan.IsEmpty)
            {
                return CalculateLength(0);
            }
            else if (textSpan.Length < 16)
            {
                return InitializeEach();
            }
            else if (textSpan.Length <= 64)
            {
                return InitializeSimdLTE64();
            }
            else
            {
                return InitializeSimdGT64();
            }
        }

        public int InitializeEach()
        {
            int mode = 0, segmentCharCount = 0;
            for (int textIndex = textSpan.Length - 1; textIndex >= 0; --textIndex)
            {
                var c = textSpan[textIndex];
                if (mode > 0)
                {
                    if (c != '/')
                    {
                        ++mode;
                        continue;
                    }

                    if (parentSegmentCount > 0)
                    {
                        --parentSegmentCount;
                    }
                    else
                    {
                        segmentCharCount += AddOrUniteSegment(textIndex + 1, mode, textIndex + 1 + mode);
                    }

                    mode = 0;
                }
                else if (mode == 0)
                {
                    if (c != '/')
                    {
                        mode = c == '.' ? -1 : 1;
                    }
                }
                else if (mode == -1)
                {
                    if (c == '/')
                    {
                        hasLeadingCurrentSegment = parentSegmentCount == 0;
                        mode = 0;
                    }
                    else
                    {
                        mode = c == '.' ? -2 : 2;
                    }
                }
                else
                {
                    Debug.Assert(mode == -2);
                    if (c == '/')
                    {
                        ++parentSegmentCount;
                        mode = 0;
                    }
                    else
                    {
                        mode = 3;
                    }
                }
            }

            if (mode > 0)
            {
                if (parentSegmentCount > 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    segmentCharCount += AddOrUniteSegment(0, mode, mode + 1);
                }
            }
            else if (!startsWithSeparator)
            {
                if (mode == -1)
                {
                    hasLeadingCurrentSegment = parentSegmentCount == 0;
                }
                else
                {
                    Debug.Assert(mode == -2);
                    ++parentSegmentCount;
                }
            }

            return CalculateLength(segmentCharCount);
        }

        private readonly int CalculateLength(int segmentCharCount)
        {
            Debug.Assert(segmentCount != 0 || segmentCharCount == 0);
            var sum = segmentCount + segmentCharCount + (endsWithSeparator ? 1 : 0);
            if (startsWithSeparator)
            {
                return segmentCount == 0 ? 1 : sum;
            }
            else if (parentSegmentCount == 0)
            {
                return sum + (hasLeadingCurrentSegment ? 1 : -1);
            }
            else
            {
                return sum + (3 * parentSegmentCount) - 1;
            }
        }

        private int InitializeSimdLTE32()
        {
            const uint OneBit = 1u;
#pragma warning disable IDE0018
            uint separator, dot, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
            if (textSpan.Length == 32)
            {
                separator = BitSpan.Get(textSpan, out dot);
            }
            else
            {
                separator = BitSpan.Get(textSpan, out dot, textSpan.Length);
            }

            separatorWall = BitSpan.CalculateSeparatorWall(separator, textSpan.Length - 1);
            current = dot & ((separator << 1) | OneBit) & separatorWall;
            parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separator & separatorWall;
            if ((current | parent | separatorDuplicate) == 0)
            {
                return textSpan.Length + (startsWithSeparator ? 1 : 0) + (endsWithSeparator ? 1 : 0);
            }

            int segmentCharCount = 0;
            var textIndex = textSpan.Length - 1;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separator, separatorDuplicate, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdGT32()
        {
            const int BitShift = 5;
            const int BitCount = 1 << BitShift;
            Debug.Assert(textSpan.Length >= BitCount + 1);
            const int BitMask = BitCount - 1;
            const uint OneBit = 1u;
#pragma warning disable IDE0018
            uint separatorCurrent, separatorPrev, dotCurrent, dotPrev, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
            int segmentCharCount = 0, textIndex = textSpan.Length - 1, batchCount = (textSpan.Length + BitMask) >>> BitShift, batchIndex = batchCount - 2;
            if ((textSpan.Length & BitMask) == default)
            {
                separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent);
            }
            else
            {
                separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent, textSpan.Length & BitMask);
            }

            separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
            separatorWall = BitSpan.CalculateSeparatorWall(separatorCurrent, textSpan.Length - 1);
            current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            separatorDuplicate = separatorCurrent & separatorWall;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                separatorCurrent = separatorPrev;
                dotCurrent = dotPrev;
                separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
                current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
                parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
                separatorDuplicate = separatorCurrent & separatorWall;
                continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            }

            separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
            current = dotPrev & ((separatorPrev << 1) | OneBit) & separatorWall;
            parent = dotPrev & (dotPrev << 1) & ((separatorPrev << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separatorPrev & separatorWall;
            continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorPrev, separatorDuplicate, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdLTE64()
        {
            const ulong OneBit = 1ul;
#pragma warning disable IDE0018
            ulong separator, dot, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
            if (textSpan.Length == 32)
            {
                separator = BitSpan.Get(textSpan, out dot);
            }
            else
            {
                separator = BitSpan.Get(textSpan, out dot, textSpan.Length);
            }

            separatorWall = BitSpan.CalculateSeparatorWall(separator, textSpan.Length - 1);
            current = dot & ((separator << 1) | OneBit) & separatorWall;
            parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separator & separatorWall;
            if ((current | parent | separatorDuplicate) == 0)
            {
                return textSpan.Length + (startsWithSeparator ? 1 : 0) + (endsWithSeparator ? 1 : 0);
            }

            int segmentCharCount = 0;
            var textIndex = textSpan.Length - 1;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separator, separatorDuplicate, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdGT64()
        {
            const int BitShift = 6;
            const int BitCount = 1 << BitShift;
            Debug.Assert(textSpan.Length >= BitCount + 1);
            const int BitMask = BitCount - 1;
            const ulong OneBit = 1ul;
#pragma warning disable IDE0018
            ulong separatorCurrent, separatorPrev, dotCurrent, dotPrev, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
            int segmentCharCount = 0, textIndex = textSpan.Length - 1, batchCount = (textSpan.Length + BitMask) >>> BitShift, batchIndex = batchCount - 2;
            if ((textSpan.Length & BitMask) == default)
            {
                separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent);
            }
            else
            {
                separatorCurrent = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount + BitCount)), out dotCurrent, textSpan.Length & BitMask);
            }

            separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
            separatorWall = BitSpan.CalculateSeparatorWall(separatorCurrent, textSpan.Length - 1);
            current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            separatorDuplicate = separatorCurrent & separatorWall;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                separatorCurrent = separatorPrev;
                dotCurrent = dotPrev;
                separatorPrev = BitSpan.Get(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), batchIndex * BitCount)), out dotPrev);
                current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
                parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
                separatorDuplicate = separatorCurrent & separatorWall;
                continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            }

            separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
            current = dotPrev & ((separatorPrev << 1) | OneBit) & separatorWall;
            parent = dotPrev & (dotPrev << 1) & ((separatorPrev << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separatorPrev & separatorWall;
            continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorPrev, separatorDuplicate, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int ProcessLastContinuation(int segmentCharCount, int length)
        {
            if (parentSegmentCount > 0)
            {
                --parentSegmentCount;
                return segmentCharCount;
            }

            return segmentCharCount + AddOrUniteSegment(0, length, length + 1);
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, uint separator, uint separatorDuplicate, uint current, uint parent, int batchIndex)
        {
            const int BitCount = 32, BitMask = BitCount - 1;
            var loopLowerLimit = batchIndex * BitCount;
            var loopUpperLimit = loopLowerLimit + BitCount;
            int nextSeparatorIndex, length;
            #region ContinueLength > 0
            if (continueLength > 0)
            {
                if (BitSpan.GetBit(separator, textIndex))
                {
                    nextSeparatorIndex = textIndex;
                    length = continueLength;
                }
                else
                {
                    Debug.Assert(!BitSpan.GetBit(parent, textIndex) && !BitSpan.GetBit(current, textIndex));
                    var temp = BitSpan.ZeroHighBits(separator, textIndex);
                    nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex + continueLength;
                }

                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    Debug.Assert(length >= 0);
                    return length;
                }
                else if (parentSegmentCount > 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    hasLeadingCurrentSegment = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref LastSegment;
                        var diff = oldPair.Offset - nextSeparatorIndex - length - 1;
                        switch (diff)
                        {
                            case 0:
                            case 1:
                                oldPair.Offset = nextSeparatorIndex + 1;
                                oldPair.Length += (length += diff);
                                break;
                            default:
                                AddSegment(nextSeparatorIndex + 1, length);
                                break;
                        }

                        segmentCharCount += length;
                    }
                }

                textIndex = nextSeparatorIndex - 1;
                if (textIndex < loopLowerLimit)
                {
                    return 0;
                }
            }
            else
            {
                Debug.Assert(continueLength == 0, $"{nameof(continueLength)}: {continueLength}");
            }
            #endregion

            if ((textIndex & BitMask) != BitMask)
            {
                var clearStartIndex = textIndex + 1;
                separator = BitSpan.ZeroHighBits(separator, clearStartIndex);
                separatorDuplicate = BitSpan.ZeroHighBits(separatorDuplicate, clearStartIndex);
                parent = BitSpan.ZeroHighBits(parent, clearStartIndex);
                current = BitSpan.ZeroHighBits(current, clearStartIndex);
            }

            if ((parent | current | separatorDuplicate) == 0)
            {
                if (parentSegmentCount == 0 || separator == 0)
                {
                    continueLength = (textIndex & BitMask) + 1;
                    textIndex = loopLowerLimit - 1;
                    Debug.Assert(continueLength >= 0);
                    return continueLength;
                }
                else
                {
                    parentSegmentCount -= BitOperations.PopCount(BitSpan.ZeroHighBits(separator, textIndex));
                    if (parentSegmentCount >= 0)
                    {
                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(separator);
                    }
                    else
                    {
                        var tempSeparator = separator;
                        for (; parentSegmentCount < 0; ++parentSegmentCount, tempSeparator = BitSpan.ResetLowestSetBit(tempSeparator))
                        {
                        }

                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(tempSeparator);
                    }
                }
            }

            do
            {
                if (BitSpan.GetBit(separator | parent | current, textIndex))
                {
                    if (BitSpan.GetBit(separator, textIndex))
                    {
                        var temp = BitSpan.ZeroHighBits(~separator, textIndex);
                        textIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentSegmentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        hasLeadingCurrentSegment = parentSegmentCount == 0;
                        textIndex -= 2;
                    }

                    continue;
                }

                {
                    var temp = BitSpan.ZeroHighBits(separator, textIndex);
                    nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex;
                }
                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    Debug.Assert(length >= 0, $"{nameof(length)}: {length} {nameof(textIndex)}: {textIndex}");
                    return length;
                }
                else if (parentSegmentCount > 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    segmentCharCount += AddOrUniteSegment(nextSeparatorIndex + 1, length, textIndex + 2);
                }

                textIndex = nextSeparatorIndex - 1;
            }
            while (textIndex >= loopLowerLimit);
            return 0;
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, ulong separator, ulong separatorDuplicate, ulong current, ulong parent, int batchIndex)
        {
            const int BitCount = 64, BitMask = BitCount - 1;
            var loopLowerLimit = batchIndex * BitCount;
            var loopUpperLimit = loopLowerLimit + BitCount;
            int nextSeparatorIndex, length;
            #region ContinueLength > 0
            if (continueLength > 0)
            {
                if (BitSpan.GetBit(separator, textIndex))
                {
                    nextSeparatorIndex = textIndex;
                    length = continueLength;
                }
                else
                {
                    Debug.Assert(!BitSpan.GetBit(parent, textIndex) && !BitSpan.GetBit(current, textIndex));
                    var temp = BitSpan.ZeroHighBits(separator, textIndex);
                    nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex + continueLength;
                }

                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    Debug.Assert(length >= 0);
                    return length;
                }
                else if (parentSegmentCount > 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    hasLeadingCurrentSegment = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref LastSegment;
                        var diff = oldPair.Offset - nextSeparatorIndex - length - 1;
                        switch (diff)
                        {
                            case 0:
                            case 1:
                                oldPair.Offset = nextSeparatorIndex + 1;
                                oldPair.Length += (length += diff);
                                break;
                            default:
                                AddSegment(nextSeparatorIndex + 1, length);
                                break;
                        }

                        segmentCharCount += length;
                    }
                }

                textIndex = nextSeparatorIndex - 1;
                if (textIndex < loopLowerLimit)
                {
                    return 0;
                }
            }
            else
            {
                Debug.Assert(continueLength == 0, $"{nameof(continueLength)}: {continueLength}");
            }
            #endregion

            if ((textIndex & BitMask) != BitMask)
            {
                var clearStartIndex = textIndex + 1;
                separator = BitSpan.ZeroHighBits(separator, clearStartIndex);
                separatorDuplicate = BitSpan.ZeroHighBits(separatorDuplicate, clearStartIndex);
                parent = BitSpan.ZeroHighBits(parent, clearStartIndex);
                current = BitSpan.ZeroHighBits(current, clearStartIndex);
            }

            if ((parent | current | separatorDuplicate) == 0)
            {
                if (parentSegmentCount == 0 || separator == 0)
                {
                    continueLength = (textIndex & BitMask) + 1;
                    textIndex = loopLowerLimit - 1;
                    Debug.Assert(continueLength >= 0);
                    return continueLength;
                }
                else
                {
                    parentSegmentCount -= BitOperations.PopCount(BitSpan.ZeroHighBits(separator, textIndex));
                    if (parentSegmentCount >= 0)
                    {
                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(separator);
                    }
                    else
                    {
                        var tempSeparator = separator;
                        for (; parentSegmentCount < 0; ++parentSegmentCount, tempSeparator = BitSpan.ResetLowestSetBit(tempSeparator))
                        {
                        }

                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(tempSeparator);
                    }
                }
            }

            do
            {
                if (BitSpan.GetBit(separator | parent | current, textIndex))
                {
                    if (BitSpan.GetBit(separator, textIndex))
                    {
                        var temp = BitSpan.ZeroHighBits(~separator, textIndex);
                        textIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentSegmentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        hasLeadingCurrentSegment = parentSegmentCount == 0;
                        textIndex -= 2;
                    }

                    continue;
                }

                {
                    var temp = BitSpan.ZeroHighBits(separator, textIndex);
                    nextSeparatorIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex;
                }
                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    Debug.Assert(length >= 0, $"{nameof(length)}: {length} {nameof(textIndex)}: {textIndex}");
                    return length;
                }
                else if (parentSegmentCount > 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    segmentCharCount += AddOrUniteSegment(nextSeparatorIndex + 1, length, textIndex + 2);
                }

                textIndex = nextSeparatorIndex - 1;
            }
            while (textIndex >= loopLowerLimit);
            return 0;
        }

        #region Write
        public readonly void Write(ref char destination)
        {
            nuint destinationOffset = default;
            if (startsWithSeparator)
            {
                if (segmentCount == 0)
                {
                    destination = '/';
                    return;
                }

                destinationOffset = WriteSegmentsWithStartingSeparator(ref destination);
            }
            else if (parentSegmentCount > 0)
            {
                destinationOffset = WriteParentSegments(ref destination);
                if (segmentCount != 0)
                {
                    destinationOffset += WriteSegmentsWithStartingSeparator(ref Unsafe.Add(ref destination, destinationOffset));
                }
            }
            else if (hasLeadingCurrentSegment)
            {
                destination = '.';
                if (segmentCount == 0)
                {
                    destinationOffset = 1;
                }
                else
                {
                    destinationOffset = WriteSegmentsWithStartingSeparator(ref Unsafe.Add(ref destination, 1)) + 1;
                }
            }
            else if (segmentCount != 0)
            {
                destinationOffset = WriteSegmentsWithoutStartingSeparator(ref destination);
            }

            if (endsWithSeparator)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
            }
        }

        public readonly void Write(Span<char> destination)
        {
            if (startsWithSeparator)
            {
                if (segmentCount == 0)
                {
                    destination[0] = '/';
                    return;
                }

                destination = WriteSegmentsWithStartingSeparator(destination);
            }
            else if (parentSegmentCount > 0)
            {
                destination = WriteParentSegments(destination);
                if (segmentCount != 0)
                {
                    destination = WriteSegmentsWithStartingSeparator(destination);
                }
            }
            else if (hasLeadingCurrentSegment)
            {
                destination[0] = '.';
                if (segmentCount == 0)
                {
                    destination = destination[1..];
                }
                else
                {
                    destination = WriteSegmentsWithStartingSeparator(destination[1..]);
                }
            }
            else if (segmentCount != 0)
            {
                destination = WriteSegmentsWithoutStartingSeparator(destination);
            }

            if (endsWithSeparator)
            {
                destination[0] = '/';
                destination = destination[1..];
            }

            Debug.Assert(destination.IsEmpty);
        }

        public static void Create(Span<char> span, UnixInfo arg) => arg.Write(ref MemoryMarshal.GetReference(span));

        private readonly nuint WriteParentSegments(ref char destination)
        {
            Unsafe.Add(ref destination, 1) = destination = '.';
            for (int i = parentSegmentCount - 2, offset = 2; i >= 0; --i, offset += 3)
            {
                Unsafe.Add(ref destination, offset + 2) = '.';
                Unsafe.Add(ref destination, offset + 1) = '.';
                Unsafe.Add(ref destination, offset) = '/';
            }

            return (nuint)(parentSegmentCount * 3 - 1);
        }

        private readonly Span<char> WriteParentSegments(Span<char> destination)
        {
            destination[1] = destination[0] = '.';
            for (int i = parentSegmentCount - 2, offset = 2; i >= 0; --i, offset += 3)
            {
                destination[offset + 2] = '.';
                destination[offset + 1] = '.';
                destination[offset] = '/';
            }

            return destination[(parentSegmentCount * 3 - 1)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint Copy(ref char destination, ref char source, ref (int Offset, int Length) segment)
        {
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref destination), ref Unsafe.As<char, byte>(ref Unsafe.Add(ref source, segment.Offset)), (uint)segment.Length << 1);
            return (nuint)segment.Length;
        }

        private readonly nuint WriteSegmentsWithStartingSeparator(ref char destination)
        {
            nuint destinationOffset = 0;
            ref var segmentRef = ref MemoryMarshal.GetReference(segmentSpan);
            ref var sourceRef = ref MemoryMarshal.GetReference(textSpan);
            for (int segmentIndex = segmentCount - 1; segmentIndex >= 0; --segmentIndex)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
                destinationOffset += Copy(ref Unsafe.Add(ref destination, destinationOffset + 1), ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex)) + 1;
            }

            return destinationOffset;
        }

        private readonly Span<char> WriteSegmentsWithStartingSeparator(Span<char> destination)
        {
            for (int segmentIndex = segmentCount - 1; segmentIndex >= 0; --segmentIndex)
            {
                destination[0] = '/';
                var (offset, length) = segmentSpan[segmentIndex];
                textSpan.Slice(offset, length).CopyTo(destination[1..]);
                destination = destination[(length + 1)..];
            }

            return destination;
        }

        private readonly nuint WriteSegmentsWithoutStartingSeparator(ref char destination)
        {
            ref var sourceRef = ref MemoryMarshal.GetReference(textSpan);
            ref var segmentRef = ref MemoryMarshal.GetReference(segmentSpan);
            int segmentIndex = segmentCount - 1;
            var destinationOffset = Copy(ref destination, ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex));
            for (segmentIndex = segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
                destinationOffset += Copy(ref Unsafe.Add(ref destination, destinationOffset + 1), ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex)) + 1;
            }

            return destinationOffset;
        }

        private readonly Span<char> WriteSegmentsWithoutStartingSeparator(Span<char> destination)
        {
            int segmentIndex = segmentCount - 1;
            var (offset, length) = segmentSpan[segmentIndex];
            textSpan.Slice(offset, length).CopyTo(destination);
            destination = destination[length..];
            for (segmentIndex = segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                destination[0] = '/';
                (offset, length) = segmentSpan[segmentIndex];
                textSpan.Slice(offset, length).CopyTo(destination[1..]);
                destination = destination[(length + 1)..];
            }

            return destination;
        }
        #endregion

        #region Debug
#if DEBUG
        public override readonly string ToString()
        {
            if (segmentCount == 0)
            {
                return "";
            }

            DefaultInterpolatedStringHandler handler = $"";
            for (int segmentIndex = segmentCount - 1; ; segmentIndex--)
            {
                var (offset, length) = segmentSpan[segmentIndex];
                handler.AppendFormatted(textSpan.Slice(offset, length));
                if (segmentIndex == 0)
                {
                    break;
                }
                else
                {
                    handler.AppendFormatted('\n');
                }
            }

            return handler.ToString();
        }

        public readonly char this[int index] => textSpan[index];
#endif
        #endregion
    }
}
