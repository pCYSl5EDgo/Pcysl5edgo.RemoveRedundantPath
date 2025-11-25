using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Pcysl5edgo.RemoveRedundantPath;

public static class SimdPath
{
    public static string RemoveRedundantSegmentsEach(string? path)
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
        return ImplEach(span) ?? path;
    }

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
        ref var textRef = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span));
        return span.Length switch
        {
            <= 32 => ImplLTE32(ref textRef, path.Length),
            <= 64 => ImplLTE64(ref textRef, path.Length),
            <= 128 => ImplLTE128(ref textRef, path.Length),
            _ => ImplGT128(ref textRef, path.Length),
        } ?? path;
    }

    private static string? ImplEach(ReadOnlySpan<char> text)
    {
        var span = (stackalloc int[text.Length + 2]);
        var info = new UnixInfo(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(text)), text.Length, span)
        {
            EndsWithSeparator = text[^1] == '/'
        };
        while (info.Offset < text.Length)
        {
            var c = text[info.Offset++];
            if (c == '/')
            {
                var foundIndex = text[info.Offset..].IndexOfAnyExcept('/');
                if (foundIndex < 0)
                {
                    break;
                }

                info.Offset += foundIndex;
            }
            else if (c == '.')
            {
                if (info.Offset >= text.Length)
                {
                    --info.Offset;
                    info.TryAddCurrentSegment();
                    break;
                }

                c = text[info.Offset++];
                if (c == '/')
                {
                    info.Offset -= 2;
                    info.TryAddCurrentSegment();
                    info.Offset += 2;
                }
                else if (c == '.')
                {
                    if (info.Offset >= text.Length)
                    {
                        info.Offset -= 2;
                        info.TryAddParentSegment();
                        break;
                    }

                    if (text[info.Offset++] == '/')
                    {
                        info.Offset -= 3;
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }
                    else
                    {
                        var foundIndex = text[info.Offset..].IndexOf('/');
                        info.Offset -= 3;
                        if (foundIndex < 0)
                        {
                            info.AddNormalSegment(text.Length - info.Offset);
                            break;
                        }
                        else
                        {
                            info.AddNormalSegment(foundIndex + 3);
                            info.Offset += 3 + foundIndex;
                        }
                    }
                }
                else
                {
                    var foundIndex = text[info.Offset..].IndexOf('/');
                    info.Offset -= 2;
                    if (foundIndex < 0)
                    {
                        info.AddNormalSegment(text.Length - info.Offset);
                        break;
                    }
                    else
                    {
                        info.AddNormalSegment(foundIndex + 2);
                        info.Offset += foundIndex + 2;
                    }
                }
            }
            else
            {
                var foundIndex = text[info.Offset..].IndexOf('/');
                --info.Offset;
                if (foundIndex < 0)
                {
                    info.AddNormalSegment(text.Length - info.Offset);
                    break;
                }
                else
                {
                    info.AddNormalSegment(foundIndex + 1);
                    info.Offset += foundIndex + 2;
                }
            }
        }

        return info.ToString();
    }

    private static string? ImplLTE32(ref ushort text, int textLength)
    {
        var dot = InitializeSeparatorDot(ref text, textLength, out uint separator);
        if (separator == default)
        {
            return default;
        }

        var separatorWithBitWall = separator | ((uint.MaxValue >>> textLength) << textLength);
        // /./ or /.$ or ^./ or ^.$
        var current = ((separator << 1) | 1u) & dot & (separatorWithBitWall >>> 1);
        // /../ or /..$ or ^../ or ^..$
        var parent = ((separator << 1) | 1u) & dot & (dot >>> 1) & (separatorWithBitWall >>> 2);
        var _ = (stackalloc int[(CountAloneSeparator(separator) + 1) << 1]);
        var info = new UnixInfo(ref text, textLength, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(separator, info.Offset);
        }

        var cp = current | parent;
        while (info.IsInRange)
        {
            if (BitSpan.GetBit(cp, info.Offset))
            {
                if (BitSpan.GetBit(current, info.Offset))
                {
                    if (info.Offset + 1 < textLength)
                    {
                        info.TryAddCurrentSegment();
                        info.Offset += 2;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return ".";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }
                else
                {
                    if (info.Offset + 2 < textLength)
                    {
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return "..";
                        }

                        info.EndsWithSeparator = false;
                        info.TryAddParentSegment();
                        break;
                    }
                }
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separator >>> info.Offset) + info.Offset;
                if (foundIndex < 32)
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                    info.Offset = foundIndex + 1;
                }
                else
                {
                    info.AddNormalSegment(textLength - info.Offset);
                    info.EndsWithSeparator = false;
                    break;
                }
            }

            info.Offset = BitSpan.TrailingOneCount(separator, info.Offset);
        }

        return info.ToString();
    }

    private static string? ImplLTE64(ref ushort text, int textLength)
    {
        var dot = InitializeSeparatorDot(ref text, textLength, out ulong separator);
        if (separator == default)
        {
            return default;
        }

        var separatorWithBitWall = separator | ((ulong.MaxValue >>> textLength) << textLength);
        // /./ or /.$ or ^./ or ^.$
        var current = ((separator << 1) | 1ul) & dot & (separatorWithBitWall >>> 1);
        // /../ or /..$ or ^../ or ^..$
        var parent = ((separator << 1) | 1ul) & dot & (dot >>> 1) & (separatorWithBitWall >>> 2);
        var _ = (stackalloc int[(CountAloneSeparator(separator) + 1) << 1]);
        var info = new UnixInfo(ref text, textLength, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(separator, info.Offset);
        }

        var cp = current | parent;
        while (info.IsInRange)
        {
            if (BitSpan.GetBit(cp, info.Offset))
            {
                if (BitSpan.GetBit(current, info.Offset))
                {
                    if (info.Offset + 1 < textLength)
                    {
                        info.TryAddCurrentSegment();
                        info.Offset += 2;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return ".";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }
                else
                {
                    if (info.Offset + 2 < textLength)
                    {
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return "..";
                        }

                        info.EndsWithSeparator = false;
                        info.TryAddParentSegment();
                        break;
                    }
                }
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separator >>> info.Offset) + info.Offset;
                if (foundIndex < 64)
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                    info.Offset = foundIndex + 1;
                }
                else
                {
                    info.AddNormalSegment(textLength - info.Offset);
                    info.EndsWithSeparator = false;
                    break;
                }
            }

            info.Offset = BitSpan.TrailingOneCount(separator, info.Offset);
        }

        return info.ToString();
    }

    private static int CountAloneSeparator(uint separator) => BitOperations.PopCount(separator & ~(separator & (separator << 1)));
    private static int CountAloneSeparator(ulong separator) => BitOperations.PopCount(separator & ~(separator & (separator << 1)));

    private static int CountAloneSeparator(ulong oldSeparator, ulong separator) => BitOperations.PopCount(separator & ~(separator & ((separator << 1) | (oldSeparator >>> 63))));

    /// <summary>
    /// Calculates the combined bitmask representing the current and parent elements based on separator and dot values.
    /// </summary>
    /// <param name="oldSeparator">The previous separator value used to determine bit alignment with the current separator.</param>
    /// <param name="currentSeparator">The separator value for the current element, influencing the calculation of current and parent bitmasks.</param>
    /// <param name="nextSeparator">The separator value for the next element, used to compute bit relationships with the current separator.</param>
    /// <param name="currentDot">The dot value for the current element, representing its bitmask in the calculation.</param>
    /// <param name="nextDot">The dot value for the next element, affecting the determination of parent bitmasks.</param>
    /// <param name="current">When this method returns, contains the bitmask representing the current element, as computed from the provided
    /// parameters.</param>
    /// <returns>A bitmask value that combines the current and parent elements based on the input separator and dot values.</returns>
    private static ulong CalculateCurrentParent(ulong oldSeparator, ulong currentSeparator, ulong nextSeparator, ulong currentDot, ulong nextDot, out ulong current)
    {
        var shiftedSeparator = (currentSeparator << 1) | (oldSeparator >>> 63);
        current = shiftedSeparator & currentDot & ((currentSeparator >>> 1) | (nextSeparator << 63));
        var parent = shiftedSeparator & currentDot & ((currentDot >>> 1) | (nextDot << 63)) & ((currentSeparator >>> 2) | (nextSeparator << 62));
        return current | parent;
    }

    /// <summary>
    /// Calculates the combined bitmask representing the last and current parent positions based on the provided separator and dot masks.
    /// </summary>
    /// <param name="oldSeparator">The previous separator bitmask used to determine parent relationships.</param>
    /// <param name="currentSeparatorWithWall">The current separator bitmask, including wall information, used to identify boundaries.</param>
    /// <param name="currentDot">The bitmask representing the current dot positions to be evaluated.</param>
    /// <param name="current">When this method returns, contains the bitmask of the current parent positions calculated from the input masks.</param>
    /// <returns>A bitmask representing the union of the last and current parent positions derived from the input masks.</returns>
    private static ulong CalculateLastCurrentParent(ulong oldSeparator, ulong currentSeparatorWithWall, ulong currentDot, out ulong current)
    {
        var shiftedSeparator = (currentSeparatorWithWall << 1) | (oldSeparator >>> 63);
        current = shiftedSeparator & currentDot & (currentSeparatorWithWall >>> 1);
        var parent = shiftedSeparator & currentDot & (currentDot >>> 1) & (currentSeparatorWithWall >>> 2);
        return current | parent;
    }

    private static string? ImplLTE128(ref ushort text, int textLength)
    {
        var lastLength = textLength & 63;
        var separatorSpan = (stackalloc ulong[4]);
        var dotNow = InitializeSeparatorDot(ref text, out var separatorNow);
        var dotNext = InitializeSeparatorDot(ref Unsafe.Add(ref text, 64), lastLength, out ulong separatorNext);
        if ((separatorNow | separatorNext) == default)
        {
            return default;
        }

        var separatorWithWall = separatorNext | ((ulong.MaxValue >>> lastLength) << lastLength);
        var cp = CalculateCurrentParent(ulong.MaxValue, separatorNow, separatorWithWall, dotNow, dotNext, out var currentNow);

        var _ = (stackalloc int[(CountAloneSeparator(separatorNow) + CountAloneSeparator(separatorNow, separatorNext) + 1) << 1]);
        var info = new UnixInfo(ref text, textLength, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset);
            if (info.Offset == 64)
            {
                info.Offset = BitSpan.TrailingOneCount(separatorNext, lastLength, 0) + 64;
            }
        }

        var segmentContinue = false;
        while (info.Offset < 64)
        {
            if (BitSpan.GetBit(cp, info.Offset))
            {
                if (BitSpan.GetBit(currentNow, info.Offset))
                {
                    info.TryAddCurrentSegment();
                    info.Offset += 2;
                }
                else
                {
                    info.TryAddParentSegment();
                    info.Offset += 3;
                }

                info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset);
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separatorNow >>> info.Offset) + info.Offset;
                info.AddNormalSegment(foundIndex - info.Offset);
                if (foundIndex < 64)
                {
                    info.Offset = BitSpan.TrailingOneCount(separatorNow, foundIndex + 1);
                }
                else
                {
                    segmentContinue = true;
                    break;
                }
            }
        }

        if (segmentContinue)
        {
            segmentContinue = false;
            var foundIndex = BitOperations.TrailingZeroCount(separatorNext);
            if (foundIndex < 64)
            {
                info.Offset = foundIndex + 64 + 1;
                info.SetLastSegmentEndExclusive(info.Offset - 1);
            }
            else
            {
                info.Offset = textLength;
                info.SetLastSegmentEndExclusive(textLength);
            }
        }

        cp = CalculateLastCurrentParent(separatorNow, separatorWithWall, dotNext, out var currentNext);
        while (info.Offset < textLength)
        {
            if (BitSpan.GetBit(cp, info.Offset))
            {
                if (BitSpan.GetBit(currentNext, info.Offset))
                {
                    if (info.Offset + 1 < textLength)
                    {
                        info.TryAddCurrentSegment();
                        info.Offset += 2;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return ".";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }
                else
                {
                    if (info.Offset + 2 < textLength)
                    {
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return "..";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }

                info.Offset = BitSpan.TrailingOneCount(separatorNext, info.Offset & 63) + 64;
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separatorNext >>> (info.Offset & 63)) + info.Offset;
                if (foundIndex < textLength)
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                    info.Offset = BitSpan.TrailingOneCount(separatorNext, foundIndex - 63) + 64;
                }
                else
                {
                    info.AddNormalSegment(textLength - info.Offset);
                    info.EndsWithSeparator = false;
                    break;
                }
            }
        }

        return info.ToString();
    }

    private static string? ImplGT128(ref ushort text, int textLength)
    {
        var lastLength = textLength & 63;
        var dotNow = InitializeSeparatorDot(ref text, out var separatorNow);
        var _ = (stackalloc int[CountAloneSeparator(separatorNow) + (textLength >>> 1) - 15]);
        var info = new UnixInfo(ref text, textLength, _);
        if (info.StartsWithSeparator)
        {
            info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset);
        }

        var dotNext = InitializeSeparatorDot(ref Unsafe.Add(ref text, 64), out var separatorNext);
        var cp = CalculateCurrentParent(ulong.MaxValue, separatorNow, separatorNext, dotNow, dotNext, out var current);
        var segmentContinue = false;
        while (info.Offset < 64)
        {
            if (BitSpan.GetBit(cp, info.Offset))
            {
                if (BitSpan.GetBit(current, info.Offset))
                {
                    info.TryAddCurrentSegment();
                    info.Offset += 2;
                }
                else
                {
                    info.TryAddParentSegment();
                    info.Offset += 3;
                }

                info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset);
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separatorNow >>> info.Offset) + info.Offset;
                info.AddNormalSegment(foundIndex - info.Offset);
                if (foundIndex < 64)
                {
                    info.Offset = BitSpan.TrailingOneCount(separatorNow, foundIndex + 1);
                }
                else
                {
                    segmentContinue = true;
                    break;
                }
            }
        }

        var arrayLengthMinus1 = (textLength - 1) >> 6;
        for (int arrayIndex = 1; arrayIndex < arrayLengthMinus1; ++arrayIndex)
        {
            var separatorOld = separatorNow;
            separatorNow = separatorNext;
            dotNow = dotNext;
            dotNext = InitializeSeparatorDot(ref Unsafe.Add(ref text, (arrayIndex + 1) << 6), out separatorNext);
            if (segmentContinue)
            {
                var foundIndex = BitOperations.TrailingZeroCount(separatorNow);
                if (foundIndex < 64)
                {
                    segmentContinue = false;
                    info.Offset = foundIndex + (arrayIndex << 6) + 1;
                    info.SetLastSegmentEndExclusive(info.Offset - 1);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset & 63) + (arrayIndex << 6);
            }

            cp = CalculateCurrentParent(separatorOld, separatorNow, separatorNext | (arrayIndex + 1 == arrayLengthMinus1 ? ((ulong.MaxValue >>> lastLength) << lastLength) : 0ul), dotNow, dotNext, out current);
            while (info.Offset < ((arrayIndex + 1) << 6))
            {
                if (BitSpan.GetBit(cp, info.Offset & 63))
                {
                    if (BitSpan.GetBit(current, info.Offset & 63))
                    {
                        info.TryAddCurrentSegment();
                        info.Offset += 2;
                    }
                    else
                    {
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }

                    info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset & 63) + (arrayIndex << 6);
                }
                else
                {
                    var length = BitOperations.TrailingZeroCount(separatorNow >>> (info.Offset & 63));
                    info.AddNormalSegment(length);
                    if (length < 64)
                    {
                        info.Offset = BitSpan.TrailingOneCount(separatorNow, length + 1 + (info.Offset & 63)) + (arrayIndex << 6);
                    }
                    else
                    {
                        segmentContinue = true;
                        break;
                    }
                }
            }
        }

        if (segmentContinue)
        {
            var foundIndex = BitOperations.TrailingZeroCount(separatorNext);
            if (foundIndex < 64)
            {
                info.Offset = foundIndex + (arrayLengthMinus1 << 6);
                info.SetLastSegmentEndExclusive(info.Offset - 1);
            }
            else
            {
                info.Offset = textLength;
                info.SetLastSegmentEndExclusive(textLength);
                info.EndsWithSeparator = true;
            }
        }
        else
        {
            info.Offset = BitSpan.TrailingOneCount(separatorNext, info.Offset & 63) + (arrayLengthMinus1 << 6);
        }

        cp = CalculateLastCurrentParent(separatorNow, separatorNext | ((ulong.MaxValue >>> lastLength) << lastLength), dotNext, out current);
        while (info.IsInRange)
        {
            if (BitSpan.GetBit(cp, info.Offset & 63))
            {
                if (BitSpan.GetBit(current, info.Offset & 63))
                {
                    if (info.Offset + 1 < textLength)
                    {
                        info.TryAddCurrentSegment();
                        info.Offset += 2;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return ".";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }
                else
                {
                    if (info.Offset + 2 < textLength)
                    {
                        info.TryAddParentSegment();
                        info.Offset += 3;
                    }
                    else
                    {
                        if (info.IsSegmentEmpty)
                        {
                            return "..";
                        }

                        info.EndsWithSeparator = false;
                        break;
                    }
                }

                info.Offset = BitSpan.TrailingOneCount(separatorNow, info.Offset & 63) + (arrayLengthMinus1 << 6);
            }
            else
            {
                var foundIndex = BitOperations.TrailingZeroCount(separatorNext >>> (info.Offset & 63)) + info.Offset;
                if (foundIndex < textLength)
                {
                    info.AddNormalSegment(foundIndex - info.Offset);
                    info.Offset = BitSpan.TrailingOneCount(separatorNext, foundIndex & 63) + (arrayLengthMinus1 << 6);
                }
                else
                {
                    info.AddNormalSegment(textLength - info.Offset);
                    info.EndsWithSeparator = false;
                    break;
                }
            }
        }

        return info.ToString();
    }

    private static ulong InitializeSeparatorDot(ref ushort text, out ulong separator)
    {
        ulong _separator = default, _dot = default;
        int offset = default;
        if (BitConverter.IsLittleEndian)
        {
            if (Vector512.IsHardwareAccelerated)
            {
                var v0 = Vector512.LoadUnsafe(ref text, (nuint)offset);
                var v1 = Vector512.LoadUnsafe(ref text, (nuint)(offset + 32));
                _separator |= Vector512.Narrow(Vector512.Equals(v0, Vector512.Create((ushort)'/')), Vector512.Equals(v1, Vector512.Create((ushort)'/'))).ExtractMostSignificantBits();
                _dot |= Vector512.Narrow(Vector512.Equals(v0, Vector512.Create((ushort)'.')), Vector512.Equals(v1, Vector512.Create((ushort)'.'))).ExtractMostSignificantBits();
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                for (; offset < 64; offset += 32)
                {
                    var v0 = Vector256.LoadUnsafe(ref text, (nuint)offset);
                    var v1 = Vector256.LoadUnsafe(ref text, (nuint)(offset + 16));
                    _separator |= ((ulong)Vector256.Narrow(Vector256.Equals(v0, Vector256.Create((ushort)'/')), Vector256.Equals(v1, Vector256.Create((ushort)'/'))).ExtractMostSignificantBits()) << offset;
                    _dot |= ((ulong)Vector256.Narrow(Vector256.Equals(v0, Vector256.Create((ushort)'.')), Vector256.Equals(v1, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits()) << offset;
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                for (; offset < 64; offset += 16)
                {
                    var v0 = Vector128.LoadUnsafe(ref text, (nuint)offset);
                    var v1 = Vector128.LoadUnsafe(ref text, (nuint)(offset + 8));
                    _separator |= ((ulong)Vector128.Narrow(Vector128.Equals(v0, Vector128.Create((ushort)'/')), Vector128.Equals(v1, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits()) << offset;
                    _dot |= ((ulong)Vector128.Narrow(Vector128.Equals(v0, Vector128.Create((ushort)'.')), Vector128.Equals(v1, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits()) << offset;
                }
            }
        }

        for (; offset < 64; ++offset)
        {
            switch (Unsafe.Add(ref text, offset))
            {
                case '/':
                    _separator |= 1ul << offset;
                    break;
                case '.':
                    _dot |= 1ul << offset;
                    break;
            }
        }

        separator = _separator;
        return _dot;
    }

    private static uint InitializeSeparatorDot(ref ushort text, int textLength, out uint separator)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(textLength, nameof(textLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textLength, 32, nameof(textLength));
        uint _dot = default, _separator = default;
        int offset = default;
        if (BitConverter.IsLittleEndian)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                for (; offset + Vector256<ushort>.Count <= textLength; offset += Vector256<ushort>.Count)
                {
                    var v = Vector256.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector256.Narrow(Vector256.Equals(v, Vector256.Create((ushort)'/')), Vector256.Equals(v, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
                    _separator |= (compoundSeparatorDot & 0xffffu) << offset;
                    _dot |= ((compoundSeparatorDot & 0xffff0000u) >>> 16) << offset;
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                for (; offset + Vector128<ushort>.Count <= textLength; offset += Vector128<ushort>.Count)
                {
                    var v = Vector128.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                    _separator |= (compoundSeparatorDot & 0xffu) << offset;
                    _dot |= ((compoundSeparatorDot & 0xff00u) >>> 8) << offset;
                }
            }
        }

        for (; offset < textLength; ++offset)
        {
            switch (Unsafe.Add(ref text, offset))
            {
                case '/':
                    _separator |= 1u << offset;
                    break;
                case '.':
                    _dot |= 1u << offset;
                    break;
            }
        }

        separator = _separator;
        return _dot;
    }

    private static ulong InitializeSeparatorDot(ref ushort text, int textLength, out ulong separator)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(textLength, nameof(textLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textLength, 64, nameof(textLength));
        ulong _dot = default, _separator = default;
        int offset = default;
        if (BitConverter.IsLittleEndian)
        {
            if (Vector512.IsHardwareAccelerated)
            {
                for (; offset + Vector512<ushort>.Count <= textLength; offset += Vector512<ushort>.Count)
                {
                    var v = Vector512.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector512.Narrow(Vector512.Equals(v, Vector512.Create((ushort)'/')), Vector512.Equals(v, Vector512.Create((ushort)'.'))).ExtractMostSignificantBits();
                    _separator |= ((ulong)(uint)compoundSeparatorDot) << offset;
                    _dot |= (compoundSeparatorDot >>> 32) << offset;
                }
            }
            if (Vector256.IsHardwareAccelerated)
            {
                for (; offset + Vector256<ushort>.Count <= textLength; offset += Vector256<ushort>.Count)
                {
                    var v = Vector256.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector256.Narrow(Vector256.Equals(v, Vector256.Create((ushort)'/')), Vector256.Equals(v, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
                    _separator |= ((ulong)(compoundSeparatorDot & 0xffffu)) << offset;
                    _dot |= ((ulong)((compoundSeparatorDot & 0xffff0000u) >>> 16)) << offset;
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                for (; offset + Vector128<ushort>.Count <= textLength; offset += Vector128<ushort>.Count)
                {
                    var v = Vector128.LoadUnsafe(ref text, (nuint)offset);
                    var compoundSeparatorDot = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                    _separator |= ((ulong)(compoundSeparatorDot & 0xffu)) << offset;
                    _dot |= ((ulong)((compoundSeparatorDot & 0xff00u) >>> 8)) << offset;
                }
            }
        }

        for (; offset < textLength; ++offset)
        {
            switch (Unsafe.Add(ref text, offset))
            {
                case '/':
                    _separator |= 1ul << offset;
                    break;
                case '.':
                    _dot |= 1ul << offset;
                    break;
            }
        }

        separator = _separator;
        return _dot;
    }

    private ref struct UnixInfo
    {
        private readonly ref ushort text;
        private readonly int textLength;
        private readonly ref int offsetRef;
        private readonly ref int lengthRef;
        private int segmentCount;
        public readonly bool IsSegmentEmpty => segmentCount == 0;
        public readonly bool IsSeparatorOnly => StartsWithSeparator && segmentCount == 1;
        private bool startsWithCurrent;
        public readonly bool StartsWithSeparator;
        public bool EndsWithSeparator;
        private int notParentCount;
        public int Offset;
        public readonly int TotalLength => Sum(ref lengthRef, segmentCount) + segmentCount - (!EndsWithSeparator ? 1 : 0);
        public readonly bool IsInRange => Offset < textLength;

#if DEBUG
        private readonly int segmentCapacity;
#endif

        public readonly void SetLastSegmentEndExclusive(int endExclusive) => Unsafe.Add(ref lengthRef, segmentCount - 1) = endExclusive - Unsafe.Add(ref offsetRef, segmentCount - 1);

        public UnixInfo(ref ushort text, int textLength, Span<int> segmentSpan)
        {
            this.text = ref text;
            this.textLength = textLength;
            offsetRef = ref MemoryMarshal.GetReference(segmentSpan);
            lengthRef = ref Unsafe.Add(ref offsetRef, segmentSpan.Length >>> 1);
#if DEBUG
            segmentCapacity = segmentSpan.Length >>> 1;
            Debug.Assert(segmentCapacity > 0);
#endif
            startsWithCurrent = false;
            EndsWithSeparator = true;
            if (StartsWithSeparator = text == '/')
            {
                Offset = 1;
                offsetRef = 0;
                lengthRef = 0;
                segmentCount = 1;
            }
            else
            {
                Offset = 0;
                segmentCount = 0;
            }
        }

        public void AddNormalSegment(int length)
        {
#if DEBUG
            Debug.Assert(Offset + length <= textLength);
            Debug.Assert(segmentCount < segmentCapacity);
#endif
            Unsafe.Add(ref offsetRef, segmentCount) = Offset;
            Unsafe.Add(ref lengthRef, segmentCount++) = length;
            ++notParentCount;
        }

        public void TryAddCurrentSegment()
        {
            if (IsSegmentEmpty)
            {
#if DEBUG
                Debug.Assert(Offset + 1 <= textLength);
                Debug.Assert(segmentCount < segmentCapacity);
#endif
                offsetRef = Offset;
                lengthRef = 1;
                segmentCount = 1;
                startsWithCurrent = true;
            }
        }

        public void TryAddParentSegment()
        {
            if (notParentCount > 0)
            {
                --notParentCount;
                if (--segmentCount == 0 && startsWithCurrent)
                {
#if DEBUG
                    Debug.Assert(Offset + 2 <= textLength);
#endif
                    startsWithCurrent = false;
                    offsetRef = Offset;
                    lengthRef = 2;
                    segmentCount = 1;
                }
                return;
            }

            switch (segmentCount)
            {
                case 0:
#if DEBUG
                    Debug.Assert(Offset + 2 <= textLength);
                    Debug.Assert(segmentCount < segmentCapacity);
#endif
                    offsetRef = Offset;
                    lengthRef = 2;
                    segmentCount = 1;
                    return;
                case 1:
                    if (StartsWithSeparator)
                    {
                        return;
                    }
                    break;
            }

            ref var oldLength = ref Unsafe.Add(ref lengthRef, segmentCount - 1);
            if (oldLength + Unsafe.Add(ref offsetRef, segmentCount - 1) + 1 == Offset)
            {
                oldLength += 3;
            }
            else
            {
#if DEBUG
                Debug.Assert(segmentCount < segmentCapacity);
#endif
                Unsafe.Add(ref offsetRef, segmentCount) = Offset;
                Unsafe.Add(ref lengthRef, segmentCount++) = 2;
            }
        }

        public static void Create(Span<char> span, UnixInfo arg)
        {
            ref byte destination = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));
            int destinationOffset = 0;
            for (int segmentIndex = 0; segmentIndex < arg.segmentCount;)
            {
                var offset = Unsafe.Add(ref arg.offsetRef, segmentIndex);
                var length = Unsafe.Add(ref arg.lengthRef, segmentIndex);
                while (++segmentIndex < arg.segmentCount && offset + length + 1 == Unsafe.Add(ref arg.offsetRef, segmentIndex))
                {
                    length += Unsafe.Add(ref arg.lengthRef, segmentIndex) + 1;
                }

                if (arg.EndsWithSeparator || segmentIndex != arg.segmentCount)
                {
                    length++;
                }

                Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref destination, destinationOffset), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref arg.text, offset)), (uint)(length << 1));
                destinationOffset += length << 1;
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

        public override readonly string? ToString()
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
            else if (answerLength == textLength)
            {
                return default;
            }

            return string.Create(answerLength, this, UnixInfo.Create);
        }
    }
}
