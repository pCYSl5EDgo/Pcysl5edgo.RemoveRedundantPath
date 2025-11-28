namespace Pcysl5edgo.RemoveRedundantPath.Benchmark;

internal static class TestData
{
    internal static readonly string[] Paths = [
        "",
        "a",
        "/",
        "//",
        "../../../../../../../../../../a/../b/c///d./././..//////////////xerea",
        "home/.",
        "/home/../usr",
        "/home/usr/../..",
        "/some/existing/path/without/relative/segments",
        "/some/lte128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo",
        "/some/gt128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo/to/test/some/of/usually/not/used/simd/branch/this/sentence/must/be/longer/than/128/characters/",
    ];
}
