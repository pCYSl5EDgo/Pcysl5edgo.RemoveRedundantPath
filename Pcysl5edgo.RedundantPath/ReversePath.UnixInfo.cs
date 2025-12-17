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

            segmentSpan[segmentCount - 1] = new(offset, length);
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

        public int Initialize()
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
                    hasLeadingCurrentSegment = true;
                }
                else
                {
                    Debug.Assert(mode == -2);
                    ++parentSegmentCount;
                }
            }

            return CalculateLength(segmentCharCount);
        }

        private int CalculateLength(int segmentCharCount)
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
                hasLeadingCurrentSegment = false;
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

            BitSpan.CalculateUpperBitWall(textSpan.Length - 1, out separatorWall);
            separatorWall |= (separator >>> 1);
            current = dot & ((separator << 1) | OneBit) & separatorWall;
            parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separator & (separatorWall | (separator << 1) | OneBit);
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
            int segmentCharCount = 0, textIndex = textSpan.Length - 1, batchCount = (textSpan.Length + BitMask) >>> BitShift, batchIndex = batchCount - 2;
#pragma warning disable IDE0018
            uint separatorCurrent, separatorPrev, dotCurrent, dotPrev, separatorWall, current, parent, separatorDuplicate;
#pragma warning restore IDE0018
            if ((textSpan.Length & BitMask) == default)
            {
                separatorCurrent = BitSpan.Get(textSpan[(batchIndex * BitCount + BitCount)..], out dotCurrent);
            }
            else
            {
                separatorCurrent = BitSpan.Get(textSpan[(batchIndex * BitCount + BitCount)..], out dotCurrent, textSpan.Length & BitMask);
            }

            separatorPrev = BitSpan.Get(textSpan[(batchIndex * BitCount)..], out dotPrev);
            BitSpan.CalculateUpperBitWall((textSpan.Length - 1) & BitMask, out separatorWall);
            separatorWall |= separatorCurrent >>> 1;
            current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | (endsWithSeparator ? OneBit << (textSpan.Length - 1) : default));
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                separatorCurrent = separatorPrev;
                dotCurrent = dotPrev;
                separatorPrev = BitSpan.Get(textSpan[(batchIndex * BitCount)..], out dotPrev);
                current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
                parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
                separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | separatorWall);
                continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, continueLength, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            }

            separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
            current = dotPrev & ((separatorPrev << 1) | OneBit) & separatorWall;
            parent = dotPrev & (dotPrev << 1) & ((separatorPrev << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separatorPrev & ((separatorPrev << 1) | OneBit | separatorWall);
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

            return segmentCharCount + AddOrUniteSegment(0, length, length + 2);
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, uint separator, uint separatorDuplicate, uint current, uint parent, int batchIndex)
        {
            const int BitCount = 32, BitMask = BitCount - 1;
            var loopLowerLimit = batchIndex * BitCount;
            var loopUpperLimit = loopLowerLimit + BitCount;
            int nextSeparatorIndex, length;
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
                    var temp = BitSpan.ZeroClearUpperBit(separator, loopUpperLimit - textIndex);
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
                        var diff = oldPair.Offset - textIndex - 1;
                        switch (diff)
                        {
                            case 0:
                            case 1:
                                oldPair = new(nextSeparatorIndex + 1, length += diff);
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
                    parentSegmentCount -= BitOperations.PopCount(BitSpan.ZeroClearUpperBit(separator, loopUpperLimit - textIndex));
                    if (parentSegmentCount >= 0)
                    {
                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(separator);
                    }
                    else
                    {
                        var tempSeparator = separator;
                        for (; parentSegmentCount <= 0; ++parentSegmentCount, tempSeparator &= tempSeparator - 1)
                        {
                        }

                        textIndex = loopLowerLimit + BitOperations.TrailingZeroCount(tempSeparator) - 1;
                    }
                }
            }

            var any = separator | parent | current;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(separator, textIndex))
                    {
                        var temp = BitSpan.ZeroClearUpperBit(~separator, loopUpperLimit - textIndex);
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
                    var temp = BitSpan.ZeroClearUpperBit(separator, loopUpperLimit - textIndex);
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

        public static void Create(Span<char> span, UnixInfo arg) => arg.Write(span);

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

        public readonly char this[int index] => this.textSpan[index];
#endif
        #endregion
    }
}
