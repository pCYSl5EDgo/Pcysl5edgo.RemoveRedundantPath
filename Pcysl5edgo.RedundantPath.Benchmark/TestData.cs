namespace Pcysl5edgo.RedundantPath.Benchmark;

internal static class TestData
{
    internal static readonly string[] Paths = [
        "",
        "a",
        "/",
        "//",
        "../../../../../../../../../../a/../b/c///d./././..//////////////xerea",
        "abc/def/ghi/jkl/mno/pqr/stu/vwx/yzα/βγΔ/ευσ/\\\\/誰もお前を許しはしない/な/ぜ/な/ら/ば/そ/も/そ/も/お/前/は/自由/であ/り/誰/に/も/呪/わ/れ/て/は/い/ない/の/だ/か/ら/../../../../../../../../../../../../../../../",
        "home/.",
        "/home/../usr",
        "/home/usr/../..",
        "/some/existing/path/without/relative/segments",
        "/some/lte128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo",
        "/some/gt128/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo/to/test/some/of/usually/not/used/simd/branch/this/sentence/must/be/longer/than/128/characters/",
        "/1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a/0123456789/abcd",
        "////1//ultra_long/chars/heiufhugaehu//////wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/../../gwe/vr/./awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars///wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/./0123456789/abcd"
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
