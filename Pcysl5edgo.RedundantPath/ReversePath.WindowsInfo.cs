using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    private ref struct WindowsInfo : IDisposable
    {
        private readonly ReadOnlySpan<char> textSpan;
        private Span<(int Offset, int Length)> segmentSpan;
        private int segmentCount;
        private readonly ref (int Offset, int Length) LastSegment => ref segmentSpan[segmentCount - 1];
        private long[]? rentalArray;

        private readonly ReadOnlySpan<char> server;
        private readonly ReadOnlySpan<char> volume;

        private int parentSegmentCount;
        private bool hasLeadingCurrentSegment;

        private readonly bool startsWithSeparator;
        private readonly bool endsWithSeparator;
        /// <summary>
        /// 0: None
        /// 1: \\
        /// 2: \\.\
        /// 3: \\?\
        /// 4: \\.\UNC\
        /// 5: \\?\UNC\
        /// </summary>
        private readonly Prefix prefix;

        public enum Prefix : byte
        {
            None,
            Unc,
            DevicePathDot,
            DevicePathQuestion,
            DevicePathDotUnc,
            DevicePathQuestionUnc,
            FullyQualified = byte.MaxValue,
        }

        /// <summary>
        /// 0 means no drive letter.
        /// 1-27 means A-Z.
        /// </summary>
        private readonly byte drivePrefix;
        public static bool ShouldPreserveTrailingDots(Prefix prefix) => prefix > Prefix.Unc;

        public WindowsInfo(ReadOnlySpan<char> textSpan, Span<ValueTuple<int, int>> segmentSpan, bool startsWithSeparator, bool endsWithSeparator, Prefix prefix, byte drivePrefix, ReadOnlySpan<char> uncServer, ReadOnlySpan<char> uncVolume)
        {
            this.textSpan = textSpan;
            this.segmentSpan = segmentSpan;
            segmentCount = 0;
            rentalArray = default;
            this.startsWithSeparator = startsWithSeparator;
            this.endsWithSeparator = endsWithSeparator;
            ArgumentOutOfRangeException.ThrowIfGreaterThan((byte)prefix, 5);
            this.prefix = prefix;
            this.drivePrefix = drivePrefix;
            server = uncServer;
            volume = uncVolume;
        }

        public void Dispose()
        {
            if (rentalArray is not null)
            {
                ArrayPool<long>.Shared.Return(rentalArray);
                rentalArray = default;
            }
        }

        public int Initialize(ref bool hasBeenChanged)
        {
            if (textSpan.IsEmpty)
            {
                hasBeenChanged |= CleanUp();
                return CalculateLength(0);
            }
            else if (textSpan.Length <= 32)
            {
                //return InitializeEach(ref hasBeenChanged);
                return InitializeSimdLTE32(ref hasBeenChanged);
            }
            else
            {
                return InitializeEach(ref hasBeenChanged);
            }
        }

        public int InitializeEach(ref bool hasBeenChanged)
        {
            bool isPreviousSeparatorCanonical = false, preserveTrailingDots = ShouldPreserveTrailingDots(prefix);
            int mode = 0, segmentCharCount = 0;
            for (int textIndex = textSpan.Length - 1; textIndex >= 0; --textIndex)
            {
                var c = textSpan[textIndex];
                if (mode > 0)
                {
                    switch (c)
                    {
                        case '/':
                            if (parentSegmentCount != 0)
                            {
                                --parentSegmentCount;
                            }
                            else
                            {
                                segmentCharCount += AddOrUniteSegment(textIndex + 1, mode, isPreviousSeparatorCanonical ? (textIndex + mode + 2) : -1);
                            }

                            hasBeenChanged = true;
                            isPreviousSeparatorCanonical = false;
                            mode = 0;
                            break;
                        case '\\':
                            if (parentSegmentCount != 0)
                            {
                                --parentSegmentCount;
                            }
                            else
                            {
                                segmentCharCount += AddOrUniteSegment(textIndex + 1, mode, isPreviousSeparatorCanonical ? (textIndex + mode + 2) : -1);
                            }

                            isPreviousSeparatorCanonical = true;
                            mode = 0;
                            break;
                        default:
                            ++mode;
                            continue;
                    }
                }
                else if (mode == 0)
                {
                    switch (c)
                    {
                        case '/':
                        case '\\':
                            hasBeenChanged = true;
                            break;
                        case '.':
                            mode = -1;
                            break;
                        default:
                            mode = 1;
                            break;
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '/':
                            ProcessDotSequence(isPreviousSeparatorCanonical, mode, ref segmentCharCount, textIndex);
                            hasBeenChanged = true;
                            isPreviousSeparatorCanonical = false;
                            mode = 0;
                            break;
                        case '\\':
                            ProcessDotSequence(isPreviousSeparatorCanonical, mode, ref segmentCharCount, textIndex);
                            isPreviousSeparatorCanonical = true;
                            mode = 0;
                            break;
                        case '.':
                            --mode;
                            break;
                        default:
                            mode = 1 - (preserveTrailingDots ? mode : 0);
                            break;
                    }
                }
            }

            if (mode > 0)
            {
                if (parentSegmentCount != 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    segmentCharCount += AddOrUniteSegment(0, mode, isPreviousSeparatorCanonical ? mode + 1 : -1);
                }
            }
            else if (mode < -2)
            {
                if (parentSegmentCount != 0)
                {
                    --parentSegmentCount;
                }
                else
                {
                    hasLeadingCurrentSegment = false;
                    if (preserveTrailingDots || endsWithSeparator || segmentCount != 0)
                    {
                        segmentCharCount += AddOrUniteSegment(0, -mode, isPreviousSeparatorCanonical ? 1 - mode : -1);
                    }
                }
            }
            else if (!startsWithSeparator)
            {
                if (mode == -1)
                {
                    hasLeadingCurrentSegment = parentSegmentCount == 0;
                }
                else if (mode == -2)
                {
                    ++parentSegmentCount;
                }
            }

            hasBeenChanged |= CleanUp();
            return CalculateLength(segmentCharCount);
        }

        private int InitializeSimdLTE32(ref bool hasBeenChanged)
        {
            bool isPreviousSeparatorCanonical = false, preserveTrailingDots = ShouldPreserveTrailingDots(prefix);
            const uint OneBit = 1u;
            const int BitWidth = 32;
#pragma warning disable IDE0018
            uint separator, dot, altSeparator, separatorWall, current, parent;
#pragma warning restore IDE0018
            if (textSpan.Length == 32)
            {
                separator = BitSpan.Get(textSpan, out dot, out altSeparator);
            }
            else
            {
                separator = BitSpan.Get(textSpan, out dot, out altSeparator, textSpan.Length);
            }

            hasBeenChanged |= altSeparator != default;
            BitSpan.CalculateUpperBitWall(textSpan.Length - 1, out separatorWall);
            separatorWall |= (separator >>> 1);
            current = dot & ((separator << 1) | OneBit) & separatorWall;
            parent = dot & (dot << 1) & ((separator << 2) | (OneBit << 1)) & separatorWall;
            int segmentCharCount = 0;
            var textIndex = textSpan.Length - 1;
            var continueLength = ProcessLoop(ref segmentCharCount, ref textIndex, ref isPreviousSeparatorCanonical, 0, separator, altSeparator, dot, current, parent, preserveTrailingDots, 0);
            if (continueLength != 0)
            {
                segmentCharCount = ProcessLastContinueation(segmentCharCount, continueLength, isPreviousSeparatorCanonical);
            }

            hasBeenChanged |= CleanUp();
            return CalculateLength(segmentCharCount);
        }

        private int ProcessLastContinueation(int segmentCharCount, int continueLength, bool isPreviousSeparatorCanonical)
        {
            if (parentSegmentCount > 0)
            {
                --parentSegmentCount;
                return segmentCharCount;
            }
            else
            {
                hasLeadingCurrentSegment = false;
                if (continueLength < 0)
                {
                    if (endsWithSeparator || segmentCount != 0)
                    {
                        return segmentCharCount + AddOrUniteSegment(0, -continueLength, isPreviousSeparatorCanonical ? -continueLength + 1 : -1);
                    }
                    else
                    {
                        return segmentCharCount;
                    }
                }
                else
                {
                    Debug.Assert(continueLength > 0);
                    return segmentCharCount + AddOrUniteSegment(0, continueLength, isPreviousSeparatorCanonical ? continueLength + 1 : -1);
                }
            }
        }

        private int ProcessLoop(ref int segmentCharCount, ref int textIndex, ref bool isPreviousSeparatorCanonical, int continueLength, uint separator, uint altSeparator, uint dot, uint current, uint parent, bool preserveTrailingDots, int batchIndex)
        {
            const int BitCount = 32, BitMask = BitCount - 1;
            var loopLowerLimit = batchIndex * BitCount;
            var textIndexOffset = loopLowerLimit + BitCount;
            var any = separator | current | parent;
            do
            {
                if (BitSpan.GetBit(any, textIndex))
                {
                    if (BitSpan.GetBit(parent, textIndex))
                    {
                        isPreviousSeparatorCanonical = !BitSpan.GetBit(altSeparator, textIndex + 1);
                        ++parentSegmentCount;
                        textIndex -= 3;
                    }
                    else if (BitSpan.GetBit(separator, textIndex))
                    {
                        isPreviousSeparatorCanonical = false;
                        textIndex = textIndexOffset - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex));
                        continue;
                    }
                    else
                    {
                        isPreviousSeparatorCanonical = !BitSpan.GetBit(altSeparator, textIndex + 1);
                        hasLeadingCurrentSegment = !startsWithSeparator && parentSegmentCount == 0;
                        textIndex -= 2;
                    }
                }
                else
                {
                    var separatorIndex = BitMask - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(separator, textIndexOffset - textIndex));
                    if (separatorIndex < 0)
                    {
                        if (preserveTrailingDots || !BitSpan.GetBit(dot, textIndex))
                        {
                            var answer = (textIndex & BitMask) + 1;
                            textIndex = loopLowerLimit - 1;
                            return answer;
                        }
                        else
                        {
                            var preDotIndex = BitMask - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(~dot, textIndexOffset - textIndex));
                            if (preDotIndex < 0)
                            {
                                var answer = -1 - (textIndex & BitMask);
                                textIndex = loopLowerLimit - 1;
                                return answer;
                            }

                            textIndex = loopLowerLimit - 1;
                            return preDotIndex + 1;
                        }
                    }

                    if (parentSegmentCount != 0)
                    {
                        --parentSegmentCount;
                    }
                    else
                    {
                        hasLeadingCurrentSegment = false;
                        if (preserveTrailingDots || !BitSpan.GetBit(dot, textIndex))
                        {
                            segmentCharCount += AddOrUniteSegment(loopLowerLimit + separatorIndex + 1, textIndex - loopLowerLimit - separatorIndex, isPreviousSeparatorCanonical ? textIndex + 2 : -1);
                        }
                        else
                        {
                            var preDotIndex = BitMask - BitOperations.LeadingZeroCount(BitSpan.ZeroClearUpperBit(~dot, textIndexOffset - textIndex));
                            if (preDotIndex == separatorIndex)
                            {
                                if (endsWithSeparator || segmentCount != 0)
                                {
                                    segmentCharCount += AddOrUniteSegment(loopLowerLimit + separatorIndex + 1, textIndex - loopLowerLimit - separatorIndex, isPreviousSeparatorCanonical ? textIndex + 2 : -1);
                                }
                            }
                            else
                            {
                                segmentCharCount += AddSegment(loopLowerLimit + separatorIndex + 1, preDotIndex - loopLowerLimit - separatorIndex);
                            }
                        }
                    }

                    isPreviousSeparatorCanonical = !BitSpan.GetBit(altSeparator, separatorIndex);
                    textIndex = loopLowerLimit + separatorIndex - 1;
                }
            }
            while (textIndex >= loopLowerLimit);

            return 0;
        }

        private bool CleanUp()
        {
            if (prefix == Prefix.DevicePathQuestion || prefix == Prefix.DevicePathDot)
            {
                hasLeadingCurrentSegment = false;
                if (drivePrefix != 0)
                {
                    parentSegmentCount = 0;
                }
            }

            if (startsWithSeparator)
            {
                parentSegmentCount = 0;
                hasLeadingCurrentSegment = false;
            }
            else if (parentSegmentCount != 0)
            {
                hasLeadingCurrentSegment = false;
            }

            return segmentCount > 1;
        }

        private void ProcessDotSequence(bool isPreviousSeparatorCanonical, int mode, ref int segmentCharCount, int textIndex)
        {
            switch (mode + 2)
            {
                case 0: // parent
                    ++parentSegmentCount;
                    break;
                case 1: // current
                    hasLeadingCurrentSegment = !startsWithSeparator && parentSegmentCount == 0;
                    break;
                default:
                    hasLeadingCurrentSegment = false;
                    segmentCharCount += AddOrUniteSegment(textIndex + 1, -mode, isPreviousSeparatorCanonical ? (textIndex - mode + 1) : -1);
                    break;
            }
        }

        public static Prefix CalculateDevicePrefix(ReadOnlySpan<char> span, out bool hasAltSeparator)
        {
            hasAltSeparator = false;
            var devicePrefix = Prefix.None;
            switch (span[0])
            {
                case '\\':
                    switch (span[1])
                    {
                        case '\\':
                            devicePrefix = Prefix.Unc;
                            if (span.Length >= 4)
                            {
                                switch (span[2])
                                {
                                    case '?':
                                        switch (span[3])
                                        {
                                            case '\\':
                                                return Prefix.FullyQualified;
                                            case '/':
                                                devicePrefix = Prefix.DevicePathQuestion;
                                                hasAltSeparator = true;
                                                break;
                                        }
                                        break;
                                    case '.':
                                        if (IsSeparator(span[3], ref hasAltSeparator))
                                        {
                                            devicePrefix = Prefix.DevicePathDot;
                                        }
                                        break;
                                }
                            }

                            break;
                        case '/':
                            hasAltSeparator = true;
                            devicePrefix = Prefix.Unc;
                            if (span.Length >= 4 && IsSeparator(span[3], ref hasAltSeparator))
                            {
                                switch (span[2])
                                {
                                    case '.':
                                        devicePrefix = Prefix.DevicePathDot;
                                        break;
                                    case '?':
                                        devicePrefix = Prefix.DevicePathQuestion;
                                        break;
                                }
                            }
                            break;
                        case '?':
                            return span.Length >= 4 && span[2] == '?' && span[3] == '\\' ? Prefix.FullyQualified : Prefix.None;
                        default:
                            return Prefix.None;
                    }
                    break;
                case '/':
                    hasAltSeparator = true;
                    if (!IsSeparator(span[1], ref hasAltSeparator))
                    {
                        return Prefix.None;
                    }

                    devicePrefix = Prefix.Unc;
                    if (span.Length >= 4 && IsSeparator(span[3], ref hasAltSeparator))
                    {
                        switch (span[2])
                        {
                            case '.':
                                devicePrefix = Prefix.DevicePathDot;
                                break;
                            case '?':
                                devicePrefix = Prefix.DevicePathQuestion;
                                break;
                        }
                    }
                    break;
            }

            switch (devicePrefix)
            {
                case Prefix.DevicePathDot:
                case Prefix.DevicePathQuestion:
                    if (span.Length >= 8 && IsSeparator(span[7], ref hasAltSeparator) && span.Slice(4, 3).SequenceEqual("UNC"))
                    {
                        devicePrefix += 2;
                    }
                    break;
            }

            return devicePrefix;
        }

        public static int CalculateLength(Prefix prefix)
        {
            return prefix switch
            {
                Prefix.Unc => 2,
                Prefix.DevicePathDot or Prefix.DevicePathQuestion => 4,
                Prefix.DevicePathDotUnc or Prefix.DevicePathQuestionUnc => 8,
                _ => 0,
            };
        }

        private readonly int CalculateLength(int segmentCharCount)
        {
            Debug.Assert(segmentCount != 0 || segmentCharCount == 0);
            int answer = 0;
            switch (prefix)
            {
                case Prefix.DevicePathDot:
                case Prefix.DevicePathQuestion:
                    answer = 5 + volume.Length;
                    goto default;
                case Prefix.DevicePathDotUnc:
                case Prefix.DevicePathQuestionUnc:
                    answer = 6;
                    goto case Prefix.Unc;
                case Prefix.Unc:
                    return answer + 4 + server.Length + volume.Length + (segmentCount != 0 ? segmentCount + segmentCharCount - 1 + (endsWithSeparator ? 1 : 0) : 0);
                default:
                    if (startsWithSeparator)
                    {
                        return (segmentCount != 0 ? segmentCount + segmentCharCount + (endsWithSeparator ? 1 : 0) : 1) + (drivePrefix != 0 ? 2 : 0);
                    }

                    answer += (drivePrefix != 0 ? 2 : 0) + (endsWithSeparator ? 1 : 0);
                    if (segmentCount == 0)
                    {
                        return answer + (parentSegmentCount != 0
                            ? (3 * parentSegmentCount - 1)
                            : (hasLeadingCurrentSegment
                                ? 1 : (endsWithSeparator ? -1 : 0)));
                    }
                    else
                    {
                        return answer + segmentCount + segmentCharCount - 1 + (parentSegmentCount != 0
                            ? (3 * parentSegmentCount)
                            : (hasLeadingCurrentSegment ? 2 : 0));
                    }
            }
        }

        private int AddSegment(int offset, int length)
        {
            if (++segmentCount > segmentSpan.Length)
            {
                if (rentalArray is null)
                {
                    rentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
                    var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(rentalArray.AsSpan());
                    segmentSpan.CopyTo(temp);
                    segmentSpan = temp;
                }
                else
                {
                    var tempRentalArray = ArrayPool<long>.Shared.Rent((int)BitOperations.RoundUpToPowerOf2((uint)segmentCount));
                    var temp = MemoryMarshal.Cast<long, ValueTuple<int, int>>(tempRentalArray.AsSpan());
                    segmentSpan.CopyTo(temp);
                    ArrayPool<long>.Shared.Return(rentalArray);
                    rentalArray = tempRentalArray;
                    segmentSpan = temp;
                }
            }

            segmentSpan[segmentCount - 1] = new(offset, length);
            return length;
        }

        private int AddOrUniteSegment(int offset, int length, int expectedOffset)
        {
            hasLeadingCurrentSegment = false;
            if (segmentCount > 0)
            {
                ref var last = ref segmentSpan[segmentCount - 1];
                if (last.Offset == expectedOffset)
                {
                    last.Offset = offset;
                    last.Length += ++length;
                    return length;
                }
            }

            return AddSegment(offset, length);
        }

        private readonly void Write(Span<char> span)
        {
            switch (prefix)
            {
                case Prefix.DevicePathDot:
                    @"\\.\".CopyTo(span);
                    if (drivePrefix != 0)
                    {
                        span = WriteDrive(span[4..]);
                        if (!span.IsEmpty)
                        {
                            span[0] = '\\';
                            span = span[1..];
                        }
                    }
                    else
                    {
                        span = WriteVolume(span[4..]);
                    }
                    break;
                case Prefix.DevicePathQuestion:
                    @"\\?\".CopyTo(span);
                    if (drivePrefix != 0)
                    {
                        span = WriteDrive(span[4..]);
                        if (!span.IsEmpty)
                        {
                            span[0] = '\\';
                            span = span[1..];
                        }
                    }
                    else
                    {
                        span = WriteVolume(span[4..]);
                    }
                    break;
                case Prefix.Unc:
                    @"\\".CopyTo(span);
                    span = WriteServerAndVolume(span[2..]);
                    break;
                case Prefix.DevicePathDotUnc:
                    @"\\.\UNC\".CopyTo(span);
                    span = WriteServerAndVolume(span[8..]);
                    break;
                case Prefix.DevicePathQuestionUnc:
                    @"\\?\UNC\".CopyTo(span);
                    span = WriteServerAndVolume(span[8..]);
                    break;
                default:
                    if (drivePrefix != 0)
                    {
                        span = WriteDrive(span);
                    }

                    if (startsWithSeparator)
                    {
                        span[0] = '\\';
                        span = span[1..];
                    }
                    break;
            }

            if (hasLeadingCurrentSegment)
            {
                span[0] = '.';
                span = span[1..];
                if (segmentCount == 0)
                {
                    goto END;
                }

                span[0] = '\\';
                span = span[1..];
            }
            else if (parentSegmentCount != 0)
            {
                span[0] = '.';
                span[1] = '.';
                span = span[2..];
                for (int i = 1; i < parentSegmentCount; ++i)
                {
                    span[2] = '.';
                    span[0] = '\\';
                    span[1] = '.';
                    span = span[3..];
                }

                if (segmentCount == 0)
                {
                    goto END;
                }

                span[0] = '\\';
                span = span[1..];
            }

            if (segmentCount != 0)
            {
                var (offset, length) = LastSegment;
                textSpan.Slice(offset, length).CopyTo(span);
                span = span[length..];
                for (int i = segmentCount - 2; i >= 0; --i)
                {
                    span[0] = '\\';
                    (offset, length) = segmentSpan[i];
                    textSpan.Slice(offset, length).CopyTo(span[1..]);
                    span = span[(length + 1)..];
                }
            }

        END:
            if (endsWithSeparator && (segmentCount != 0 || parentSegmentCount != 0 || hasLeadingCurrentSegment))
            {
                span[0] = '\\';
                span = span[1..];
            }

            Debug.Assert(span.IsEmpty);
        }

        private readonly Span<char> WriteDrive(Span<char> span)
        {
            span[0] = (char)('A' - 1 + drivePrefix);
            span[1] = ':';
            return span[2..];
        }

        public static void Create(Span<char> span, WindowsInfo info)
        {
            info.Write(span);
        }

        private readonly Span<char> WriteServerAndVolume(Span<char> span)
        {
            server.CopyTo(span);
            if (server.Length == span.Length)
            {
                return [];
            }

            span[server.Length] = '\\';
            return WriteVolume(span[(server.Length + 1)..]);
        }

        private readonly Span<char> WriteVolume(Span<char> span)
        {
            volume.CopyTo(span);
            if (volume.Length == span.Length)
            {
                return [];
            }

            span[volume.Length] = '\\';
            return span[(volume.Length + 1)..];
        }

        public static byte CalculateDrivePrefix(ref ReadOnlySpan<char> span, ref bool hasBeenChanged)
        {
            if (span.Length >= 2 && span[1] == ':')
            {
                var diff = span[0] - 'A';
                if ((uint)diff < 26u)
                {
                    span = span[2..];
                    return (byte)(diff + 1);
                }
                else if ((uint)(diff - 32) < 26u)
                {
                    hasBeenChanged = true;
                    span = span[2..];
                    return (byte)(diff - 31);
                }
            }

            return default;
        }

        #region Debug
#if DEBUG
        public override readonly string ToString()
        {
            if (segmentCount == 0)
            {
                return "";
            }

            DefaultInterpolatedStringHandler handler = $"";
            for (int segmentIndex = segmentCount - 1; ; segmentIndex--)
            {
                var (offset, length) = segmentSpan[segmentIndex];
                handler.AppendFormatted(textSpan.Slice(offset, length));
                if (segmentIndex == 0)
                {
                    break;
                }
                else
                {
                    handler.AppendFormatted('\n');
                }
            }

            return handler.ToString();
        }
#endif
        #endregion
    }
}