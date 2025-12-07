using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsForceEach(string? path)
    {
        if (path is null)
        {
            return "";
        }
        else if (path.Length <= 1)
        {
            return path;
        }
        else if (path.Length == 2)
        {
            return path[0] == '/' && (path[1] == '/' || path[1] == '.') ? "/" : path;
        }

        var span = path.AsSpan();
        ref var text = ref MemoryMarshal.GetReference(span);
        var startsWithSeparator = text == '/';
        var endsWithSeparator = Unsafe.Add(ref text, span.Length - 1) == '/';
        var textLength = span.Length - (startsWithSeparator ? 1 : 0) - (endsWithSeparator ? 1 : 0);
        var segmentCount = UnixInfo.CalculateMaxSegmentCount(textLength);
        var _ = (stackalloc ValueTuple<int, int>[segmentCount < 8 ? segmentCount : 8]);
        string answer;
        var info = new UnixInfo(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), _, startsWithSeparator, endsWithSeparator);
        try
        {
            answer = ToStringForceEach(path, textLength, ref info);
        }
        finally
        {
            info.Dispose();
        }
     
        return answer;
    }

    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsUnix(string? path)
    {
        if (path is null)
        {
            return "";
        }
        else if (path.Length <= 1)
        {
            return path;
        }
        else if (path.Length == 2)
        {
            return path[0] == '/' && (path[1] == '/' || path[1] == '.') ? "/" : path;
        }

        var span = path.AsSpan();
        ref var text = ref MemoryMarshal.GetReference(span);
        var startsWithSeparator = text == '/';
        var endsWithSeparator = Unsafe.Add(ref text, span.Length - 1) == '/';
        var textLength = span.Length - (startsWithSeparator ? 1 : 0) - (endsWithSeparator ? 1 : 0);
        var segmentCount = UnixInfo.CalculateMaxSegmentCount(textLength);
        var _ = (stackalloc ValueTuple<int, int>[segmentCount < 8 ? segmentCount : 8]);
        var info = new UnixInfo(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), _, startsWithSeparator, endsWithSeparator);
        try
        {
            return ToString(path, textLength, ref info);
        }
        finally
        {
            info.Dispose();
        }
    }

    private static string ToStringForceEach(string path, int textLength, ref UnixInfo info)
    {
        var answerLength = info.InitializeEach(textLength);
        if (answerLength >= path.Length)
        {
            return path;
        }
        else if (answerLength <= 0)
        {
            return "";
        }
        else if (info.IsSlashOnly)
        {
            return "/";
        }
        else
        {
            return string.Create(answerLength, info, UnixInfo.Create);
        }
    }

    private static string ToString(string path, int textLength, ref UnixInfo info)
    {
        var answerLength = info.Initialize(textLength);
        return answerLength == path.Length
            ? path
            : answerLength <= 0
                ? ""
                : info.IsSlashOnly
                    ? "/"
                    : string.Create(answerLength, info, UnixInfo.Create);
    }

    public static string RemoveRedundantSegmentsWindows(string? path)
    {
        if (path is null || path.Length == 0)
        {
            return "";
        }
        else if (path.Length == 1)
        {
            return path[0] == '/' ? "\\" : path;
        }

        var span = path.AsSpan();
        var prefix = WindowsInfo.CalculateDevicePrefix(span, out bool hasAltSeparator);
        if (prefix == WindowsInfo.Prefix.FullyQualified)
        {
            return path;
        }

        bool startsWithSeparator = false;
        byte drivePrefix = 0;
        span = span[WindowsInfo.CalculateLength(prefix)..];
        ReadOnlySpan<char> server = [], volume = [];
        switch (prefix)
        {
            case WindowsInfo.Prefix.Unc:
            case WindowsInfo.Prefix.DevicePathDotUnc:
            case WindowsInfo.Prefix.DevicePathQuestionUnc:
                {
                    var index = span.IndexOfAny('\\', '/');
                    server = span[..index];
                    span = span[(index + 1)..];
                    index = span.IndexOfAny('\\', '/');
                    volume = span[..index];
                    span = span[(index + 1)..];
                    startsWithSeparator = true;
                }
                break;
            case WindowsInfo.Prefix.DevicePathDot:
            case WindowsInfo.Prefix.DevicePathQuestion:
                {
                    var index = span.IndexOfAny('\\', '/');
                    if (index < 0)
                    {
                        volume = span;
                        span = [];
                    }
                    else
                    {
                        volume = span[..index];
                        span = span[(index + 1)..];
                    }

                    if (volume.Length == 2 && volume[1] == ':')
                    {
                        var diff = volume[0] - 'A';
                        if ((uint)diff < 26u)
                        {
                            drivePrefix = (byte)(diff + 1);
                            volume = [];
                        }
                        else if ((uint)(diff - 32) < 26u)
                        {
                            hasAltSeparator = true;
                            drivePrefix = (byte)(diff - 31);
                            volume = [];
                        }
                    }
                }
                break;
            default:
                if (span.Length >= 2 && span[1] == ':')
                {
                    var diff = span[0] - 'A';
                    if ((uint)diff < 26u)
                    {
                        drivePrefix = (byte)(diff + 1);
                        span = span[2..];
                    }
                    else if ((uint)(diff - 32) < 26u)
                    {
                        hasAltSeparator = true;
                        drivePrefix = (byte)(diff - 31);
                        span = span[2..];
                    }
                }
                break;
        }

        var firstNotSeparatorIndex = span.IndexOfAnyExcept(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (firstNotSeparatorIndex >= 0)
        {
            startsWithSeparator |= firstNotSeparatorIndex > 0;
            span = span[firstNotSeparatorIndex..];
        }
        else
        {
            startsWithSeparator |= !span.IsEmpty;
            span = [];
        }

        bool endsWithSeparator;
        if (span.IsEmpty)
        {
            endsWithSeparator = false;
        }
        else
        {
            var lastNotSeparatorIndex = span.LastIndexOfAnyExcept(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Debug.Assert(lastNotSeparatorIndex >= 0);
            endsWithSeparator = lastNotSeparatorIndex + 1 != span.Length;
            span = span[..(lastNotSeparatorIndex + 1)];
        }
        var maxSegmentCapacity = ((span.Length + 3) >> 2) << 1;
        var _ = (stackalloc long[maxSegmentCapacity <= 32 ? maxSegmentCapacity : 32]);
        var info = new WindowsInfo(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span)), MemoryMarshal.Cast<long, ValueTuple<int, int>>(_), startsWithSeparator, endsWithSeparator, prefix, drivePrefix, server, volume);
        try
        {
            var answerLength = info.Initialize(span.Length, ref hasAltSeparator);
            if (!hasAltSeparator && answerLength >= path.Length)
            {
                return path;
            }
            else if (answerLength <= 0)
            {
                return "";
            }
            else
            {
                return string.Create(path.Length < answerLength ? path.Length : answerLength, info, WindowsInfo.Create);
            }
        }
        finally
        {
            info.Dispose();
        }
    }

    private static bool IsSeparator(char c, ref bool hasChanged)
    {
        if (c == '\\')
        {
            return true;
        }
        else if (c == Path.AltDirectorySeparatorChar)
        {
            hasChanged = true;
            return true;
        }
        else
        {
            return false;
        }
    }
}
