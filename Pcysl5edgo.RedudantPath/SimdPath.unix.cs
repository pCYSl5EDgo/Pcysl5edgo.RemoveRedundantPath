using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Pcysl5edgo.RemoveRedundantPath;

public static class SimdPath
{
    public static string RemoveRedundantSegmentsSpan(string? path)
    {
        if (path is null)
        {
            return string.Empty;
        }
        else if (path.Length <= 1)
        {
            return path;
        }
        else if (path.Length == 2)
        {
            return path[0] != '/' || path[1] != '/' && path[1] != '.' ? path : "/";
        }

        var span = path.AsSpan();
        if (span.Length <= 64)
        {
            return ImplLTE64(path, ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span)));
        }
        else
        {
            return ImplGT64(path, ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span)));
        }
    }

    private static string ImplLTE64(string path, ref ushort text)
    {
        var separator = InitializeSeparatorCurrentParent(ref text, path.Length, out var current, out var parent);
        if (separator == default)
        {
            return path;
        }

        var _ = (stackalloc int[(BitOperations.PopCount(separator) + 1) << 1]);
        var info = new UnixInfo(path, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(separator, 64, info.Offset);
        }

        while (info.IsInRange)
        {
            var foundIndex = BitOperations.TrailingZeroCount(separator >>> info.Offset) + info.Offset;
            if (foundIndex >= 64)
            {
                // separator not found
                if (BitSpan.GetBit(current, info.Offset))
                {
                    if (info.IsSegmentEmpty)
                    {
                        return ".";
                    }
                }
                else if (BitSpan.GetBit(parent, info.Offset))
                {
                    if (info.IsSegmentEmpty)
                    {
                        return "..";
                    }

                    info.TryAddParentSegment();
                }
                else
                {
                    info.AddNormalSegment(path.Length - info.Offset);
                }

                info.EndsWithSeparator = false;
                break;
            }
            else
            {
                if (BitSpan.GetBit(current, info.Offset))
                {
                    info.TryAddCurrentSegment();
                }
                else if (BitSpan.GetBit(parent, info.Offset))
                {
                    info.TryAddParentSegment();
                }
                else
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                }

                info.Offset = BitSpan.TrailingOneCount(separator, 64, foundIndex + 1);
            }
        }

        return info.CreateOrAsIs(path);
    }

    private static string ImplGT64(string path, ref ushort text)
    {
        var ulongCount = (path.Length + 63) >>> 6;
        var __ = (stackalloc ulong[ulongCount * 3]);
        ref var separatorRef = ref MemoryMarshal.GetReference(__);
        ref var currentRef = ref Unsafe.Add(ref separatorRef, ulongCount);
        ref var parentRef = ref Unsafe.Add(ref currentRef, ulongCount);
        var maxSegmentCount = InitializeSeparatorCurrentParent(ref text, path.Length, ref separatorRef, ref currentRef, ref parentRef) + 1;
        var _ = (stackalloc int[maxSegmentCount << 1]);
        var info = new UnixInfo(path, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(ref currentRef, path.Length, info.Offset);
        }

        while (info.IsInRange)
        {
            var foundIndex = BitSpan.TrailingZeroCount(ref separatorRef, path.Length, info.Offset);
            if (foundIndex >= path.Length)
            {
                // separator not found
                if (BitSpan.GetBit(ref currentRef, info.Offset))
                {
                    if (info.IsSegmentEmpty)
                    {
                        return ".";
                    }
                }
                else if (BitSpan.GetBit(ref parentRef, info.Offset))
                {
                    if (info.IsSegmentEmpty)
                    {
                        return "..";
                    }

                    info.TryAddParentSegment();
                }
                else
                {
                    info.AddNormalSegment(path.Length - info.Offset);
                }

                info.EndsWithSeparator = false;
                break;
            }
            else
            {
                if (BitSpan.GetBit(ref currentRef, info.Offset))
                {
                    info.TryAddCurrentSegment();
                }
                else if (BitSpan.GetBit(ref parentRef, info.Offset))
                {
                    info.TryAddParentSegment();
                }
                else
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                }

                info.Offset = BitSpan.TrailingOneCount(ref separatorRef, 64, foundIndex + 1);
            }
        }

        return info.CreateOrAsIs(path);
    }

    private static ulong InitializeSeparatorCurrentParent(ref ushort text, int textLength, out ulong current, out ulong parent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(textLength, nameof(textLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textLength, 64, nameof(textLength));
        ulong dot = default, separator = default;
        int offset = default;
        if (BitConverter.IsLittleEndian)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                for (; offset + Vector256<ushort>.Count <= textLength; offset += Vector256<ushort>.Count)
                {
                    var v = Vector256.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector256.Narrow(Vector256.Equals(v, Vector256.Create((ushort)'/')), Vector256.Equals(v, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
                    separator |= ((ulong)(compoundSeparatorDot & 0xffffu)) << offset;
                    dot |= ((ulong)((compoundSeparatorDot & 0xffff0000u) >>> 16)) << offset;
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                for (; offset + Vector128<ushort>.Count <= textLength; offset += Vector128<ushort>.Count)
                {
                    var v = Vector128.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                    separator |= ((ulong)(compoundSeparatorDot & 0xffu)) << offset;
                    dot |= ((ulong)((compoundSeparatorDot & 0xff00u) >>> 8)) << offset;
                }
            }
        }

        for (; offset < textLength; ++offset)
        {
            switch (Unsafe.Add(ref text, offset))
            {
                case '/':
                    separator |= 1ul << offset;
                    break;
                case '.':
                    dot |= 1ul << offset;
                    break;
            }
        }

        if (separator == default)
        {
            Unsafe.SkipInit(out current);
            Unsafe.SkipInit(out parent);
        }
        else
        {
            var separatorWithBitWall = separator | ((ulong.MaxValue >>> textLength) << textLength);
            // /./ or /.$ or ^./ or ^.$
            current = ((separator << 1) | 1ul) & dot & (separatorWithBitWall >>> 1);
            // /../ or /..$ or ^../ or ^..$
            parent = ((separator << 1) | 1ul) & dot & (dot >>> 1) & (separatorWithBitWall >>> 2);
        }

        return separator;
    }

    private static int InitializeSeparatorCurrentParent(ref ushort text, int textLength, ref ulong separatorRef, ref ulong currentRef, ref ulong parentRef)
    {
        int offset = default;
        if (BitConverter.IsLittleEndian)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                for (; offset + Vector256<ushort>.Count <= textLength; offset += Vector256<ushort>.Count)
                {
                    var v = Vector256.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector256.Narrow(Vector256.Equals(v, Vector256.Create((ushort)'/')), Vector256.Equals(v, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
                    var tempOffset = offset & 63;
                    var partialSeparator = (ushort)compoundSeparatorDot;
                    Unsafe.Add(ref separatorRef, offset >>> 6) |= ((ulong)(partialSeparator)) << tempOffset;
                    Unsafe.Add(ref currentRef, offset >>> 6) |= ((ulong)((compoundSeparatorDot & 0xffff0000u) >>> 16)) << tempOffset;
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                for (; offset + Vector128<ushort>.Count <= textLength; offset += Vector128<ushort>.Count)
                {
                    var v = Vector128.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                    var tempOffset = offset & 63;
                    var partialSeparator = (byte)compoundSeparatorDot;
                    Unsafe.Add(ref separatorRef, offset >>> 6) |= ((ulong)partialSeparator) << tempOffset;
                    Unsafe.Add(ref currentRef, offset >>> 6) |= ((ulong)((compoundSeparatorDot & 0xff00u) >>> 8)) << tempOffset;
                }
            }
        }

        for (; offset < textLength; ++offset)
        {
            var tempOffset = offset & 63;
            switch (Unsafe.Add(ref text, offset))
            {
                case '/':
                    Unsafe.Add(ref separatorRef, offset >>> 6) |= 1ul << tempOffset;
                    break;
                case '.':
                    Unsafe.Add(ref currentRef, offset >>> 6) |= 1ul << tempOffset;
                    break;
            }
        }

        var segmentCount = BitOperations.PopCount(separatorRef & ~((separatorRef << 1) & separatorRef));
        var nextSeparatorLsh62 = Unsafe.Add(ref separatorRef, 1) << 62;
        var separatorLsh1 = (separatorRef << 1) | 1ul;
        parentRef = separatorLsh1 & currentRef & ((Unsafe.Add(ref currentRef, 1) << 63) | (currentRef >>> 1)) & (nextSeparatorLsh62 | (separatorRef >>> 2));
        currentRef &= separatorLsh1 & ((nextSeparatorLsh62 << 1) | (separatorRef >>> 1));
        var oldSeparator = separatorRef >>> 63;
        int arrayLengthMinus1 = ((textLength + 63) >>> 6) - 1;
        for (int arrayIndex = 1; arrayIndex < arrayLengthMinus1; ++arrayIndex)
        {
            ref var tempCurrent = ref Unsafe.Add(ref currentRef, arrayIndex);
            var tempSeparator = Unsafe.Add(ref separatorRef, arrayIndex);
            separatorLsh1 = (tempSeparator << 1) | oldSeparator;
            oldSeparator = tempSeparator >>> 63;
            segmentCount += BitOperations.PopCount(tempSeparator & ~(separatorLsh1 & tempSeparator));
            nextSeparatorLsh62 = Unsafe.Add(ref separatorRef, arrayIndex + 1) << 62;
            Unsafe.Add(ref parentRef, arrayIndex) = separatorLsh1 & tempCurrent & ((Unsafe.Add(ref tempCurrent, 1) << 63) | (tempCurrent >>> 1)) & (nextSeparatorLsh62 | (tempSeparator >>> 2));
            tempCurrent &= separatorLsh1 & ((nextSeparatorLsh62 << 1) | (tempSeparator >>> 1));
        }

        {
            ref var tempCurrent = ref Unsafe.Add(ref currentRef, arrayLengthMinus1);
            var tempSeparator = Unsafe.Add(ref separatorRef, arrayLengthMinus1) | ((ulong.MaxValue >>> (textLength & 63)) << (textLength & 63));
            separatorLsh1 = (tempSeparator << 1) | oldSeparator;
            segmentCount += BitOperations.PopCount(tempSeparator & ~(separatorLsh1 & tempSeparator));
            Unsafe.Add(ref parentRef, arrayLengthMinus1) = separatorLsh1 & tempCurrent & (tempCurrent >>> 1) & ((ulong.MaxValue << 62) | (tempSeparator >>> 2));
            tempCurrent &= separatorLsh1 & ((ulong.MaxValue << 63) | (tempSeparator >>> 1));
        }

        return segmentCount;
    }

    private ref struct UnixInfo
    {
        private readonly ReadOnlySpan<char> Text;
        private readonly ref int OffsetRef;
        private readonly ref int LengthRef;
        private int SegmentCount;
        public readonly bool IsSegmentEmpty => SegmentCount == 0;
        public readonly bool IsSeparatorOnly => StartsWithSeparator && SegmentCount == 1;
        private bool StartsWithCurrent;
        public readonly bool StartsWithSeparator;
        public bool EndsWithSeparator;
        private int NotParentCount;
        public int Offset;
        public readonly int TotalLength => Sum(ref LengthRef, SegmentCount) + SegmentCount - (!EndsWithSeparator ? 1 : 0);
        public readonly bool IsInRange => Offset < Text.Length;

        public UnixInfo(ReadOnlySpan<char> text, Span<int> segmentSpan)
        {
            Text = text;
            OffsetRef = ref MemoryMarshal.GetReference(segmentSpan);
            LengthRef = ref Unsafe.Add(ref OffsetRef, segmentSpan.Length >>> 1);
            StartsWithCurrent = false;
            EndsWithSeparator = true;
            if (StartsWithSeparator = MemoryMarshal.GetReference(text) == '/')
            {
                Offset = 1;
                OffsetRef = 0;
                LengthRef = 0;
                SegmentCount = 1;
            }
            else
            {
                Offset = 0;
                SegmentCount = 0;
            }
        }

        public void AddNormalSegment(int length)
        {
            Unsafe.Add(ref OffsetRef, SegmentCount) = Offset;
            Unsafe.Add(ref LengthRef, SegmentCount++) = length;
            ++NotParentCount;
        }

        public void TryAddCurrentSegment()
        {
            if (IsSegmentEmpty)
            {
                OffsetRef = Offset;
                LengthRef = 1;
                SegmentCount = 1;
                StartsWithCurrent = true;
            }
        }

        public void TryAddParentSegment()
        {
            if (NotParentCount > 0)
            {
                --NotParentCount;
                if (--SegmentCount == 0 && StartsWithCurrent)
                {
                    StartsWithCurrent = false;
                    OffsetRef = Offset;
                    LengthRef = 2;
                    SegmentCount = 1;
                }
                return;
            }

            switch (SegmentCount)
            {
                case 0:
                    OffsetRef = Offset;
                    LengthRef = 2;
                    SegmentCount = 1;
                    return;
                case 1:
                    if (StartsWithSeparator)
                    {
                        return;
                    }
                    break;
            }

            ref var oldLength = ref Unsafe.Add(ref LengthRef, SegmentCount - 1);
            if (oldLength + Unsafe.Add(ref OffsetRef, SegmentCount - 1) + 1 == Offset)
            {
                oldLength += 3;
            }
            else
            {
                Unsafe.Add(ref OffsetRef, SegmentCount) = Offset;
                Unsafe.Add(ref LengthRef, SegmentCount++) = 2;
            }
        }

        public static void Create(Span<char> span, UnixInfo arg)
        {
            for (int segmentIndex = 0; segmentIndex < arg.SegmentCount;)
            {
                var slice = arg.Text.Slice(Unsafe.Add(ref arg.OffsetRef, segmentIndex), Unsafe.Add(ref arg.LengthRef, segmentIndex));
                slice.CopyTo(span);
                span = span[slice.Length..];
                if (++segmentIndex < arg.SegmentCount || arg.EndsWithSeparator)
                {
                    MemoryMarshal.GetReference(span) = '/';
                    span = span[1..];
                }
            }
        }

        private static int Sum(ref int value, int length)
        {
            int answer = 0;
            int index = 0;
            if (Vector.IsHardwareAccelerated && length >= Vector<int>.Count << 1)
            {
                Vector<int> temp = Vector<int>.Zero;
                for (; index + Vector<int>.Count <= length; index += Vector<int>.Count)
                {
                    temp += Vector.LoadUnsafe(ref value, (uint)index);
                }

                for (int i = 0; i < Vector<int>.Count; i++)
                {
                    answer += temp[i];
                }
            }

            for (; index < length; ++index)
            {
                answer += Unsafe.Add(ref value, index);
            }

            return answer;
        }

        public readonly string CreateOrAsIs(string path)
        {
            if (IsSeparatorOnly)
            {
                return "/";
            }

            var answerLength = TotalLength;
            if (answerLength <= 0)
            {
                return "";
            }
            else if (answerLength == path.Length)
            {
                return path;
            }

            return string.Create(answerLength, this, UnixInfo.Create);
        }
    }
}
