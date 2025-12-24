using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RedundantPath;

public static partial class ReversePath
{
    [SkipLocalsInit]
    public static string RemoveRedundantSegmentsRoughUnix(string? path, Kind kind = Kind.Each)
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

        var segmentCapacity = RoughUnixInfo.CalculateMaxSegmentCount(span.Length);
        if (segmentCapacity >= 256)
        {
            var array = ArrayPool<long>.Shared.Rent(segmentCapacity);
            try
            {
                var info = new RoughUnixInfo(span, MemoryMarshal.Cast<long, (int, int)>(array.AsSpan()), startsWithSeparator, endsWithSeparator);
                var answerLength = kind switch
                {
                    Kind.Simd32 => info.Initialize32(),
                    Kind.Simd64 => info.Initialize64(),
                    _ => info.InitializeEach(),
                };
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
                    return string.Create(answerLength, info, RoughUnixInfo.Create);
                }
            }
            finally
            {
                ArrayPool<long>.Shared.Return(array);
            }
        }
        else
        {
            var _ = (stackalloc ValueTuple<int, int>[segmentCapacity]);
            var info = new RoughUnixInfo(span, _, startsWithSeparator, endsWithSeparator);
            var answerLength = kind switch
            {
                Kind.Simd32 => info.Initialize32(),
                Kind.Simd64 => info.Initialize64(),
                _ => info.InitializeEach(),
            };
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
                return string.Create(answerLength, info, RoughUnixInfo.Create);
            }
        }
    }
}
