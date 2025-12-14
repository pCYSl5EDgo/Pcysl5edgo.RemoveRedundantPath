using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Pcysl5edgo.RedundantPath;

public static class BitSpan
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ulong array, int bitOffset)
    {
        return ((array >>> bitOffset) & 1ul) != default;
    }

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
    private static uint Get(ref ushort source, out uint dot)
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
    private static uint Get(ref ushort source, out uint dot, int length)
    {
        Debug.Assert((uint)(length - 1) <= 31u);
        uint separator = 0, _dot = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated && length >= 16)
        {
            for (; i + Vector128<ushort>.Count <= length; i += Vector128<ushort>.Count)
            {
                var v = Vector128.LoadUnsafe(ref source, (nuint)i);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                separator |= ((uint)(byte)compound) << i;
                _dot |= (compound >>> 8) << i;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CalculateUpperBitWall(int length, out uint wall)
    {
        Debug.Assert((uint)length <= 32u);
        wall = (uint.MaxValue >>> length) << length;
    }

    public static uint ZeroClearUpperBit(uint value, int clearLength)
    {
        return (uint)clearLength < 32u ? ((value << clearLength) >>> clearLength) : 0;
    }
}
