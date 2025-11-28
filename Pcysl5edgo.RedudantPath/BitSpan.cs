using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedudantPath;

public static class BitSpan
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ref ulong array, int bitOffset) => (Unsafe.Add(ref array, bitOffset >>> 6) & (1ul << (bitOffset & 63))) != default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ulong array, int bitOffset) => ((array >>> bitOffset) & 1ul) != default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBitTrue(ref ulong array, int bitOffset) => Unsafe.Add(ref array, bitOffset >>> 6) |= 1ul << (bitOffset & 63);

    public static int TrailingZeroCount(ulong array, int bitLength, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitLength);
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitLength, 64);
        if (bitOffset < bitLength)
        {
            var temp = array >>> bitOffset;
            if (temp != default)
            {
                var answer = BitOperations.TrailingZeroCount(temp) + bitOffset;
                if (answer < bitLength)
                {
                    return answer;
                }
            }
        }

        return bitLength;
    }

    public static int TrailingOneCount(uint array, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        var temp = (~array) >>> bitOffset;
        if (temp != default)
        {
            var answer = BitOperations.TrailingZeroCount(temp) + bitOffset;
            if (answer < 32)
            {
                return answer;
            }
        }

        return 32;
    }

    public static int TrailingOneCount(ulong array, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        var temp = (~array) >>> bitOffset;
        if (temp != default)
        {
            var answer = BitOperations.TrailingZeroCount(temp) + bitOffset;
            if (answer < 64)
            {
                return answer;
            }
        }

        return 64;
    }

    public static int TrailingOneCount(ulong array, int bitLength, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitLength);
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitLength, 64);
        if (bitOffset < bitLength)
        {
            var temp = (~array >>> bitOffset);
            if (temp != default)
            {
                var answer = BitOperations.TrailingZeroCount(temp) + bitOffset;
                if (answer < bitLength)
                {
                    return answer;
                }
            }
        }

        return bitLength;
    }

    public static int TrailingZeroCount(ref ulong array, int bitLength, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitLength);
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        if (bitOffset >= bitLength)
        {
            goto END;
        }

        var arrayLengthMinus1 = ((bitLength + 63) >>> 6) - 1;
        if (arrayLengthMinus1 == 0)
        {
            return TrailingZeroCount(array, bitLength, bitOffset);
        }

        var arrayOffset = bitOffset >>> 6;
        var temp = Unsafe.Add(ref array, arrayOffset) >>> (bitOffset & 63);
        if (temp != default)
        {
            return (arrayOffset << 6) + BitOperations.TrailingZeroCount(temp) + (bitOffset & 63);
        }

        for (int arrayIndex = arrayOffset + 1; arrayIndex < arrayLengthMinus1; ++arrayIndex)
        {
            temp = Unsafe.Add(ref array, arrayIndex);
            if (temp != default)
            {
                return (arrayIndex << 6) + BitOperations.TrailingZeroCount(temp);
            }
        }

        temp = Unsafe.Add(ref array, arrayLengthMinus1) & (ulong.MaxValue >>> (bitLength & 63));
        if (temp != default)
        {
            var answer = (arrayLengthMinus1 << 6) + BitOperations.TrailingZeroCount(temp);
            if (answer < bitLength)
            {
                return answer;
            }
        }

    END:
        return bitLength;
    }

    public static int TrailingOneCount(ref ulong array, int bitLength, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitLength);
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        if (bitOffset >= bitLength)
        {
            goto END;
        }

        var arrayLengthMinus1 = ((bitLength + 63) >>> 6) - 1;
        if (arrayLengthMinus1 == 0)
        {
            return TrailingOneCount(array, bitLength, bitOffset);
        }

        var arrayOffset = bitOffset >>> 6;
        var temp = (~Unsafe.Add(ref array, arrayOffset)) >>> (bitOffset & 63);
        if (temp != default)
        {
            return (arrayOffset << 6) + BitOperations.TrailingZeroCount(temp) + (bitOffset & 63);
        }

        for (int arrayIndex = arrayOffset + 1; arrayIndex < arrayLengthMinus1; ++arrayIndex)
        {
            temp = ~Unsafe.Add(ref array, arrayIndex);
            if (temp != default)
            {
                return (arrayIndex << 6) + BitOperations.TrailingZeroCount(temp);
            }
        }

        temp = (~Unsafe.Add(ref array, arrayLengthMinus1)) & (ulong.MaxValue >>> (bitLength & 63));
        if (temp != default)
        {
            var answer = (arrayLengthMinus1 << 6) + BitOperations.TrailingZeroCount(temp);
            if (answer < bitLength)
            {
                return answer;
            }
        }

    END:
        return bitLength;
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
}
