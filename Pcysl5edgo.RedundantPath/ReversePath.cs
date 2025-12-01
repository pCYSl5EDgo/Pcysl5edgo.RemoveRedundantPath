using System.Buffers;
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
        var segmentCountX2 = Info.CalculateMaxSegmentCount(textLength) << 1;
        if (segmentCountX2 > 256)
        {
            return LongPathForceEach(path, ref text, startsWithSeparator, endsWithSeparator, textLength, segmentCountX2);
        }

        var _ = (stackalloc int[segmentCountX2]);
        var info = new Info(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), ref MemoryMarshal.GetReference(_), ref Unsafe.Add(ref MemoryMarshal.GetReference(_), segmentCountX2 >>> 1), startsWithSeparator, endsWithSeparator);
        return ToStringForceEach(path, textLength, ref info);
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
        var segmentCountX2 = Info.CalculateMaxSegmentCount(textLength) << 1;
        if (segmentCountX2 > 256)
        {
            return LongPath(path, ref text, startsWithSeparator, endsWithSeparator, textLength, segmentCountX2);
        }

        var _ = (stackalloc int[segmentCountX2]);
        var info = new Info(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), ref MemoryMarshal.GetReference(_), ref Unsafe.Add(ref MemoryMarshal.GetReference(_), segmentCountX2 >>> 1), startsWithSeparator, endsWithSeparator);
        return ToString(path, textLength, ref info);
    }

    private static string ToStringForceEach(string path, int textLength, ref Info info)
    {
        var answerLength = info.InitializeEach(textLength);
        return answerLength == path.Length
            ? path
            : answerLength <= 0
                ? ""
                : info.IsSlashOnly
                    ? "/"
                    : string.Create(answerLength, info, Info.Create);
    }

    private static string ToString(string path, int textLength, ref Info info)
    {
        var answerLength = info.Initialize(textLength);
        return answerLength == path.Length
            ? path
            : answerLength <= 0
                ? ""
                : info.IsSlashOnly
                    ? "/"
                    : string.Create(answerLength, info, Info.Create);
    }

    private static string LongPath(string path, ref char text, bool startsWithSeparator, bool endsWithSeparator, int textLength, int segmentCountX2)
    {
        var rental = ArrayPool<int>.Shared.Rent(segmentCountX2);
        try
        {
            ref var offsetRef = ref MemoryMarshal.GetArrayDataReference(rental);
            var info = new Info(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), ref offsetRef, ref Unsafe.Add(ref offsetRef, rental.Length >>> 1), startsWithSeparator, endsWithSeparator);
            return ToString(path, textLength, ref info);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rental);
        }
    }

    private static string LongPathForceEach(string path, ref char text, bool startsWithSeparator, bool endsWithSeparator, int textLength, int segmentCountX2)
    {
        var rental = ArrayPool<int>.Shared.Rent(segmentCountX2);
        try
        {
            ref var offsetRef = ref MemoryMarshal.GetArrayDataReference(rental);
            var info = new Info(ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref text, startsWithSeparator ? 1 : 0)), ref offsetRef, ref Unsafe.Add(ref offsetRef, rental.Length >>> 1), startsWithSeparator, endsWithSeparator);
            return ToStringForceEach(path, textLength, ref info);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rental);
        }
    }

    public static IAsyncEnumerable<char>? RemoveRedundantSegmentsWindows(string original)
    {
        throw new NotImplementedException();
    }
}
