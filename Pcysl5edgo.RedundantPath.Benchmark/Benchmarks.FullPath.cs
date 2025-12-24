using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pcysl5edgo.RedundantPath.Benchmark;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[ShortRunJob]
//[DisassemblyDiagnoser(maxDepth: 6, exportHtml: true)]
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
        return ReversePath.RemoveRedundantSegmentsUnix(Source, ReversePath.Kind.Each);
#endif
    }

#if WINDOWS_NT
    [Benchmark]
    public string ReverseSimd()
    {
        return ReversePath.RemoveRedundantSegmentsWindows(Source, false);
    }
#else
    [Benchmark]
    public string ReverseSimd32()
    {
        return ReversePath.RemoveRedundantSegmentsUnix(Source, ReversePath.Kind.Simd32);
    }

    [Benchmark]
    public string ReverseSimd64()
    {
        return ReversePath.RemoveRedundantSegmentsUnix(Source, ReversePath.Kind.Simd32);
    }

    [Benchmark]
    public string AllocOnce()
    {
        return ReversePath.RemoveRedundantSegmentsUnixAllocOnce(Source);
    }
#endif

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
