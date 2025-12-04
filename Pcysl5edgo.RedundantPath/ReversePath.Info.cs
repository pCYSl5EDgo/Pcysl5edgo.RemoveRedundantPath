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
        private readonly ref ushort textRef;
        private ref (int Offset, int Length) segmentRef;
        private int segmentCapacity;
        private long[]? rentalArray;
        private int segmentCount;
        private readonly bool startsWithSeparator;
        private readonly bool endsWithSepartor;
        private int parentCount;
        private bool hasCurrent;
        public readonly bool IsSlashOnly => startsWithSeparator && segmentCount == 0;

        public UnixInfo(ref ushort textRef, Span<ValueTuple<int, int>> span, bool startsWithSeparator, bool endsWithSepartor)
        {
            this.textRef = ref textRef;
            segmentRef = ref MemoryMarshal.GetReference(span);
            segmentCapacity = span.Length;
            this.startsWithSeparator = startsWithSeparator;
            this.endsWithSepartor = endsWithSepartor;
            segmentCount = 0;
            parentCount = 0;
            hasCurrent = false;
        }

        private void AddSegment(int offset, int length)
        {
            if (segmentCount >= segmentCapacity)
            {
                if (rentalArray is null)
                {
                    rentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)(segmentCount + 1)));
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<ValueTuple<int, int>, byte>(ref segmentRef), ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(rentalArray)), (uint)(segmentCount * 8u));
                    segmentCapacity = rentalArray.Length;
                }
                else
                {
                    var tempRentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)(segmentCount + 1)));
                    Unsafe.CopyBlockUnaligned(ref Unsafe.As<ValueTuple<int, int>, byte>(ref segmentRef), ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(rentalArray)), (uint)(segmentCount * 8u));
                    ArrayPool<long>.Shared.Return(rentalArray);
                    rentalArray = tempRentalArray;
                    segmentCapacity = rentalArray.Length;
                }
            }

            Unsafe.Add(ref segmentRef, segmentCount++) = new(offset, length);
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

        public int Initialize(int textLength)
        {
            Debug.Assert(textLength >= 0);
            if (textLength < 16)
            {
                return InitializeEach(textLength);
            }
            else if (textLength <= 32)
            {
                return InitializeSimdLTE32(textLength);
            }
            else if (nuint.Size == sizeof(uint))
            {
                return InitializeSimdGT32(textLength);
            }
            else if (textLength <= 64)
            {
                return InitializeSimdLTE64(textLength);
            }
            else
            {
                return InitializeSimdGT64(textLength);
            }
        }

        public int InitializeEach(int textLength)
        {
            int mode = 0, segmentCharCount = 0;
            for (int textIndex = textLength - 1; textIndex >= 0; --textIndex)
            {
                var c = Unsafe.Add(ref textRef, textIndex);
                if (mode > 0)
                {
                    if (c != '/')
                    {
                        ++mode;
                        continue;
                    }

                    if (parentCount != 0)
                    {
                        --parentCount;
                    }
                    else
                    {
                        if (segmentCount == 0)
                        {
                            AddSegment(textIndex + 1, segmentCharCount = mode);
                        }
                        else
                        {
                            ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                            if (++textIndex + mode == oldPair.Offset)
                            {
                                oldPair.Offset = textIndex;
                                oldPair.Length += ++mode;
                                segmentCharCount += mode;
                            }
                            else
                            {
                                Unsafe.Add(ref segmentRef, segmentCount++) = new(textIndex, mode);
                                segmentCharCount += mode;
                            }
                        }

                        hasCurrent = false;
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
                        hasCurrent = parentCount == 0;
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
                        ++parentCount;
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
                if (parentCount > 0)
                {
                    --parentCount;
                }
                else
                {
                    hasCurrent = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(0, segmentCharCount = mode);
                    }
                    else
                    {
                        ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                        if (mode + 1 == oldPair.Offset)
                        {
                            oldPair.Offset = 0;
                            oldPair.Length += mode + 1;
                            segmentCharCount += mode + 1;
                        }
                        else
                        {
                            Unsafe.Add(ref segmentRef, segmentCount++) = new(0, mode);
                            segmentCharCount += mode;
                        }
                    }
                }
            }
            else if (!startsWithSeparator)
            {
                if (mode == -1)
                {
                    hasCurrent = true;
                }
                else
                {
                    Debug.Assert(mode == -2);
                    ++parentCount;
                }
            }

            return CalculateLength(segmentCharCount);
        }

        private int CalculateLength(int segmentCharCount)
        {
            Debug.Assert(segmentCount != 0 || segmentCharCount == 0);
            var sum = segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0);
            if (startsWithSeparator)
            {
                return segmentCount == 0 ? 1 : sum;
            }
            else if (parentCount == 0)
            {
                return sum + (hasCurrent ? 1 : -1);
            }
            else
            {
                hasCurrent = false;
                return sum + (3 * parentCount) - 1;
            }
        }

        private int InitializeSimdLTE32(int textLength)
        {
            var sep = BitSpan.Get(ref textRef, out uint dot, textLength);
            var sepWall = ((sep >>> 1) | BitSpan.CalculateUpperBitWall32(textLength - 1));
            var current = dot & ((sep << 1) | 1u) & sepWall;
            var parent = dot & (dot << 1) & ((sep << 2) | 2u) & sepWall;
            var sepDup = sep & (sepWall | (sep << 1) | 1u);
            if ((current | parent | sepDup) == 0)
            {
                return textLength + 1 + (endsWithSepartor ? 1 : 0);
            }

            int segmentCharCount = 0;
            var textIndex = textLength - 1;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, sep, sepDup, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdLTE64(int textLength)
        {
            const ulong One = 1ul;
            var sep = BitSpan.Get(ref textRef, out ulong dot, textLength);
            var sepWall = ((sep >>> 1) | BitSpan.CalculateUpperBitWall64(textLength - 1));
            var current = dot & ((sep << 1) | One) & sepWall;
            var parent = dot & (dot << 1) & ((sep << 2) | (One << 1)) & sepWall;
            var sepDup = sep & (sepWall | (sep << 1) | 1ul);
            if ((current | parent | sepDup) == 0)
            {
                return textLength + 1 + (endsWithSepartor ? 1 : 0);
            }

            int segmentCharCount = 0;
            var textIndex = textLength - 1;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, sep, sepDup, current, parent, 0);
            if (continueLength > 0)
            {
                segmentCharCount = ProcessLastContinuation(segmentCharCount, continueLength);
            }

            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdGT32(int textLength)
        {
            const int BitShift = 5;
            const int BitCount = 1 << BitShift;
            Debug.Assert(textLength >= BitCount + 1);
            const int BitMask = BitCount - 1;
            const uint OneBit = 1u;
            int segmentCharCount = 0, textIndex = textLength - 1, batchCount = (textLength + BitMask) >>> BitShift, batchIndex = batchCount - 2;
            var separatorCurrent = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount + BitCount), out uint dotCurrent, textLength & BitMask);
            var separatorPrev = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount), out uint dotPrev);
            var separatorWall = BitSpan.CalculateUpperBitWall32((textLength - 1) & BitMask) | (separatorCurrent >>> 1);
            var current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            var parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            var separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | (endsWithSepartor ? OneBit << (textLength - 1) : default));
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                separatorCurrent = separatorPrev;
                dotCurrent = dotPrev;
                separatorPrev = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount), out dotPrev);
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

        private int InitializeSimdGT64(int textLength)
        {
            const int BitShift = 6;
            const int BitCount = 1 << BitShift;
            Debug.Assert(textLength >= BitCount + 1);
            const int BitMask = BitCount - 1;
            const ulong OneBit = 1ul;
            int segmentCharCount = 0, textIndex = textLength - 1, batchCount = (textLength + BitMask) >>> BitShift, batchIndex = batchCount - 2;
            var separatorCurrent = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount + BitCount), out ulong dotCurrent, textLength & BitMask);
            var separatorPrev = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount), out ulong dotPrev);
            var separatorWall = BitSpan.CalculateUpperBitWall64((textLength - 1) & BitMask) | (separatorCurrent >>> 1);
            var current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            var parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            var separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | (endsWithSepartor ? OneBit << (textLength - 1) : default));
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, 0, separatorCurrent, separatorDuplicate, current, parent, batchIndex + 1);
            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                separatorCurrent = separatorPrev;
                dotCurrent = dotPrev;
                separatorPrev = BitSpan.Get(ref Unsafe.Add(ref textRef, batchIndex * BitCount), out dotPrev);
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
            if (parentCount != 0)
            {
                --parentCount;
                return segmentCharCount;
            }

            hasCurrent = false;
            if (segmentCount == 0)
            {
                AddSegment(0, length);
                return length;
            }

            ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
            if (length + 2 == oldPair.Offset)
            {
                oldPair.Offset = 0;
                oldPair.Length += ++length;
            }
            else
            {
                AddSegment(0, length);
            }

            return segmentCharCount + length;
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, uint separator, uint separatorDuplicate, uint current, uint parent, int batchIndex)
        {
            const int BitCount = 32;
            var loopLowerLimit = batchIndex * BitCount;
            var textIndexOffset = loopLowerLimit + BitCount;
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
                    var temp = BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex);
                    nextSeparatorIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex + continueLength;
                }

                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    return length;
                }
                else if (parentCount != 0)
                {
                    --parentCount;
                }
                else
                {
                    hasCurrent = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                        var diff = oldPair.Offset - textIndex - 1;
                        switch (diff)
                        {
                            case 0:
                            case 1:
                                length += diff;
                                oldPair = new(nextSeparatorIndex + 1, length);
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

            if (parentCount == 0 && (parent | current | separatorDuplicate) == 0)
            {
                continueLength = (textIndex & (BitCount - 1)) + 1;
                textIndex = loopLowerLimit - 1;
                return continueLength;
            }

            var any = separator | parent | current;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(separator, textIndex))
                    {
                        var temp = BitSpan.ZeroClearUpperBit(~separator, textIndexOffset - textIndex);
                        textIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        hasCurrent = parentCount == 0;
                        textIndex -= 2;
                    }

                    continue;
                }

                {
                    var temp = BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex);
                    nextSeparatorIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex;
                }
                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    return length;
                }
                else if (parentCount != 0)
                {
                    --parentCount;
                }
                else
                {
                    hasCurrent = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                        if (textIndex + 2 == oldPair.Offset)
                        {
                            oldPair.Offset = nextSeparatorIndex + 1;
                            oldPair.Length += ++length;
                        }
                        else
                        {
                            AddSegment(nextSeparatorIndex + 1, length);
                        }

                        segmentCharCount += length;
                    }
                }

                textIndex = nextSeparatorIndex - 1;
            }
            while (textIndex >= loopLowerLimit);
            return 0;
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, int continueLength, ulong separator, ulong separatorDuplicate, ulong current, ulong parent, int batchIndex)
        {
            const int BitCount = 64;
            var loopLowerLimit = batchIndex * BitCount;
            var textIndexOffset = loopLowerLimit + BitCount;
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
                    var temp = BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex);
                    nextSeparatorIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex + continueLength;
                }

                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    return length;
                }
                else if (parentCount != 0)
                {
                    --parentCount;
                }
                else
                {
                    hasCurrent = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                        switch (oldPair.Offset - textIndex - 1)
                        {
                            case 0:
                                oldPair = new(nextSeparatorIndex + 1, length);
                                break;
                            case 1:
                                oldPair = new(nextSeparatorIndex + 1, ++length);
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

            if (parentCount == 0 && (parent | current | separatorDuplicate) == 0)
            {
                continueLength = (textIndex & (BitCount - 1)) + 1;
                textIndex = loopLowerLimit - 1;
                return continueLength;
            }

            var any = separator | parent | current;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(separator, textIndex))
                    {
                        var temp = BitSpan.ZeroClearUpperBit(~separator, textIndexOffset - textIndex);
                        textIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        hasCurrent = parentCount == 0;
                        textIndex -= 2;
                    }

                    continue;
                }

                {
                    var temp = BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex);
                    nextSeparatorIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                    length = textIndex - nextSeparatorIndex;
                }
                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    return length;
                }
                else if (parentCount != 0)
                {
                    --parentCount;
                }
                else
                {
                    hasCurrent = false;
                    if (segmentCount == 0)
                    {
                        AddSegment(nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref Unsafe.Add(ref segmentRef, segmentCount - 1);
                        if (textIndex + 2 == oldPair.Offset)
                        {
                            oldPair.Offset = nextSeparatorIndex + 1;
                            oldPair.Length += ++length;
                        }
                        else
                        {
                            AddSegment(nextSeparatorIndex + 1, length);
                        }

                        segmentCharCount += length;
                    }
                }

                textIndex = nextSeparatorIndex - 1;
            }
            while (textIndex >= loopLowerLimit);
            return 0;
        }

        #region Write
        public readonly void Write(ref char destination)
        {
            if (startsWithSeparator)
            {
                if (segmentCount == 0)
                {
                    destination = '/';
                    return;
                }

                destination = ref WriteSegmentsWithStartingSeparator(ref destination);
            }
            else if (parentCount != 0)
            {
                destination = ref WriteParentSegments(ref destination);
                if (segmentCount != 0)
                {
                    destination = ref WriteSegmentsWithStartingSeparator(ref destination);
                }
            }
            else if (hasCurrent)
            {
                destination = '.';
                destination = ref Unsafe.Add(ref destination, 1);
                if (segmentCount != 0)
                {
                    destination = ref WriteSegmentsWithStartingSeparator(ref destination);
                }
            }
            else if (segmentCount != 0)
            {
                destination = ref WriteSegmentsWithoutStartingSeparator(ref destination);
            }

            if (endsWithSepartor)
            {
                destination = '/';
            }
        }

        public static void Create(Span<char> span, UnixInfo arg) => arg.Write(ref MemoryMarshal.GetReference(span));

        private readonly ref char WriteParentSegments(ref char destination)
        {
            Unsafe.Add(ref destination, 1) = destination = '.';
            for (int i = parentCount - 2, offset = 2; i >= 0; --i, offset += 3)
            {
                Unsafe.Add(ref destination, offset) = '/';
                Unsafe.Add(ref destination, offset + 1) = '.';
                Unsafe.Add(ref destination, offset + 2) = '.';
            }

            return ref Unsafe.Add(ref destination, parentCount * 3 - 1);
        }

        private readonly ref char WriteSegmentsWithStartingSeparator(ref char destination)
        {
            for (int segmentIndex = segmentCount - 1; segmentIndex >= 0; --segmentIndex)
            {
                destination = '/';
                var (Offset, Length) = Unsafe.Add(ref segmentRef, segmentIndex);
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref destination, 1)), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, Offset)), (uint)(Length << 1));
                destination = ref Unsafe.Add(ref destination, Length + 1);
            }

            return ref destination;
        }

        private readonly ref char WriteSegmentsWithoutStartingSeparator(ref char destination)
        {
            int segmentIndex = segmentCount - 1;
            var pair = Unsafe.Add(ref segmentRef, segmentIndex);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref destination), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, pair.Offset)), (uint)(pair.Length << 1));
            destination = ref Unsafe.Add(ref destination, pair.Length);
            for (segmentIndex = segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                destination = '/';
                pair = Unsafe.Add(ref segmentRef, segmentIndex);
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref destination, 1)), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, pair.Offset)), (uint)(pair.Length << 1));
                destination = ref Unsafe.Add(ref destination, pair.Length + 1);
            }

            return ref destination;
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
            for (int i = segmentCount - 1; ; i--)
            {
                var (Offset, Length) = Unsafe.Add(ref segmentRef, i);
                handler.AppendFormatted(MemoryMarshal.Cast<ushort, char>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref textRef, Offset), Length)));
                if (i == 0)
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

        public readonly string CalculateOriginalText(int textLength)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<ushort, char>(ref textRef), textLength).ToString();
        }
#endif
        #endregion
    }
}
