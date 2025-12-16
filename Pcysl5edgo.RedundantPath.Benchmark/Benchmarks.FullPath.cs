using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pcysl5edgo.RedundantPath.Benchmark;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[ShortRunJob]
[BenchmarkCategory("FullPath")]
public class FullPathBenchmarks
{
    [ParamsSource(nameof(TestPaths))]
    public string Source = "";

    public IEnumerable<string> TestPaths => TestData.Paths.Where(static x => x.StartsWith('/') && !x.EndsWith("/.") && !x.EndsWith("/..") && !x.Contains("/./") && !x.Contains("/../"))
#if WINDOWS_NT
        .Concat(TestData.WindowsFullPaths)
#endif
        ;

    [Benchmark]
    public string ReverseEach()
    {
#if WINDOWS_NT
        return ReversePath.RemoveRedundantSegmentsWindows(Source, true);
#else
        return ReversePath.RemoveRedundantSegmentsUnix(Source, true);
#endif
    }

    [Benchmark]
    public string ReverseSimd()
    {
#if WINDOWS_NT
        return ReversePath.RemoveRedundantSegmentsWindows(Source, false);
#else
        return ReversePath.RemoveRedundantSegmentsUnix(Source, false);
#endif
    }

    [Benchmark(Baseline = true)]
    public string Old()
    {
        ValueStringBuilder builder = new(Source.Length);
        if (RedundantSegmentHelper.TryRemoveRedundantSegments(Source.AsSpan(), ref builder))
        {
            return builder.ToString();
        }
        else
        {
            return Source;
        }
    }

    [Benchmark]
    public string Full()
    {
        return System.IO.Path.GetFullPath(Source);
    }
}
