using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedudantPath;

public static partial class ReversePath
{
    private ref struct Info
    {
        private readonly ref ushort textRef;
        private readonly ref int offsetRef;
        private readonly ref int lengthRef;
        private int segmentCount;
        private readonly bool startsWithSeparator;
        private readonly bool endsWithSepartor;
        private int parentCount;
        private bool hasCurrent;
        public readonly bool IsSlashOnly => startsWithSeparator && segmentCount == 0;

        public Info(ref ushort textRef, ref int offsetRef, ref int lengthRef, bool startsWithSeparator, bool endsWithSepartor)
        {
            this.textRef = ref textRef;
            this.offsetRef = ref offsetRef;
            this.lengthRef = ref lengthRef;
            this.startsWithSeparator = startsWithSeparator;
            this.endsWithSepartor = endsWithSepartor;
            segmentCount = 0;
            parentCount = 0;
            hasCurrent = false;
        }

        // a/./a
        public static int CalculateMaxSegmentCount(int charCount) => (charCount + 3) >>> 2;

        public int Initialize(int textLength)
        {
            Debug.Assert(textLength >= 0);
            if (startsWithSeparator)
            {
                if (textLength < 16)
                {
                    return InitializeWithStartingSeparator(textLength);
                }
                else if (textLength <= 32)
                {
                    return InitializeWithStartingSeparatorSimdLTE32(textLength);
                }
                else if (textLength <= 64)
                {
                    return InitializeWithStartingSeparatorSimdLTE64(textLength);
                }
                else
                {
                    return InitializeWithStartingSeparatorSimdGT64(textLength);
                }
            }
            else
            {
                return InitializeWithoutStartingSeparator(textLength);
            }
        }

        private int InitializeWithStartingSeparator(int textLength)
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
                    else if (segmentCount == 0)
                    {
                        offsetRef = textIndex + 1;
                        lengthRef = segmentCharCount = mode;
                        segmentCount = 1;
                    }
                    else
                    {
                        ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                        if (++textIndex + mode == oldOffset)
                        {
                            oldOffset = textIndex;
                            Unsafe.Add(ref lengthRef, segmentCount - 1) += ++mode;
                            segmentCharCount += mode;
                        }
                        else
                        {
                            Unsafe.Add(ref offsetRef, segmentCount) = textIndex;
                            Unsafe.Add(ref lengthRef, segmentCount++) = mode;
                            segmentCharCount += mode;
                        }
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
                    mode = c switch
                    {
                        '/' => 0,
                        '.' => -2,
                        _ => 2,
                    };
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
                hasCurrent = false;
                if (parentCount == 0)
                {
                    if (segmentCount == 0)
                    {
                        offsetRef = 0;
                        lengthRef = segmentCharCount = mode;
                        segmentCount = 1;
                    }
                    else
                    {
                        ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                        if (mode + 1 == oldOffset)
                        {
                            oldOffset = 0;
                            Unsafe.Add(ref lengthRef, segmentCount - 1) += mode + 1;
                            segmentCharCount += mode + 1;
                        }
                        else
                        {
                            Unsafe.Add(ref offsetRef, segmentCount) = 0;
                            Unsafe.Add(ref lengthRef, segmentCount++) = mode;
                            segmentCharCount += mode;
                        }
                    }
                }
                else
                {
                    --parentCount;
                }
            }

            Debug.Assert(segmentCount != 0 || segmentCharCount == 0);
            if (segmentCount == 0)
            {
                return 1;
            }

            return segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0);
        }

        private int InitializeWithoutStartingSeparator(int textLength)
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
                            offsetRef = textIndex + 1;
                            lengthRef = segmentCharCount = mode;
                            segmentCount = 1;
                        }
                        else
                        {
                            ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                            if (++textIndex + mode == oldOffset)
                            {
                                oldOffset = textIndex;
                                Unsafe.Add(ref lengthRef, segmentCount - 1) += ++mode;
                                segmentCharCount += mode;
                            }
                            else
                            {
                                Unsafe.Add(ref offsetRef, segmentCount) = textIndex;
                                Unsafe.Add(ref lengthRef, segmentCount++) = mode;
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
                        offsetRef = 0;
                        lengthRef = segmentCharCount = mode;
                        segmentCount = 1;
                    }
                    else
                    {
                        ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                        if (mode + 1 == oldOffset)
                        {
                            oldOffset = 0;
                            Unsafe.Add(ref lengthRef, segmentCount - 1) += mode + 1;
                            segmentCharCount += mode + 1;
                        }
                        else
                        {
                            Unsafe.Add(ref offsetRef, segmentCount) = 0;
                            Unsafe.Add(ref lengthRef, segmentCount++) = mode;
                            segmentCharCount += mode;
                        }
                    }
                }
            }
            else if (mode == -1)
            {
                hasCurrent = true;
            }
            else
            {
                Debug.Assert(mode == -2);
                ++parentCount;
            }

            Debug.Assert(segmentCount != 0 || segmentCharCount == 0);
            var sum = segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0) - 1;
            if (parentCount == 0)
            {
                return sum + (hasCurrent ? 2 : 0);
            }
            else
            {
                hasCurrent = false;
                return sum + (3 * parentCount);
            }
        }

        public int InitializeWithStartingSeparatorSimdLTE32(int textLength)
        {
            var sep = BitSpan.Get(ref textRef, out uint dot, textLength);
            var sepWall = ((sep >>> 1) | BitSpan.CalculateUpperBitWall32(textLength - 1));
            var current = dot & ((sep << 1) | 1u) & sepWall;
            var parent = dot & (dot << 1) & ((sep << 2) | 2u) & sepWall;
            var sepWithNeighborSep = sep & (sepWall | (sep << 1) | 1u);
            if ((current | parent | sepWithNeighborSep) == 0)
            {
                return textLength + 1 + (endsWithSepartor ? 1 : 0);
            }

            int segmentCharCount = 0;
            var any = sep | current | parent;
            const int BitCount = 32;
            for (int textIndex = textLength - 1; textIndex >= 0;)
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(sep, textIndex))
                    {
                        textIndex = BitCount - 1 - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(~sep, BitCount - textIndex));
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        textIndex -= 2;
                    }
                }
                else
                {
                    var nextSeparatorIndex = BitCount - 1 - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(sep, BitCount - textIndex));
                    var length = textIndex - nextSeparatorIndex;
                    if (parentCount != 0)
                    {
                        --parentCount;
                    }
                    else
                    {
                        if (segmentCount == 0)
                        {
                            offsetRef = nextSeparatorIndex + 1;
                            lengthRef = length;
                            segmentCount = 1;
                            segmentCharCount = length;
                        }
                        else
                        {
                            ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                            if (textIndex + 2 == oldOffset)
                            {
                                oldOffset = nextSeparatorIndex + 1;
                                Unsafe.Add(ref lengthRef, segmentCount - 1) += ++length;
                                segmentCharCount += length;
                            }
                            else
                            {
                                Unsafe.Add(ref offsetRef, segmentCount) = nextSeparatorIndex + 1;
                                Unsafe.Add(ref lengthRef, segmentCount++) = length;
                                segmentCharCount += length;
                            }
                        }
                    }

                    textIndex = nextSeparatorIndex - 1;
                }
            }

            var totalLength = segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0);
            return totalLength == 0 ? 1 : totalLength;
        }

        public int InitializeWithStartingSeparatorSimdLTE64(int textLength)
        {
            var sep = BitSpan.Get(ref textRef, out ulong dot, textLength);
            var sepWall = ((sep >>> 1) | BitSpan.CalculateUpperBitWall64(textLength - 1));
            var current = dot & ((sep << 1) | 1ul) & sepWall;
            var parent = dot & (dot << 1) & ((sep << 2) | 2ul) & sepWall;
            var sepWithNeighborSep = sep & (sepWall | (sep << 1) | 1ul);
            if ((current | parent | sepWithNeighborSep) == 0)
            {
                return textLength + 1 + (endsWithSepartor ? 1 : 0);
            }

            int segmentCharCount = 0;
            var any = sep | current | parent;
            const int BitCount = 64;
            int textIndex = textLength - 1;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(sep, textIndex))
                    {
                        textIndex = BitCount - 1 - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(~sep, BitCount - textIndex));
                    }
                    else if (BitSpan.GetBit(parent, textIndex))
                    {
                        ++parentCount;
                        textIndex -= 3;
                    }
                    else
                    {
                        Debug.Assert(BitSpan.GetBit(current, textIndex));
                        textIndex -= 2;
                    }
                }
                else
                {
                    var nextSeparatorIndex = BitCount - 1 - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(sep, BitCount - textIndex));
                    var length = textIndex - nextSeparatorIndex;
                    if (parentCount != 0)
                    {
                        --parentCount;
                    }
                    else
                    {
                        if (segmentCount == 0)
                        {
                            offsetRef = nextSeparatorIndex + 1;
                            lengthRef = length;
                            segmentCount = 1;
                            segmentCharCount = length;
                        }
                        else
                        {
                            ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                            if (textIndex + 2 == oldOffset)
                            {
                                oldOffset = nextSeparatorIndex + 1;
                                Unsafe.Add(ref lengthRef, segmentCount - 1) += ++length;
                            }
                            else
                            {
                                Unsafe.Add(ref offsetRef, segmentCount) = nextSeparatorIndex + 1;
                                Unsafe.Add(ref lengthRef, segmentCount++) = length;
                            }

                            segmentCharCount += length;
                        }
                    }

                    textIndex = nextSeparatorIndex - 1;
                }
            } while (textIndex >= 0);

            var totalLength = segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0);
            return totalLength == 0 ? 1 : totalLength;
        }

        public int InitializeWithStartingSeparatorSimdGT64(int textLength)
        {
            Debug.Assert(textLength >= 65);
            const int BitCount = 64;
            int segmentCharCount = 0, textIndex = textLength - 1, ulongCount = (textLength + (BitCount - 1)) >>> 6;
            var sepCurrent = BitSpan.Get(ref Unsafe.Add(ref textRef, (ulongCount - 1) * BitCount), out ulong dotCurrent, textLength & (BitCount - 1));
            var sepPrev = BitSpan.Get(ref Unsafe.Add(ref textRef, (ulongCount - 2) * BitCount), out ulong dotPrev);
            var sepWall = BitSpan.CalculateUpperBitWall64((textLength & (BitCount - 1)) - 1) | (sepCurrent >>> 1);
            var current = dotCurrent & ((sepCurrent << 1) | (sepPrev >>> (BitCount - 1))) & sepWall;
            var parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> (BitCount - 1))) & ((sepCurrent << 2) | (sepPrev >>> (BitCount - 2))) & sepWall;
            var doesNormalPathSegmentContinue = DoLoopWithStartingSeparator(ref segmentCharCount, ref textIndex, false, sepCurrent, parent, current, ulongCount - 1);

            for (int ulongIndex = ulongCount - 2; ulongIndex > 0; --ulongIndex)
            {
                sepWall = (sepCurrent << (BitCount - 1)) | (sepPrev >>> 1);
                sepCurrent = sepPrev;
                dotCurrent = dotPrev;
                sepPrev = BitSpan.Get(ref textRef, out dotPrev);
                current = dotCurrent & ((sepCurrent << 1) | (sepPrev >>> (BitCount - 1))) & sepWall;
                parent = dotCurrent & ((dotCurrent << 1) | (dotPrev >>> (BitCount - 1))) & ((sepCurrent << 2) | (sepPrev >>> (BitCount - 2))) & sepWall;
                doesNormalPathSegmentContinue = DoLoopWithStartingSeparator(ref segmentCharCount, ref textIndex, doesNormalPathSegmentContinue, sepCurrent, parent, current, ulongIndex);
            }

            sepWall = (sepCurrent << (BitCount - 1)) | (sepPrev >>> 1);
            current = dotPrev & ((sepPrev << 1) | 1ul) & sepWall;
            parent = dotPrev & (dotPrev << 1) & ((sepPrev << 2) | 2ul) & sepWall;
            _ = DoLoopWithStartingSeparator(ref segmentCharCount, ref textIndex, doesNormalPathSegmentContinue, sepPrev, parent, current, 0);

            var totalLength = segmentCount + segmentCharCount + (endsWithSepartor ? 1 : 0);
            return totalLength == 0 ? 1 : totalLength;
        }

        private bool DoLoopWithStartingSeparator(ref int segmentCharCount, ref int textIndex, bool doesNormalPathSegmentContinue, ulong separator, ulong parent, ulong current, int ulongIndex)
        {
            var loopLowerLimit = ulongIndex << 6;
            var textIndexOffset = loopLowerLimit + 64;
            var any = separator | parent | current;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    textIndex = RarePathWithStartingSeparator(textIndex, separator, parent, textIndexOffset);
                    continue;
                }

                var temp = BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex);
                var nextSeparatorIndex = textIndexOffset - 1 - BitOperations.LeadingZeroCount(temp);
                var length = textIndex - nextSeparatorIndex;
                if (parentCount != 0)
                {
                    --parentCount;
                }
                else if (segmentCount == 0)
                {
                    segmentCharCount = SetInitialSegment(nextSeparatorIndex, length);
                }
                else
                {
                    ref var oldOffset = ref Unsafe.Add(ref offsetRef, segmentCount - 1);
                    if (doesNormalPathSegmentContinue)
                    {
                        doesNormalPathSegmentContinue = false;
                        oldOffset = nextSeparatorIndex + 1;
                        Unsafe.Add(ref lengthRef, segmentCount - 1) += length;
                    }
                    else if (textIndex + 2 == oldOffset)
                    {
                        oldOffset = nextSeparatorIndex + 1;
                        Unsafe.Add(ref lengthRef, segmentCount - 1) += ++length;
                    }
                    else
                    {
                        Unsafe.Add(ref offsetRef, segmentCount) = nextSeparatorIndex + 1;
                        Unsafe.Add(ref lengthRef, segmentCount++) = length;
                    }

                    segmentCharCount += length;
                }

                if (nextSeparatorIndex < loopLowerLimit)
                {
                    textIndex = nextSeparatorIndex;
                    return true;
                }
                else
                {
                    textIndex = nextSeparatorIndex - 1;
                }
            }
            while (textIndex >= loopLowerLimit);
            return false;
        }

        private int SetInitialSegment(int nextSeparatorIndex, int length)
        {
            segmentCount = 1;
            offsetRef = nextSeparatorIndex + 1;
            return lengthRef = length;
        }

        private int RarePathWithStartingSeparator(int textIndex, ulong separatorCurrent, ulong parent, int textIndexOffset)
        {
            if (BitSpan.GetBit(separatorCurrent, textIndex))
            {
                return textIndexOffset - 1 - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(~separatorCurrent, textIndexOffset - textIndex));
            }
            else if (BitSpan.GetBit(parent, textIndex))
            {
                ++parentCount;
                return textIndex - 3;
            }
            else
            {
                return textIndex - 2;
            }
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

        public static void Create(Span<char> span, Info arg) => arg.Write(ref MemoryMarshal.GetReference(span));

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
                var charCount = Unsafe.Add(ref lengthRef, segmentIndex);
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref destination, 1)), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, Unsafe.Add(ref offsetRef, segmentIndex))), (uint)(charCount << 1));
                destination = ref Unsafe.Add(ref destination, charCount + 1);
            }

            return ref destination;
        }

        private readonly ref char WriteSegmentsWithoutStartingSeparator(ref char destination)
        {
            int segmentIndex = segmentCount - 1;
            var charCount = Unsafe.Add(ref lengthRef, segmentIndex);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref destination), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, Unsafe.Add(ref offsetRef, segmentIndex))), (uint)(charCount << 1));
            destination = ref Unsafe.Add(ref destination, charCount);
            for (segmentIndex = segmentCount - 2; segmentIndex >= 0; --segmentIndex)
            {
                destination = '/';
                charCount = Unsafe.Add(ref lengthRef, segmentIndex);
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref destination, 1)), ref Unsafe.As<ushort, byte>(ref Unsafe.Add(ref textRef, Unsafe.Add(ref offsetRef, segmentIndex))), (uint)(charCount << 1));
                destination = ref Unsafe.Add(ref destination, charCount + 1);
            }

            return ref destination;
        }
        #endregion

        #region Debug
#if DEBUG
        public override string ToString()
        {
            if (segmentCount == 0)
            {
                return "[]";
            }

            DefaultInterpolatedStringHandler handler = $"[\n";
            for (int i = segmentCount - 1; i >= 0; i--)
            {
                var offset = Unsafe.Add(ref offsetRef, i);
                var length = Unsafe.Add(ref lengthRef, i);
                handler.AppendFormatted(MemoryMarshal.Cast<ushort, char>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref textRef, offset), length)));
                handler.AppendFormatted('\n');
            }
            handler.AppendFormatted(']');
            return handler.ToString();
        }
#endif
        #endregion
    }
}
