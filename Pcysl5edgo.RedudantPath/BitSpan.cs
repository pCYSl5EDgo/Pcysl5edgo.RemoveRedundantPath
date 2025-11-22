using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Pcysl5edgo.RemoveRedundantPath;

public static class BitSpan
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(ref byte array, int bitOffset) => (Unsafe.Add(ref array, bitOffset >>> 3) >>> (bitOffset & 7)) != default;

    public static int IndexOfTrue(ref byte array, int bitLength, int bitOffset)
    {
        if (bitOffset >= bitLength)
        {
            return -1;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(bitLength, nameof(bitLength));
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset, nameof(bitOffset));
        var arrayIndex = bitOffset >>> 3;
        if (arrayIndex == 0)
        {
            return IndexOfTrueInternal(ref array, bitLength, bitOffset);
        }

        var answer = IndexOfTrueInternal(ref Unsafe.Add(ref array, arrayIndex), bitLength - (arrayIndex << 3), bitOffset & 7);
        return answer + (answer >= 0 ? (arrayIndex << 3) : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(ref byte array, int bitOffset, bool value)
    {
        if (value)
        {
            SetBitTrue(ref array, bitOffset);
        }
        else
        {
            SetBitFalse(ref array, bitOffset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBitFalse(ref byte array, int bitOffset) => Unsafe.Add(ref array, bitOffset >>> 3) &= (byte)~(1 << (bitOffset & 7));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBitTrue(ref byte array, int bitOffset) => Unsafe.Add(ref array, bitOffset >>> 3) |= (byte)(1 << (bitOffset & 7));

    private static int IndexOfTrueInternal(ref byte array, int bitLength, int bitOffset)
    {
        var bits32 = array & (uint.MaxValue << bitOffset);
        if (bits32 != default)
        {
            return BitOperations.TrailingZeroCount(bits32);
        }
        else if ((bitLength -= 8) <= 0)
        {
            return -1;
        }

        bitOffset = 8;
        array = ref Unsafe.Add(ref array, 1);
        if (bitLength >= 64)
        {
            if (nuint.Size == 4)
            {
                var loopCount = bitLength >>> 5;
                for (int loopIndex = 0; loopIndex < loopCount; ++loopIndex, bitOffset += 32, array = ref Unsafe.Add(ref array, 4))
                {
                    bits32 = Unsafe.ReadUnaligned<uint>(ref array);
                    if (bits32 != default)
                    {
                        return bitOffset + BitOperations.TrailingZeroCount(BitConverter.IsLittleEndian ? bits32 : BinaryPrimitives.ReverseEndianness(bits32));
                    }
                }

                bitLength -= loopCount << 5;
            }
            else
            {
                var loopCount = bitLength >>> 6;
                for (int loopIndex = 0; loopIndex < loopCount; ++loopIndex, bitOffset += 64, array = ref Unsafe.Add(ref array, 8))
                {
                    var bits64 = Unsafe.ReadUnaligned<ulong>(ref array);
                    if (bits64 != default)
                    {
                        return bitOffset + BitOperations.TrailingZeroCount(BitConverter.IsLittleEndian ? bits64 : BinaryPrimitives.ReverseEndianness(bits64));
                    }
                }

                bitLength -= loopCount << 6;
            }

            if (bitLength == default)
            {
                return -1;
            }
        }
        {
            var byteLoopCount = bitLength >>> 3;
            for (int loopIndex = 0; loopIndex < byteLoopCount; ++loopIndex, bitOffset += 8, array = ref Unsafe.Add(ref array, 1))
            {
                bits32 = array;
                if (bits32 != default)
                {
                    return bitOffset + BitOperations.TrailingZeroCount(bits32);
                }
            }

            bitLength -= byteLoopCount << 3;
            if (bitLength == default)
            {
                return -1;
            }
        }

        bits32 = array & ((1u << bitLength) - 1u);
        if (bits32 == default)
        {
            return -1;
        }

        return bitOffset + BitOperations.TrailingZeroCount(bits32);
    }
}
