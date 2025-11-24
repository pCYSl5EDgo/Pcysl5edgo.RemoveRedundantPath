using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Pcysl5edgo.RemoveRedundantPath;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[MediumRunJob]
public class Benchmarks
{
    [ParamsSource(nameof(TestPaths_Unix))]
    public string Source = "";

    private static readonly string[] _paths_unix = [
        // "",
        // "a",
        // "/",
        // "//",
        "../../../../../../../../../../a/../b/c///d./././..//////////////xerea",
        "home/.",
        "/home/../usr",
        "/home/usr/../..",
        "/some/existing/path/without/relative/segments",
        "/some/lte128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo",
        "/some/gt128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo/to/test/some/of/usually/not/used/simd/branch/this/sentence/must/be/longer/than/128/characters/",
    ];
    public IEnumerable<string> TestPaths_Unix => _paths_unix;

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
