using System.Buffers;
using System.IO;
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
        var info = new UnixInfo(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), _, startsWithSeparator, endsWithSeparator);
        try
        {
            return ToStringForceEach(path, textLength, ref info);
        }
        finally
        {
            info.Dispose();
        }
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
        return answerLength == path.Length
            ? path
            : answerLength <= 0
                ? ""
                : info.IsSlashOnly
                    ? "/"
                    : string.Create(answerLength, info, UnixInfo.Create);
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
        if (path is null)
        {
            return "";
        }
        else if (path.Length <= 1)
        {
            return path[0] == '/' ? "\\" : path;
        }

        throw new NotImplementedException();
    }
}
