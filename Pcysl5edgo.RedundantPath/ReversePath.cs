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
        span = span.TrimStart('/');
        bool startsWithSeparator = span.Length != path.Length, endsWithSeparator;
        {
            var oldLength = span.Length;
            span = span.TrimEnd('/');
            endsWithSeparator = span.Length != oldLength;
        }

        var segmentCount = UnixInfo.CalculateMaxSegmentCount(span.Length);
        var _ = (stackalloc ValueTuple<int, int>[segmentCount < 8 ? segmentCount : 8]);
        var info = new UnixInfo(span, _, startsWithSeparator, endsWithSeparator);
        try
        {
            var answerLength = forceEach ? info.InitializeEach() : info.Initialize();
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

        bool startsWithSeparator = false, endsWithSeparator;
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

                    if (volume.Length == 2)
                    {
                        Debug.Assert(drivePrefix == 0);
                        drivePrefix = WindowsInfo.CalculateDrivePrefix(ref volume, ref hasAltSeparator);
                        Debug.Assert(drivePrefix != 0 || volume.IsEmpty);
                    }
                }
                break;
            default:
                Debug.Assert(drivePrefix == 0);
                drivePrefix = WindowsInfo.CalculateDrivePrefix(ref span, ref hasAltSeparator);
                break;
        }

        const string separators = @"\/";
        {
            var oldLength = span.Length;
            span = span.TrimStart(separators);
            startsWithSeparator |= span.Length != oldLength;
        }
        {
            var oldLength = span.Length;
            span = span.TrimEnd(separators);
            endsWithSeparator = span.Length != oldLength;
        }

        if (!endsWithSeparator && !WindowsInfo.ShouldPreserveTrailingDots(prefix))
        {
            var trimmed = span.TrimEnd('.');
            if (span.Length - trimmed.Length >= 3)
            {
                span = trimmed.TrimEnd(separators);
                endsWithSeparator = span.Length != trimmed.Length;
            }
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
