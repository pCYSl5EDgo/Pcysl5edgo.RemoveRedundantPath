using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsUnixAllocOnce(string? path)
    {
        if (path is null)
        {
            return "";
        }
        else if (path.Length <= 1)
        {
            return path;
        }
        else if (path.Length == 2)
        {
            return path[0] == '/' && (path[1] == '/' || path[1] == '.') ? "/" : path;
        }

        var span = path.AsSpan();
        span = span.TrimStart('/');
        bool startsWithSeparator = span.Length != path.Length, endsWithSeparator;
        {
            var oldLength = span.Length;
            span = span.TrimEnd('/');
            endsWithSeparator = span.Length != oldLength;
        }

        if (span.IsEmpty)
        {
            if (startsWithSeparator)
            {
                return "/";
            }
            else
            {
                return "";
            }
        }
        else
        {
            var info = new UnixInfoAllocOnce(span, startsWithSeparator, endsWithSeparator);
            if (span.Length <= 64)
            {
                return info.ToStringLTE64(path);
            }
            else
            {
                return info.ToStringGT1024(path);
            }
        }
    }

    private ref struct UnixInfoAllocOncePair
    {
        public UnixInfoAllocOnce info;
        public Span<(int Offset, int Length)> segmentSpan;

        public UnixInfoAllocOncePair(UnixInfoAllocOnce info, Span<(int Offset, int Length)> segmentSpan)
        {
            this.info = info;
            this.segmentSpan = segmentSpan;
        }

        #region Write
        public readonly void Write(ref char destination)
        {
            nuint destinationOffset = default;
            if (info.startsWithSeparator)
            {
                if (info.segmentCount == 0)
                {
                    destination = '/';
                    return;
                }

                destinationOffset = WriteSegmentsWithStartingSeparator(ref destination);
            }
            else if (info.parentSegmentCount > 0)
            {
                destinationOffset = WriteParentSegments(ref destination);
                if (info.segmentCount != 0)
                {
                    destinationOffset += WriteSegmentsWithStartingSeparator(ref Unsafe.Add(ref destination, destinationOffset));
                }
            }
            else if (info.hasLeadingCurrentSegment)
            {
                destination = '.';
                if (info.segmentCount == 0)
                {
                    destinationOffset = 1;
                }
                else
                {
                    destinationOffset = WriteSegmentsWithStartingSeparator(ref Unsafe.Add(ref destination, 1)) + 1;
                }
            }
            else if (info.segmentCount != 0)
            {
                destinationOffset = WriteSegmentsWithoutStartingSeparator(ref destination);
            }

            if (info.endsWithSeparator)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
            }
        }

        public readonly void Write(Span<char> destination)
        {
            if (info.startsWithSeparator)
            {
                if (info.segmentCount == 0)
                {
                    destination[0] = '/';
                    return;
                }

                destination = WriteSegmentsWithStartingSeparator(destination);
            }
            else if (info.parentSegmentCount > 0)
            {
                destination = WriteParentSegments(destination);
                if (info.segmentCount != 0)
                {
                    destination = WriteSegmentsWithStartingSeparator(destination);
                }
            }
            else if (info.hasLeadingCurrentSegment)
            {
                destination[0] = '.';
                if (info.segmentCount == 0)
                {
                    destination = destination[1..];
                }
                else
                {
                    destination = WriteSegmentsWithStartingSeparator(destination[1..]);
                }
            }
            else if (info.segmentCount != 0)
            {
                destination = WriteSegmentsWithoutStartingSeparator(destination);
            }

            if (info.endsWithSeparator)
            {
                destination[0] = '/';
                destination = destination[1..];
            }

            Debug.Assert(destination.IsEmpty);
        }

        public static void Create(Span<char> span, UnixInfoAllocOncePair arg) => arg.Write(ref MemoryMarshal.GetReference(span));

        private readonly nuint WriteParentSegments(ref char destination)
        {
            Unsafe.Add(ref destination, 1) = destination = '.';
            for (int i = info.parentSegmentCount - 2, offset = 2; i >= 0; --i, offset += 3)
            {
                Unsafe.Add(ref destination, offset + 2) = '.';
                Unsafe.Add(ref destination, offset + 1) = '.';
                Unsafe.Add(ref destination, offset) = '/';
            }

            return (nuint)(info.parentSegmentCount * 3 - 1);
        }

        private readonly Span<char> WriteParentSegments(Span<char> destination)
        {
            destination[1] = destination[0] = '.';
            for (int i = info.parentSegmentCount - 2, offset = 2; i >= 0; --i, offset += 3)
            {
                destination[offset + 2] = '.';
                destination[offset + 1] = '.';
                destination[offset] = '/';
            }

            return destination[(info.parentSegmentCount * 3 - 1)..];
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
            ref var sourceRef = ref MemoryMarshal.GetReference(info.textSpan);
            for (int segmentIndex = info.segmentCount - 1; segmentIndex >= 0; --segmentIndex)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
                destinationOffset += Copy(ref Unsafe.Add(ref destination, destinationOffset + 1), ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex)) + 1;
            }

            return destinationOffset;
        }

        private readonly Span<char> WriteSegmentsWithStartingSeparator(Span<char> destination)
        {
            for (int segmentIndex = info.segmentCount - 1; segmentIndex >= 0; --segmentIndex)
            {
                destination[0] = '/';
                var (offset, length) = segmentSpan[segmentIndex];
                info.textSpan.Slice(offset, length).CopyTo(destination[1..]);
                destination = destination[(length + 1)..];
            }

            return destination;
        }

        private readonly nuint WriteSegmentsWithoutStartingSeparator(ref char destination)
        {
            ref var sourceRef = ref MemoryMarshal.GetReference(info.textSpan);
            ref var segmentRef = ref MemoryMarshal.GetReference(segmentSpan);
            int segmentIndex = info.segmentCount - 1;
            var destinationOffset = Copy(ref destination, ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex));
            for (segmentIndex = info.segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                Unsafe.Add(ref destination, destinationOffset) = '/';
                destinationOffset += Copy(ref Unsafe.Add(ref destination, destinationOffset + 1), ref sourceRef, ref Unsafe.Add(ref segmentRef, segmentIndex)) + 1;
            }

            return destinationOffset;
        }

        private readonly Span<char> WriteSegmentsWithoutStartingSeparator(Span<char> destination)
        {
            int segmentIndex = info.segmentCount - 1;
            var (offset, length) = segmentSpan[segmentIndex];
            info.textSpan.Slice(offset, length).CopyTo(destination);
            destination = destination[length..];
            for (segmentIndex = info.segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                destination[0] = '/';
                (offset, length) = segmentSpan[segmentIndex];
                info.textSpan.Slice(offset, length).CopyTo(destination[1..]);
                destination = destination[(length + 1)..];
            }

            return destination;
        }
        #endregion
    }

    private struct UnixTuple64(ulong separator, ulong separatorDuplicate, ulong current, ulong parent)
    {
        public ulong separator = separator;
        public ulong separatorDuplicate = separatorDuplicate;
        public ulong current = current;
        public ulong parent = parent;

        public readonly ulong Any => separatorDuplicate | current | parent;

        public readonly int EstimateSegmentCapacity()
        {
            return BitOperations.PopCount(Any);
        }

        public void ZeroHighBits(int clearIndex)
        {
            separator = BitSpan.ZeroHighBits(separator, clearIndex);
            separatorDuplicate = BitSpan.ZeroHighBits(separatorDuplicate, clearIndex);
            current = BitSpan.ZeroHighBits(current, clearIndex);
            parent = BitSpan.ZeroHighBits(parent, clearIndex);
        }
    }

    private ref struct UnixInfoAllocOnce
    {
        public readonly ReadOnlySpan<char> textSpan;
        public readonly bool startsWithSeparator;
        public readonly bool endsWithSeparator;
        public int segmentCount;
        public int segmentCharCount;
        public int parentSegmentCount;
        public bool hasLeadingCurrentSegment;

        public UnixInfoAllocOnce(ReadOnlySpan<char> textSpan, bool startsWithSeparator, bool endsWithSeparator)
        {
            this.textSpan = textSpan;
            this.startsWithSeparator = startsWithSeparator;
            this.endsWithSeparator = endsWithSeparator;
            segmentCount = 0;
            segmentCharCount = 0;
            parentSegmentCount = 0;
            hasLeadingCurrentSegment = false;
        }

        public readonly int CalculateLength()
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

        public string ToStringGT64LTE1024(string path)
        {
            const int BitShift = 6, BitCount = 1 << BitShift, BitMask = BitCount - 1;
            Debug.Assert(textSpan.Length > BitCount);
            int batchCount = (textSpan.Length + BitMask) >>> BitShift;
            var bitArraySpan = (stackalloc UnixTuple64[batchCount]);
            var segmentCapacity = InitializeBitArray(ref MemoryMarshal.GetReference(bitArraySpan));
            var segmentSpan = (stackalloc (int Offset, int Length)[segmentCapacity]);
            return ToString(path, segmentSpan, bitArraySpan);
        }

        private string ToString(string path, scoped Span<(int Offset, int Length)> segmentSpan, scoped Span<UnixTuple64> bitArraySpan)
        {
            var textIndex = textSpan.Length - 1;
            var batchIndex = bitArraySpan.Length - 1;
            var continueLength = ProcessLoop(segmentSpan, ref textIndex, 0, bitArraySpan[batchIndex], batchIndex);
            while (--batchIndex > 0)
            {
                continueLength = ProcessLoop(segmentSpan, ref textIndex, continueLength, bitArraySpan[batchIndex], batchIndex);
            }

            continueLength = ProcessLoop(segmentSpan, ref textIndex, continueLength, bitArraySpan[0], 0);
            if (continueLength > 0)
            {
                ProcessLastContinuation(segmentSpan, continueLength);
            }

            return ToString(path, segmentSpan);
        }

        public string ToStringGT1024(string path)
        {
            const int BitShift = 6, BitCount = 1 << BitShift, BitMask = BitCount - 1;
            Debug.Assert(textSpan.Length > BitCount);
            int batchCount = (textSpan.Length + BitMask) >>> BitShift;
            var bitArrayArray = ArrayPool<ulong>.Shared.Rent(batchCount * 4);
            try
            {
                var segmentCapacity = InitializeBitArray(ref Unsafe.As<ulong, UnixTuple64>(ref MemoryMarshal.GetArrayDataReference(bitArrayArray)));
                if (segmentCapacity > 128)
                {
                    var segmentArray = ArrayPool<ulong>.Shared.Rent(segmentCapacity);
                    try
                    {
                        var segmentSpan = MemoryMarshal.Cast<ulong, (int Offset, int Length)>(segmentArray.AsSpan(0, segmentCapacity));
                        return ToString(path, segmentSpan, MemoryMarshal.Cast<ulong, UnixTuple64>(bitArrayArray.AsSpan(0, batchCount * 4)));
                    }
                    finally
                    {
                        ArrayPool<ulong>.Shared.Return(segmentArray);
                    }
                }
                else
                {
                    var segmentSpan = (stackalloc (int Offset, int Length)[segmentCapacity]);
                    return ToString(path, segmentSpan, MemoryMarshal.Cast<ulong, UnixTuple64>(bitArrayArray.AsSpan(0, batchCount * 4)));
                }
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(bitArrayArray);
            }
        }

        private readonly int InitializeBitArray(ref UnixTuple64 tupleRef)
        {
            const int BitShift = 6, BitCount = 1 << BitShift, BitMask = BitCount - 1;
            int batchCount = (textSpan.Length + BitMask) >>> BitShift, batchIndex = batchCount - 2;
            const ulong OneBit = 1;
#pragma warning disable IDE0018
            ulong separatorCurrent, separatorPrev, dotCurrent, dotPrev, separatorWall, separatorDuplicate, current, parent;
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
            separatorWall = BitSpan.CalculateSeparatorWall(separatorCurrent, textSpan.Length - 1);
            current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
            parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
            separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | (endsWithSeparator ? OneBit << (textSpan.Length - 1) : default));
            var segmentCapacity = (Unsafe.Add(ref tupleRef, batchIndex + 1) = new(separatorCurrent, separatorDuplicate, current, parent)).EstimateSegmentCapacity();

            while (--batchIndex >= 0)
            {
                separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
                dotCurrent = dotPrev;
                separatorCurrent = separatorPrev;
                separatorPrev = BitSpan.Get(textSpan[(batchIndex * BitCount)..], out dotPrev);
                separatorDuplicate = separatorCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask) | separatorWall);
                current = dotCurrent & ((separatorCurrent << 1) | (separatorPrev >>> BitMask)) & separatorWall;
                parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> BitMask)) & ((separatorCurrent << 2) | (separatorPrev >>> (BitCount - 2))) & separatorWall;
                segmentCapacity += (Unsafe.Add(ref tupleRef, batchIndex + 1) = new(separatorCurrent, separatorDuplicate, current, parent)).EstimateSegmentCapacity();
            }

            separatorWall = (separatorCurrent << BitMask) | (separatorPrev >>> 1);
            separatorDuplicate = separatorPrev & ((separatorPrev << 1) | separatorWall);
            current = dotPrev & ((separatorPrev << 1) | OneBit) & separatorWall;
            parent = dotPrev & (dotPrev << 1) & ((separatorPrev << 2) | (OneBit << 1)) & separatorWall;
            return (tupleRef = new(separatorPrev, separatorDuplicate, current, parent)).EstimateSegmentCapacity() + segmentCapacity + 1;
        }

        public string ToStringLTE64(string path)
        {
            const int BitCount = 64;
            Debug.Assert(textSpan.Length <= BitCount);
            const ulong OneBit = 1;
#pragma warning disable IDE0018
            ulong separator, dot, current, parent, separatorDuplicate, separatorWall;
#pragma warning restore IDE0018
            separator = textSpan.Length == 64
                ? BitSpan.Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(textSpan)), out dot)
                : BitSpan.Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(textSpan)), out dot, textSpan.Length);
            separatorWall = BitSpan.CalculateSeparatorWall(separator, textSpan.Length - 1);
            current = dot & ((separator << 1) | OneBit) & separatorWall;
            parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
            separatorDuplicate = separator & (separator >>> 1);
            UnixTuple64 tuple = new(separator, separatorDuplicate, current, parent);
            if (tuple.Any == default)
            {
                segmentCount = 1;
                segmentCharCount = textSpan.Length;
                var answerLength = CalculateLength();
                if (answerLength >= path.Length)
                {
                    return path;
                }
                else
                {
                    return string.Create(answerLength, this, CopySingle);
                }
            }

            var segmentCapacity = tuple.EstimateSegmentCapacity() + 1;
            var segmentSpan = (stackalloc ValueTuple<int, int>[segmentCapacity]);

            var textIndex = textSpan.Length - 1;
            var continueLength = ProcessLoop(segmentSpan, ref textIndex, 0, tuple, 0);
            if (continueLength > 0)
            {
                ProcessLastContinuation(segmentSpan, continueLength);
            }

            return ToString(path, segmentSpan);
        }

        private readonly string ToString(string path, Span<(int, int)> segmentSpan)
        {
            var answerLength = CalculateLength();
            if (answerLength <= 0)
            {
                return "";
            }
            else if (answerLength >= path.Length)
            {
                return path;
            }
            else
            {
                var pair = new UnixInfoAllocOncePair(this, segmentSpan);
                return string.Create(answerLength, pair, UnixInfoAllocOncePair.Create);
            }
        }

        private void ProcessLastContinuation(scoped Span<(int Offset, int Length)> segmentSpan, int length)
        {
            if (parentSegmentCount > 0)
            {
                --parentSegmentCount;
            }
            else
            {
                segmentCharCount += AddOrUniteSegment(segmentSpan, 0, length, length + 2);
            }
        }

        private int ProcessLoop(scoped Span<(int Offset, int Length)> segmentSpan, ref int textIndex, int continueLength, UnixTuple64 tuple, int batchIndex)
        {
            const int BitCount = 64, BitMask = BitCount - 1;
            var loopLowerLimit = batchIndex * BitCount;
            var loopUpperLimit = loopLowerLimit + BitCount;
            int nextSeparatorIndex, length;
            #region ContinueLength > 0
            if (continueLength > 0)
            {
                if (BitSpan.GetBit(tuple.separator, textIndex))
                {
                    nextSeparatorIndex = textIndex;
                    length = continueLength;
                }
                else
                {
                    Debug.Assert(!BitSpan.GetBit(tuple.parent, textIndex) && !BitSpan.GetBit(tuple.current, textIndex));
                    var temp = BitSpan.ZeroHighBits(tuple.separator, textIndex);
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
                        AddSegment(segmentSpan, nextSeparatorIndex + 1, segmentCharCount = length);
                    }
                    else
                    {
                        ref var oldPair = ref segmentSpan[segmentCount - 1];
                        var diff = oldPair.Offset - nextSeparatorIndex - length - 1;
                        switch (diff)
                        {
                            case 0:
                            case 1:
                                oldPair.Offset = nextSeparatorIndex + 1;
                                oldPair.Length += (length += diff);
                                break;
                            default:
                                AddSegment(segmentSpan, nextSeparatorIndex + 1, length);
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
                tuple.ZeroHighBits(textIndex + 1);
            }

            if (tuple.Any == 0)
            {
                if (parentSegmentCount == 0 || tuple.separator == 0)
                {
                    continueLength = (textIndex & BitMask) + 1;
                    textIndex = loopLowerLimit - 1;
                    Debug.Assert(continueLength >= 0);
                    return continueLength;
                }
                else
                {
                    parentSegmentCount -= BitOperations.PopCount(BitSpan.ZeroHighBits(tuple.separator, textIndex));
                    if (parentSegmentCount >= 0)
                    {
                        textIndex = loopLowerLimit - 1;
                        return BitOperations.TrailingZeroCount(tuple.separator);
                    }
                    else
                    {
                        var tempSeparator = tuple.separator;
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
                if (BitSpan.GetBit(tuple.separator | tuple.parent | tuple.current, textIndex))
                {
                    if (BitSpan.GetBit(tuple.separator, textIndex))
                    {
                        var temp = BitSpan.ZeroHighBits(~tuple.separator, textIndex);
                        textIndex = loopUpperLimit - 1 - BitOperations.LeadingZeroCount(temp);
                    }
                    else if (BitSpan.GetBit(tuple.parent, textIndex))
                    {
                        ++parentSegmentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(tuple.current, textIndex));
                        hasLeadingCurrentSegment = parentSegmentCount == 0;
                        textIndex -= 2;
                    }

                    continue;
                }

                {
                    var temp = BitSpan.ZeroHighBits(tuple.separator, textIndex);
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
                    segmentCharCount += AddOrUniteSegment(segmentSpan, nextSeparatorIndex + 1, length, textIndex + 2);
                }

                textIndex = nextSeparatorIndex - 1;
            }
            while (textIndex >= loopLowerLimit);
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AddSegment(scoped Span<(int Offset, int Length)> segmentSpan, int offset, int length)
        {
            segmentSpan[segmentCount++] = new(offset, length);
            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AddOrUniteSegment(scoped Span<(int Offset, int Length)> segmentSpan, int offset, int length, int expectedOffset)
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

            return AddSegment(segmentSpan, offset, length);
        }

        private static void CopySingle(Span<char> span, UnixInfoAllocOnce arg)
        {
            if (arg.startsWithSeparator)
            {
                span[0] = '/';
                span = span[1..];
            }

            if (arg.endsWithSeparator)
            {
                span[^1] = '/';
            }

            arg.textSpan.CopyTo(span);
        }
    }
}
