using System.Buffers.Binary;
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
        var lengthSpan = (stackalloc int[span.Count('/') + 1]);
        var offsetSpan = (stackalloc int[lengthSpan.Length]);
        ref var lengthRef = ref MemoryMarshal.GetReference(lengthSpan);
        ref var offsetRef = ref MemoryMarshal.GetReference(offsetSpan);
        var notParentCount = 0;
        var segmentCount = 0;
        var endsWithSeparator = false;
        var offset = 0;
        var startsWithSeparator = MemoryMarshal.GetReference(span) == '/';
        if (startsWithSeparator)
        {
            offset = 1;
            span = span[1..];
            segmentCount = 1;
            lengthRef = 0;
            offsetRef = 0;
        }

        do
        {
            var length = span.IndexOf('/');
            if (length < 0)
            {
                if (segmentCount == 0 && offset == 0)
                {
                    return path;
                }

                endsWithSeparator = false;
                switch (span.Length)
                {
                    case 1:
                        if (MemoryMarshal.GetReference(span) != '.')
                        {
                            goto default;
                        }
                        else if (segmentCount == 0)
                        {
                            return ".";
                        }

                        break;
                    case 2:
                        var char2 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(MemoryMarshal.AsBytes(span))));
                        if (char2 != ('.' | ((uint)'.' << 16)))
                        {
                            goto default;
                        }
                        else if (segmentCount == 0)
                        {
                            lengthRef = 2;
                            offsetRef = offset;
                            segmentCount = 1;
                        }
                        else if (startsWithSeparator && segmentCount == 1)
                        {
                            segmentCount = 1;
                        }
                        else if (notParentCount == 0)
                        {
                            ref var oldLength = ref Unsafe.Add(ref lengthRef, segmentCount - 1);
                            if (Unsafe.Add(ref offsetRef, segmentCount - 1) + oldLength + 1 == offset)
                            {
                                oldLength += 3;
                            }
                            else
                            {
                                Unsafe.Add(ref lengthRef, segmentCount) = 2;
                                Unsafe.Add(ref offsetRef, segmentCount) = offset;
                                ++segmentCount;
                            }
                        }
                        else
                        {
                            --notParentCount;
                            --segmentCount;
                        }
                        break;
                    default:
                        ++notParentCount;
                        Unsafe.Add(ref lengthRef, segmentCount) = span.Length;
                        Unsafe.Add(ref offsetRef, segmentCount++) = offset;
                        break;
                }
                break;
            }
            else if (length == 0) // consecutive separators
            {
                span = span[1..];
                ++offset;
                // When segmentCount is 0, I have already set `startsWithSeparator` to true.
            }
            else
            {
                switch (length)
                {
                    case 1:
                        if (MemoryMarshal.GetReference(span) != '.')
                        {
                            goto default;
                        }
                        else if (segmentCount == 0)
                        {
                            lengthRef = 1;
                            offsetRef = 0;
                            notParentCount = 1;
                            segmentCount = 1;
                        }
                        break;
                    case 2:
                        var char2 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(MemoryMarshal.AsBytes(span))));
                        if (char2 != ('.' | ((uint)'.' << 16)))
                        {
                            goto default;
                        }
                        else if (segmentCount == 0)
                        {
                            lengthRef = 2;
                            offsetRef = offset;
                            segmentCount = 1;
                        }
                        else if (startsWithSeparator && segmentCount == 1)
                        {
                            segmentCount = 1;
                        }
                        else if (notParentCount == 0)
                        {
                            ref var oldLength = ref Unsafe.Add(ref lengthRef, segmentCount - 1);
                            if (Unsafe.Add(ref offsetRef, segmentCount - 1) + oldLength + 1 == offset)
                            {
                                oldLength += 3;
                            }
                            else
                            {
                                Unsafe.Add(ref lengthRef, segmentCount) = 2;
                                Unsafe.Add(ref offsetRef, segmentCount) = offset;
                                ++segmentCount;
                            }
                        }
                        else
                        {
                            --notParentCount;
                            --segmentCount;
                        }
                        break;
                    default:
                        ++notParentCount;
                        Unsafe.Add(ref lengthRef, segmentCount) = length;
                        Unsafe.Add(ref offsetRef, segmentCount++) = offset;
                        break;
                }

                endsWithSeparator = true;
                span = span[++length..];
                offset += length;
            }
        } while (!span.IsEmpty);
        if (segmentCount == 1 && startsWithSeparator)
        {
            return "/";
        }

        var answerLength = Sum(ref lengthRef, segmentCount) + segmentCount - (!endsWithSeparator ? 1 : 0);
        if (answerLength <= 0)
        {
            return "";
        }
        else if (answerLength == path.Length)
        {
            return path;
        }

        var temp = new CreateStructure(path.AsSpan(), offsetSpan[..segmentCount], ref lengthRef, endsWithSeparator);
        return string.Create(answerLength, temp, CreateStructure.Create);
    }

    private readonly ref struct CreateStructure
    {
        public readonly ReadOnlySpan<char> Text;
        public readonly ReadOnlySpan<int> Offset;
        public readonly ref int LengthRef;
        public readonly bool EndsWithSeparator;

        public CreateStructure(ReadOnlySpan<char> text, ReadOnlySpan<int> offsetSpan, ref int lengthRef, bool endsWithSeparator)
        {
            Text = text;
            Offset = offsetSpan;
            LengthRef = ref lengthRef;
            EndsWithSeparator = endsWithSeparator;
        }

        public static void Create(Span<char> span, CreateStructure arg)
        {
            for (int segmentIndex = 0; segmentIndex < arg.Offset.Length;)
            {
                var slice = arg.Text.Slice(arg.Offset[segmentIndex], Unsafe.Add(ref arg.LengthRef, segmentIndex));
                slice.CopyTo(span);
                span = span[slice.Length..];
                if (++segmentIndex < arg.Offset.Length || arg.EndsWithSeparator)
                {
                    MemoryMarshal.GetReference(span) = '/';
                    span = span[1..];
                }
            }
        }
    }

    /// <param name="source">Text span</param>
    /// <param name="sourceLength">Text span length</param>
    /// <param name="dots">Dot bit array</param>
    /// <param name="separators">Separator bit array</param>
    /// <returns>Separator Total Count</returns>
    private static int InitializeDotAndSepartorBitArray(ref ushort source, uint sourceLength, ref byte dots, ref byte separators)
    {
        int validSeparatorTotalCount = 0;
        uint index = 0;
        if (Vector256.IsHardwareAccelerated)
        {
            const uint stride = 32;
            for (; index + stride <= sourceLength; index += stride)
            {
                var t0 = Vector256.LoadUnsafe(ref source, index);
                var t1 = Vector256.LoadUnsafe(ref source, index + (uint)Vector256<ushort>.Count);
                var separatorBit = Vector256.Narrow(Vector256.Equals(t0, Vector256.Create((ushort)'/')), Vector256.Equals(t1, Vector256.Create((ushort)'/'))).ExtractMostSignificantBits();
                var dotBit = Vector256.Narrow(Vector256.Equals(t0, Vector256.Create((ushort)'.')), Vector256.Equals(t1, Vector256.Create((ushort)'.'))).ExtractMostSignificantBits();
                if (!BitConverter.IsLittleEndian)
                {
                    separatorBit = BinaryPrimitives.ReverseEndianness(separatorBit);
                    dotBit = BinaryPrimitives.ReverseEndianness(dotBit);
                }

                Unsafe.WriteUnaligned(ref Unsafe.Add(ref separators, index >>> 3), separatorBit);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref dots, index >>> 3), dotBit);
                validSeparatorTotalCount += BitOperations.PopCount(separatorBit ^ (separatorBit & (separatorBit >>> 1)));
            }
        }
        else if (Vector128.IsHardwareAccelerated)
        {
            const uint stride = 16;
            for (; index + stride <= sourceLength; index += stride)
            {
                var t0 = Vector128.LoadUnsafe(ref source, index);
                var t1 = Vector128.LoadUnsafe(ref source, index + (uint)Vector128<ushort>.Count);
                var separatorBit = Vector128.Narrow(Vector128.Equals(t0, Vector128.Create((ushort)'/')), Vector128.Equals(t1, Vector128.Create((ushort)'/'))).ExtractMostSignificantBits();
                var dotBit = Vector128.Narrow(Vector128.Equals(t0, Vector128.Create((ushort)'.')), Vector128.Equals(t1, Vector128.Create((ushort)'.'))).ExtractMostSignificantBits();
                if (!BitConverter.IsLittleEndian)
                {
                    separatorBit = BinaryPrimitives.ReverseEndianness(separatorBit);
                    dotBit = BinaryPrimitives.ReverseEndianness(dotBit);
                }

                Unsafe.WriteUnaligned(ref Unsafe.Add(ref separators, index >>> 3), (ushort)separatorBit);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref dots, index >>> 3), (ushort)dotBit);
                validSeparatorTotalCount += BitOperations.PopCount(separatorBit ^ (separatorBit & (separatorBit >>> 1)));
            }
        }

        for (; index < sourceLength; index++)
        {
            var c = Unsafe.Add(ref source, index);
            if (c == '/')
            {
                Unsafe.Add(ref separators, index >>> 3) |= (byte)(1u << ((int)index & 7));
                ++validSeparatorTotalCount;
            }
            else if (c == '.')
            {
                Unsafe.Add(ref dots, index >>> 3) |= (byte)(1u << ((int)index & 7));
            }
        }

        return validSeparatorTotalCount;
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
}
