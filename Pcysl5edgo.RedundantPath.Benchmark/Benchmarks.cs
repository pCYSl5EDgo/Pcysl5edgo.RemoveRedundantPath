using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pcysl5edgo.RedundantPath.Benchmark;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[LongRunJob]
[BenchmarkCategory("FullPath")]
public class FullPathBenchmarks
{
    [ParamsSource(nameof(TestPaths_Unix))]
    public string Source = "";

    public IEnumerable<string> TestPaths_Unix => TestData.Paths.Where(static x => x.StartsWith('/') && !x.EndsWith("/.") && !x.EndsWith("/..") && !x.Contains("/./") && !x.Contains("/../"));

    [Benchmark(Baseline = true)]
    public string ReverseEach()
    {
        return ReversePath.RemoveRedundantSegmentsForceEach(Source);
    }

    [Benchmark]
    public string ReverseSimd()
    {
        return ReversePath.RemoveRedundantSegments(Source);
    }

    [Benchmark]
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

[LongRunJob]
[BenchmarkCategory("RelativePath")]
public class RelativePathBenchmarks
{
    [ParamsSource(nameof(TestPaths_Unix))]
    public string Source = "";

    public IEnumerable<string> TestPaths_Unix => TestData.Paths.Where(x => x.Length >= 16);

    [Benchmark(Baseline = true)]
    public string ReverseEach()
    {
        return ReversePath.RemoveRedundantSegmentsForceEach(Source);
    }

    [Benchmark]
    public string ReverseSimd()
    {
        return ReversePath.RemoveRedundantSegments(Source);
    }

    [Benchmark]
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
}
