using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Collections.Generic;

namespace Pcysl5edgo.RemoveRedundantPath;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[CPUUsageDiagnoser]
[MemoryDiagnoser]
[ShortRunJob]
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
        "/home/usr/whose/my/under/water/eater",
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
}
