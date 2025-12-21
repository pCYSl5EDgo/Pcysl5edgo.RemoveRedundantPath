using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Pcysl5edgo.RedundantPath;

public static class BitSpan
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ulong array, int bitOffset)
    {
        return ((array >>> bitOffset) & 1ul) != default;
    }

    /// <summary>
    /// Retrieves the value of the bit at the specified offset in the given 32-bit unsigned integer.
    /// </summary>
    /// <param name="array">The 32-bit unsigned integer from which to extract the bit value.</param>
    /// <param name="bitOffset">The zero-based position(mod 32) of the bit to retrieve.</param>
    /// <returns>true if the bit at the specified offset is set; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(uint array, int bitOffset)
    {
        return ((array >>> bitOffset) & 1u) != default;
    }

    public static string ToString(ulong bitArray, int bitLength)
    {
        return string.Create(bitLength, bitArray, static (span, bitArray) =>
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = (bitArray & (1ul << i)) != default ? '1' : '0';
            }
        });
    }

    public static string ToString(ref ulong bitArray, int bitLength)
    {
        return string.Create(bitLength, MemoryMarshal.CreateReadOnlySpan(ref bitArray, (bitLength + 63) >>> 6), static (span, bitArraySpan) =>
        {
            for (int i = 0; i < span.Length; ++i)
            {
                span[i] = (bitArraySpan[i >>> 6] & (1ul << (i & 63))) != default ? '1' : '0';
            }
        });
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot)
    {
        Debug.Assert(source.Length >= 32);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot);
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot, int length)
    {
        Debug.Assert(source.Length >= length);
        Debug.Assert(length > 0);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot, length);
    }

    /// <summary>
    /// Scans up to 32 UTF-16 code units for the '.' and '/' characters and returns bitmasks indicating their positions.
    /// </summary>
    /// <remarks>Each bit in the returned value and in the <paramref name="dot"/> output corresponds to a position in the first 32 UTF-16 code units starting at <paramref name="source"/>.
    /// Bit 0 represents the first code unit, bit 1 the second, and so on.
    /// If the input contains fewer than 32 code units, behavior is  undefined.</remarks>
    /// <param name="source">A reference to the first element of a span of at least 32 UTF-16 code units to scan.</param>
    /// <param name="dot">When this method returns, contains a bitmask where each set bit indicates the position of a '.' character in the input.</param>
    /// <returns>A bitmask where each set bit indicates the position of a '/' character in the input.</returns>
    public static uint Get(ref ushort source, out uint dot)
    {
        if (Vector512.IsHardwareAccelerated)
        {
            var v = Vector512.LoadUnsafe(ref source);
            var compound = Vector512.Narrow(Vector512.Equals(v, Vector512.Create((ushort)'.')), Vector512.Equals(v, Vector512.Create((ushort)'/'))).ExtractMostSignificantBits();
            dot = (uint)compound;
            return (uint)(compound >>> 32);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var d = Vector256.Create((ushort)'.');
            dot = Vector256.Narrow(Vector256.Equals(v0, d), Vector256.Equals(v1, d)).ExtractMostSignificantBits();
            var s = Vector256.Create((ushort)'/');
            return Vector256.Narrow(Vector256.Equals(v0, s), Vector256.Equals(v1, s)).ExtractMostSignificantBits();
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var v0 = Vector128.LoadUnsafe(ref source);
            var v1 = Vector128.LoadUnsafe(ref source, 8);
            var v2 = Vector128.LoadUnsafe(ref source, 16);
            var v3 = Vector128.LoadUnsafe(ref source, 24);
            var d = Vector128.Create((ushort)'.');
            var d0 = Vector128.Narrow(Vector128.Equals(v0, d), Vector128.Equals(v1, d)).ExtractMostSignificantBits();
            var d1 = Vector128.Narrow(Vector128.Equals(v2, d), Vector128.Equals(v3, d)).ExtractMostSignificantBits();
            dot = d0 | (d1 << 16);
            var s = Vector128.Create((ushort)'/');
            var s0 = Vector128.Narrow(Vector128.Equals(v0, s), Vector128.Equals(v1, s)).ExtractMostSignificantBits();
            var s1 = Vector128.Narrow(Vector128.Equals(v2, s), Vector128.Equals(v3, s)).ExtractMostSignificantBits();
            return s0 | (s1 << 16);
        }
        else
        {
            uint _separator = default, _dot = default;
            for (int i = 0; i < 32; ++i)
            {
                switch (Unsafe.Add(ref source, i))
                {
                    case '.':
                        _dot |= 1u << i;
                        break;
                    case '/':
                        _separator |= 1u << i;
                        break;
                }
            }

            dot = _dot;
            return _separator;
        }
    }

    /// <summary>
    /// Scans up to 32 or less UTF-16 code units for the '.' and '/' characters and returns bitmasks indicating their positions.
    /// </summary>
    /// <remarks>Each bit in the returned value and in the <paramref name="dot"/> output corresponds to a position in the first 32 or less UTF-16 code units starting at <paramref name="source"/>.
    /// Bit 0 represents the first code unit, bit 1 the second, and so on.</remarks>
    /// <param name="source">A reference to the first element of a span of 32 or less UTF-16 code units to scan.</param>
    /// <param name="dot">When this method returns, contains a bitmask where each set bit indicates the position of a '.' character in the input.</param>
    /// <param name="length">The number of characters to scan. Must be between 1 and 32, inclusive.</param>
    /// <returns>A bitmask with bits set at positions where a '/' character was found in the input sequence.</returns>
    public static uint Get(ref ushort source, out uint dot, int length)
    {
        Debug.Assert((uint)(length - 1) < 31u);
        uint separator = 0, _dot = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated && length >= 16)
        {
            for (; i + Vector128<ushort>.Count < length; i += Vector128<ushort>.Count)
            {
                var v = Vector128.LoadUnsafe(ref source, (nuint)i);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                _dot |= (compound >>> 8) << i;
                separator |= ((uint)(byte)compound) << i;
            }

            {
                var offset = length - Vector128<ushort>.Count;
                var v = Vector128.LoadUnsafe(ref source, (nuint)offset);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                dot = _dot | ((compound >>> 8) << offset);
                return separator | (((uint)(byte)compound) << offset);
            }
        }

        for (; i < length; ++i)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '/':
                    separator |= 1u << i;
                    break;
                case '.':
                    _dot |= 1u << i;
                    break;
            }
        }

        dot = _dot;
        return separator;
    }

    public static ulong Get(ReadOnlySpan<char> source, out ulong dot)
    {
        Debug.Assert(source.Length >= 32);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot);
    }

    public static ulong Get(ReadOnlySpan<char> source, out ulong dot, int length)
    {
        Debug.Assert(source.Length >= length);
        Debug.Assert(length > 0);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot, length);
    }

    public static ulong Get(ref ushort source, out ulong dot)
    {
        if (Vector512.IsHardwareAccelerated)
        {
            var v0 = Vector512.LoadUnsafe(ref source);
            var v1 = Vector512.LoadUnsafe(ref source, 32);
            dot = Vector512.Narrow(Vector512.Equals(v0, Vector512.Create((ushort)'.')), Vector512.Equals(v1, Vector512.Create((ushort)'.'))).ExtractMostSignificantBits();
            return Vector512.Narrow(Vector512.Equals(v0, Vector512.Create((ushort)'/')), Vector512.Equals(v1, Vector512.Create((ushort)'/'))).ExtractMostSignificantBits();
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var v2 = Vector256.LoadUnsafe(ref source, 32);
            var v3 = Vector256.LoadUnsafe(ref source, 48);
            var d0 = (ulong)Vector256.Narrow(Vector256.Equals(v0, Vector256.Create((ushort)'.')), Vector256.Equals(v1, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
            var d1 = (ulong)Vector256.Narrow(Vector256.Equals(v2, Vector256.Create((ushort)'.')), Vector256.Equals(v3, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
            var s0 = (ulong)Vector256.Narrow(Vector256.Equals(v0, Vector256.Create((ushort)'/')), Vector256.Equals(v1, Vector256.Create((ushort)'/'))).ExtractMostSignificantBits();
            var s1 = (ulong)Vector256.Narrow(Vector256.Equals(v2, Vector256.Create((ushort)'/')), Vector256.Equals(v3, Vector256.Create((ushort)'/'))).ExtractMostSignificantBits();
            dot = d0 | (d1 << 32);
            return s0 | (s1 << 32);
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var v0 = Vector128.LoadUnsafe(ref source);
            var v1 = Vector128.LoadUnsafe(ref source, 8);
            var v2 = Vector128.LoadUnsafe(ref source, 16);
            var v3 = Vector128.LoadUnsafe(ref source, 24);
            var v4 = Vector128.LoadUnsafe(ref source, 32);
            var v5 = Vector128.LoadUnsafe(ref source, 40);
            var v6 = Vector128.LoadUnsafe(ref source, 48);
            var v7 = Vector128.LoadUnsafe(ref source, 56);
            var d0 = (ulong)Vector128.Narrow(Vector128.Equals(v0, Vector128.Create((ushort)'.')), Vector128.Equals(v1, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
            var d1 = (ulong)Vector128.Narrow(Vector128.Equals(v2, Vector128.Create((ushort)'.')), Vector128.Equals(v3, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
            var d2 = (ulong)Vector128.Narrow(Vector128.Equals(v4, Vector128.Create((ushort)'.')), Vector128.Equals(v5, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
            var d3 = (ulong)Vector128.Narrow(Vector128.Equals(v6, Vector128.Create((ushort)'.')), Vector128.Equals(v7, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
            var s0 = (ulong)Vector128.Narrow(Vector128.Equals(v0, Vector128.Create((ushort)'/')), Vector128.Equals(v1, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits();
            var s1 = (ulong)Vector128.Narrow(Vector128.Equals(v2, Vector128.Create((ushort)'/')), Vector128.Equals(v3, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits();
            var s2 = (ulong)Vector128.Narrow(Vector128.Equals(v4, Vector128.Create((ushort)'/')), Vector128.Equals(v5, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits();
            var s3 = (ulong)Vector128.Narrow(Vector128.Equals(v6, Vector128.Create((ushort)'/')), Vector128.Equals(v7, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits();
            dot = d0 | (d1 << 16) | (d2 << 32) | (d3 << 48);
            return s0 | (s1 << 16) | (s2 << 32) | (s3 << 48);
        }
        else
        {
            ulong separator = default, _dot = default;
            for (int i = 0; i < 64; i++)
            {
                switch (Unsafe.Add(ref source, i))
                {
                    case '/':
                        separator |= 1u << i;
                        break;
                    case '.':
                        _dot |= 1u << i;
                        break;
                }
            }

            dot = _dot;
            return separator;
        }
    }

    public static ulong Get(ref ushort source, out ulong dot, int length)
    {
        if (length >= 32)
        {
            uint s0 = Get(ref source, out uint d0);
            if (length == 32)
            {
                dot = d0;
                return s0;
            }
            else
            {
                uint s1 = Get(ref Unsafe.Add(ref source, 32), out uint d1, length & 31);
                dot = d0 | ((ulong)d1 << 32);
                return s0 | ((ulong)s1 << 32);
            }
        }
        else
        {
            uint s0 = Get(ref source, out uint d0, length);
            dot = d0;
            return s0;
        }
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot, out uint altSeparator)
    {
        Debug.Assert(source.Length >= 32);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot, out altSeparator);
    }

    public static uint Get(ReadOnlySpan<char> source, out uint dot, out uint altSeparator, int length)
    {
        Debug.Assert(source.Length >= length);
        Debug.Assert((uint)(length - 1) < 31u);
        return Get(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(source)), out dot, out altSeparator, length);
    }

    public static uint Get(ref ushort source, out uint dot, out uint altSeparator)
    {
        if (Vector512.IsHardwareAccelerated)
        {
            var v = Vector512.LoadUnsafe(ref source);
            var vSlash = Vector512.Equals(v, Vector512.Create((ushort)'/'));
            var vBackslash = Vector512.Equals(v, Vector512.Create((ushort)'\\'));
            var vDot = Vector512.Equals(v, Vector512.Create((ushort)'.'));
            var compound = Vector512.Narrow(vSlash | vBackslash, vDot).ExtractMostSignificantBits();
            altSeparator = (uint)(Bmi2.X64.IsSupported
                ? Bmi2.X64.ParallelBitExtract(vSlash.ExtractMostSignificantBits(), 0x5555555555555555ul)
                : Vector512.Narrow(vSlash, Vector512<ushort>.Zero).ExtractMostSignificantBits());
            dot = (uint)(compound >>> 32);
            return (uint)compound;
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var vSlash = Vector256.Create((ushort)'/');
            var vBackslash = Vector256.Create((ushort)'\\');
            var vDot = Vector256.Create((ushort)'.');
            var narrowedSlash = Vector256.Narrow(Vector256.Equals(v0, vSlash), Vector256.Equals(v1, vSlash));
            var narrowedBackslash = Vector256.Narrow(Vector256.Equals(v0, vBackslash), Vector256.Equals(v1, vBackslash));
            altSeparator = narrowedSlash.ExtractMostSignificantBits();
            dot = Vector256.Narrow(Vector256.Equals(v0, vDot), Vector256.Equals(v1, vDot)).ExtractMostSignificantBits();
            return (narrowedSlash | narrowedBackslash).ExtractMostSignificantBits();
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            var v0 = Vector128.LoadUnsafe(ref source);
            var v1 = Vector128.LoadUnsafe(ref source, 8);
            var v2 = Vector128.LoadUnsafe(ref source, 16);
            var v3 = Vector128.LoadUnsafe(ref source, 24);
            var vSlash = Vector128.Create((ushort)'/');
            var vBackslash = Vector128.Create((ushort)'\\');
            var vDot = Vector128.Create((ushort)'.');
            var narrowedSlash0 = Vector128.Narrow(Vector128.Equals(v0, vSlash), Vector128.Equals(v1, vSlash));
            var narrowedBackslash0 = Vector128.Narrow(Vector128.Equals(v0, vBackslash), Vector128.Equals(v1, vBackslash));
            var separator0 = Vector128.BitwiseOr(narrowedSlash0, narrowedBackslash0).ExtractMostSignificantBits();
            var narrowedSlash1 = Vector128.Narrow(Vector128.Equals(v2, vSlash), Vector128.Equals(v3, vSlash));
            var narrowedBackslash1 = Vector128.Narrow(Vector128.Equals(v2, vBackslash), Vector128.Equals(v3, vBackslash));
            var separator1 = (narrowedSlash1 | narrowedBackslash1).ExtractMostSignificantBits();
            var dot0 = Vector128.Narrow(Vector128.Equals(v0, vDot), Vector128.Equals(v1, vDot)).ExtractMostSignificantBits();
            var dot1 = Vector128.Narrow(Vector128.Equals(v2, vDot), Vector128.Equals(v3, vDot)).ExtractMostSignificantBits();
            dot = dot0 | (dot1 << 16);
            altSeparator = narrowedSlash0.ExtractMostSignificantBits() | (narrowedBackslash1.ExtractMostSignificantBits() << 16);
            return separator0 | (separator1 << 16);
        }

        uint separator = 0, _dot = 0, _altSeparator = 0;
        for (int i = 0; i < 32; i++)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '\\':
                    separator |= 1u << i;
                    break;
                case '/':
                    _altSeparator |= 1u << i;
                    separator |= 1u << i;
                    break;
                case '.':
                    _dot |= 1u << i;
                    break;
            }
        }

        dot = _dot;
        altSeparator = _altSeparator;
        return separator;
    }

    public static uint Get(ref ushort source, out uint dot, out uint altSeparator, int length)
    {
        Debug.Assert((uint)(length - 1) < 31u);
        uint separator = 0, _dot = 0, _altSeparator = 0;
        if (Vector128.IsHardwareAccelerated && length >= 16)
        {
            for (int i = 0; i + Vector128<ushort>.Count < length; i += Vector128<ushort>.Count)
            {
                var v = Vector128.LoadUnsafe(ref source, (nuint)i);
                var vSlash = Vector128.Equals(v, Vector128.Create((ushort)'/'));
                var vBackslash = Vector128.Equals(v, Vector128.Create((ushort)'\\'));
                var vDot = Vector128.Equals(v, Vector128.Create((ushort)'.'));
                var compound = Vector128.Narrow(vSlash | vBackslash, vDot).ExtractMostSignificantBits();
                _altSeparator |= (Bmi2.IsSupported ? Bmi2.ParallelBitExtract(vSlash.ExtractMostSignificantBits(), 0x5555) : Vector128.Narrow(vSlash, Vector128<ushort>.Zero).ExtractMostSignificantBits()) << i;
                _dot |= (compound >>> 8) << i;
                separator |= ((uint)(byte)compound) << i;
            }

            {
                var offset = length - Vector128<ushort>.Count;
                var v = Vector128.LoadUnsafe(ref source, (nuint)offset);
                var vSlash = Vector128.Equals(v, Vector128.Create((ushort)'/'));
                var vBackslash = Vector128.Equals(v, Vector128.Create((ushort)'\\'));
                var vDot = Vector128.Equals(v, Vector128.Create((ushort)'.'));
                var compound = Vector128.Narrow(vSlash | vBackslash, vDot).ExtractMostSignificantBits();
                altSeparator = _altSeparator | ((Bmi2.IsSupported ? Bmi2.ParallelBitExtract(vSlash.ExtractMostSignificantBits(), 0x5555) : Vector128.Narrow(vSlash, Vector128<ushort>.Zero).ExtractMostSignificantBits()) << offset);
                dot = _dot | ((compound >>> 8) << offset);
                return separator | (((uint)(byte)compound) << offset);
            }
        }

        for (int i = 0; i < length; i++)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '\\':
                    separator |= 1u << i;
                    break;
                case '/':
                    _altSeparator |= 1u << i;
                    separator |= 1u << i;
                    break;
                case '.':
                    _dot |= 1u << i;
                    break;
            }
        }

        altSeparator = _altSeparator;
        dot = _dot;
        return separator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CalculateUpperBitWall(int length, out uint wall)
    {
        Debug.Assert((uint)length <= 32u);
        wall = (uint.MaxValue >>> length) << length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CalculateUpperBitWall(int length, out ulong wall)
    {
        Debug.Assert((uint)length <= 64u);
        wall = (ulong.MaxValue >>> length) << length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ZeroHighBits(uint value, int index)
    {
        if (Bmi2.IsSupported)
        {
            return Bmi2.ZeroHighBits(value, (uint)(index & 31));
        }

        return value & (~(uint.MaxValue << index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ZeroHighBits(ulong value, int index)
    {
        if (Bmi2.X64.IsSupported)
        {
            return Bmi2.X64.ZeroHighBits(value, (ulong)(index & 63));
        }

        return value & (~(ulong.MaxValue << index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ResetLowestSetBit(uint value)
    {
        if (Bmi1.IsSupported)
        {
            return Bmi1.ResetLowestSetBit(value);
        }

        return value & (value - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ResetLowestSetBit(ulong value)
    {
        if (Bmi1.X64.IsSupported)
        {
            return Bmi1.X64.ResetLowestSetBit(value);
        }

        return value & (value - 1);
    }
}
