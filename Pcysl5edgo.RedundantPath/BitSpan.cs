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

    public static uint Get(ref ushort source, out uint dot, out uint backslash)
    {
        if (Vector512.IsHardwareAccelerated)
        {
            var v = Vector512.LoadUnsafe(ref source);
            dot = (uint)Vector512.Equals(v, Vector512.Create((ushort)'.')).ExtractMostSignificantBits();
            var compound = Vector512.Narrow(Vector512.Equals(v, Vector512.Create((ushort)'\\')), Vector512.Create((ushort)'/')).ExtractMostSignificantBits();
            backslash = (uint)compound;
            return (uint)(compound >>> 32);
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var d = Vector256.Create((ushort)'.');
            dot = Vector256.Narrow(Vector256.Equals(v0, d), Vector256.Equals(v1, d)).ExtractMostSignificantBits();
            var b = Vector256.Create((ushort)'\\');
            backslash = Vector256.Narrow(Vector256.Equals(v0, b), Vector256.Equals(v1, b)).ExtractMostSignificantBits();
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
            var b = Vector128.Create((ushort)'\\');
            var b0 = Vector128.Narrow(Vector128.Equals(v0, b), Vector128.Equals(v1, b)).ExtractMostSignificantBits();
            var b1 = Vector128.Narrow(Vector128.Equals(v2, b), Vector128.Equals(v3, b)).ExtractMostSignificantBits();
            backslash = b0 | (b1 << 16);
            var s = Vector128.Create((ushort)'/');
            var s0 = Vector128.Narrow(Vector128.Equals(v0, s), Vector128.Equals(v1, s)).ExtractMostSignificantBits();
            var s1 = Vector128.Narrow(Vector128.Equals(v2, s), Vector128.Equals(v3, s)).ExtractMostSignificantBits();
            return s0 | (s1 << 16);
        }
        else
        {
            uint _separator = default, _dot = default, _backslash = default;
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
                    case '\\':
                        _backslash |= 1u << i;
                        break;
                }
            }

            dot = _dot;
            backslash = _backslash;
            return _separator;
        }
    }

    public static uint Get(ref ushort source, out uint dot, int length)
    {
        Debug.Assert((uint)length <= 32u);
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

    public static uint Get(ref ushort source, out uint dot, out uint backslash, int length)
    {
        Debug.Assert((uint)length <= 32u);
        uint separator = 0, _dot = 0, _backslash = 0;
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
        backslash = _backslash;
        return separator;
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
        else
        if (Vector256.IsHardwareAccelerated)
        {
            var v0 = Vector256.LoadUnsafe(ref source);
            var v1 = Vector256.LoadUnsafe(ref source, 16);
            var v2 = Vector256.LoadUnsafe(ref source, 32);
            var v3 = Vector256.LoadUnsafe(ref source, 48);
            var d = Vector256.Create((ushort)'.');
            var d0 = (ulong)Vector256.Narrow(Vector256.Equals(v0, d), Vector256.Equals(v1, d)).ExtractMostSignificantBits();
            var d1 = (ulong)Vector256.Narrow(Vector256.Equals(v2, d), Vector256.Equals(v3, d)).ExtractMostSignificantBits();
            dot = d0 | (d1 << 32);
            var s = Vector256.Create((ushort)'/');
            var s0 = (ulong)Vector256.Narrow(Vector256.Equals(v0, s), Vector256.Equals(v1, s)).ExtractMostSignificantBits();
            var s1 = (ulong)Vector256.Narrow(Vector256.Equals(v2, s), Vector256.Equals(v3, s)).ExtractMostSignificantBits();
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
            var d = Vector128.Create((ushort)'.');
            var d0 = (ulong)Vector128.Narrow(Vector128.Equals(v0, d), Vector128.Equals(v1, d)).ExtractMostSignificantBits();
            var d1 = (ulong)Vector128.Narrow(Vector128.Equals(v2, d), Vector128.Equals(v3, d)).ExtractMostSignificantBits();
            var d2 = (ulong)Vector128.Narrow(Vector128.Equals(v4, d), Vector128.Equals(v5, d)).ExtractMostSignificantBits();
            var d3 = (ulong)Vector128.Narrow(Vector128.Equals(v6, d), Vector128.Equals(v7, d)).ExtractMostSignificantBits();
            dot = d0 | (d1 << 16) | (d2 << 32) | (d3 << 48);
            var s = Vector128.Create((ushort)'/');
            var s0 = (ulong)Vector128.Narrow(Vector128.Equals(v0, s), Vector128.Equals(v1, s)).ExtractMostSignificantBits();
            var s1 = (ulong)Vector128.Narrow(Vector128.Equals(v2, s), Vector128.Equals(v3, s)).ExtractMostSignificantBits();
            var s2 = (ulong)Vector128.Narrow(Vector128.Equals(v4, s), Vector128.Equals(v5, s)).ExtractMostSignificantBits();
            var s3 = (ulong)Vector128.Narrow(Vector128.Equals(v6, s), Vector128.Equals(v7, s)).ExtractMostSignificantBits();
            return s0 | (s1 << 16) | (s2 << 32) | (s3 << 48);
        }
        else
        {
            ulong _separator = default, _dot = default;
            for (int i = 0; i < 64; ++i)
            {
                switch (Unsafe.Add(ref source, i))
                {
                    case '.':
                        _dot |= 1ul << i;
                        break;
                    case '/':
                        _separator |= 1ul << i;
                        break;
                }
            }

            dot = _dot;
            return _separator;
        }
    }

    public static ulong Get(ref ushort source, out ulong dot, int length)
    {
        Debug.Assert((uint)length <= 64u);
        ulong separator = 0, _dot = 0;
        int i = 0;
        if (Vector128.IsHardwareAccelerated && length >= 16)
        {
            for (; i + Vector128<ushort>.Count <= length; i += Vector128<ushort>.Count)
            {
                var v = Vector128.LoadUnsafe(ref source, (nuint)i);
                var compound = Vector128.Narrow(Vector128.Equals(v, Vector128.Create((ushort)'/')), Vector128.Equals(v, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                separator |= ((ulong)(byte)compound) << i;
                _dot |= ((ulong)(compound >>> 8)) << i;
            }
        }

        for (; i < length; ++i)
        {
            switch (Unsafe.Add(ref source, i))
            {
                case '/':
                    separator |= 1ul << i;
                    break;
                case '.':
                    _dot |= 1ul << i;
                    break;
            }
        }

        dot = _dot;
        return separator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong CalculateUpperBitWall64(int length)
    {
        return (ulong.MaxValue >>> length) << length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateUpperBitWall32(int length)
    {
        Debug.Assert((uint)length <= 32u);
        return (uint.MaxValue >>> length) << length;
    }

    public static ulong ZeroClearUpperBit(ulong value, int clearLength)
    {
        return (uint)clearLength < 64u ? ((value << clearLength) >>> clearLength) : 0;
    }

    public static uint ZeroClearUpperBit(uint value, int clearLength)
    {
        return (uint)clearLength < 32u ? ((value << clearLength) >>> clearLength) : 0;
    }
}
