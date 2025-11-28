using System.Diagnostics;
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

        public int Initialize(int textLength) => startsWithSeparator ? InitializeWithStartingSeparator(textLength) : InitializeWithoutStartingSeparator(textLength);

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

            return segmentCharCount + segmentCount + (endsWithSepartor ? 1 : 0);
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
    }
}
