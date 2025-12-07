using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pcysl5edgo.RedundantPath.Benchmark;

[LongRunJob]
[BenchmarkCategory("RelativePath")]
public class RelativePathBenchmarks
{
    [ParamsSource(nameof(TestPaths))]
    public string Source = "";

    public IEnumerable<string> TestPaths => TestData.Paths
#if WINDOWS_NT
        .Concat(TestData.WindowsPaths)
#endif
        .Where(x => x.Length >= 16);

    [Benchmark]
    public string ReverseEach()
    {
#if WINDOWS_NT
        return ReversePath.RemoveRedundantSegmentsWindows(Source);
#else
        return ReversePath.RemoveRedundantSegmentsForceEach(Source);
#endif
    }

#if WINDOWS_NT
#else
    [Benchmark]
    public string ReverseSimd()
    {
        return ReversePath.RemoveRedundantSegmentsUnix(Source);
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
}
