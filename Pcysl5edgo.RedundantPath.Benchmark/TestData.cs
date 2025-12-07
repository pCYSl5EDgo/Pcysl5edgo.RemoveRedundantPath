namespace Pcysl5edgo.RedundantPath.Benchmark;

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

    internal static readonly string[] WindowsFullPaths = [
        @"C:\",
        @"A:\Users\匿名希望\Downloads\🏰.exe",
        @"C:\Program Files (x86)\😂🙇‍♀️🙇‍♂️\👉.txt\nise_file.bat",
        @"\\?\UNC\Remote Server\First Volume\folder-19\_mid-folder-19_\subfolder-81\0.py",
        @"\\Z:\ton\two\"
    ];
    internal static readonly string[] WindowsPaths = [
        @"./",
        @"./////",
        @"\..\/...///",
        @"..\/...///",
        @"\\?..\/...///",
        @"\\?..\abcdef/...///",
        @"\??\..\abcdef/...///",
        @"//.\D:\abc..\def.../..///....",
    ];
}
