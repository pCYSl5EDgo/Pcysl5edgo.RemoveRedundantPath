using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pcysl5edgo.RemoveRedundantPath;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[MediumRunJob]
[BenchmarkCategory("FullPath")]
public class FullPathBenchmarks
{
    [ParamsSource(nameof(TestPaths_Unix))]
    public string Source = "";

    public IEnumerable<string> TestPaths_Unix => TestData.Paths.Where(static x => x.StartsWith('/'));

    [Benchmark]
    public string SimdSpan()
    {
        return SimdPath.RemoveRedundantSegmentsSpan(Source);
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

[ShortRunJob]
[BenchmarkCategory("RelativePath")]
public class RelativePathBenchmarks
{
    [ParamsSource(nameof(TestPaths_Unix))]
    public string Source = "";

    public IEnumerable<string> TestPaths_Unix => TestData.Paths.Where(static x => (uint)(x.Length - 3) <= 29u);

    [Benchmark]
    public string Each()
    {
        return SimdPath.RemoveRedundantSegmentsEach(Source);
    }

    [Benchmark]
    public string SimdSpan()
    {
        return SimdPath.RemoveRedundantSegmentsSpan(Source);
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
