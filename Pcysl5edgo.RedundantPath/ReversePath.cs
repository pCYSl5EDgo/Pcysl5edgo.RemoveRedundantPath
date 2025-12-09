using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsUnix(string? path, bool forceEach = false)
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
            var answerLength = forceEach ? info.InitializeEach(textLength) : info.Initialize(textLength);
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
        finally
        {
            info.Dispose();
        }
    }

    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsWindows(string? path, bool forceEach = false)
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
                    if (index < 0)
                    {
                        server = span;
                        span = [];
                    }
                    else
                    {
                        server = span[..index];
                        span = span[(index + 1)..];
                        index = span.IndexOfAny('\\', '/');
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
                    }

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

                    drivePrefix = WindowsInfo.CalculateDrivePrefix(ref span, ref hasAltSeparator);
                }
                break;
            default:
                drivePrefix = WindowsInfo.CalculateDrivePrefix(ref span, ref hasAltSeparator);
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
            var answerLength = forceEach ? info.InitializeEach(span.Length, ref hasAltSeparator) : info.Initialize(span.Length, ref hasAltSeparator);
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
