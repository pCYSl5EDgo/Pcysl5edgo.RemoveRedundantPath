/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

namespace Pcysl5edgo.RedundantPath.Tests;

public class RedundantSegmentsTests_Unix : RedundantSegmentsTestsBase
{
    #region Tests
    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixAllocOnceTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnixAllocOnce(original);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        Assert.Equal(actual, ReversePath.RemoveRedundantSegmentsUnixAllocOnce(actual));
        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsUnixAllocOnce(actual)));
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseSimd32Test(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnix(original, ReversePath.Kind.Simd32);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseSimd64Test(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnix(original, ReversePath.Kind.Simd64);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseEachTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnix(original, ReversePath.Kind.Each);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        Assert.Equal(actual, ReversePath.RemoveRedundantSegmentsUnix(actual, ReversePath.Kind.Each));
        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsUnix(actual, ReversePath.Kind.Each)));
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseSimd32NoTrimTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnixNoTrim(original, ReversePath.Kind.Simd32);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseSimd64NoTrimTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnixNoTrim(original, ReversePath.Kind.Simd64);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixReverseEachNoTrimTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnixNoTrim(original, ReversePath.Kind.Each);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        Assert.Equal(actual, ReversePath.RemoveRedundantSegmentsUnixNoTrim(actual, ReversePath.Kind.Each));
        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsUnixNoTrim(actual, ReversePath.Kind.Each)));
    }
    #endregion

    #region Test data

    public static readonly TheoryData<string, string> TestPaths_Unix = new()
    {
            // Qualified Unmodified
            { @"/",      @"/" },
            { @"/home",  @"/home" },
            { @"/home/", @"/home/" },

            // Qualified Modified

            // Single
            { @"/.",         @"/" },
            { @"/./",        @"/" },
            { @"/./././",    @"/" },

            { @"/home/.",              @"/home" },
            { @"/home/./",             @"/home/" },
            { @"/home/./user",         @"/home/user" },
            { @"/home/././user",       @"/home/user" },

            // Double
            { @"/..",        @"/" },
            { @"/../",       @"/" },
            { @"/../../../", @"/" },

            { @"/home/..",               @"/" },
            { @"/home/../",              @"/" },
            { @"/home/../user",          @"/user" },
            { @"/home/../../user",       @"/user" },
            { @"/home/../user/..",       @"/" },
            { @"/home/../../user/../..", @"/" },
            { @"/home/.././user/../.",   @"/" },

            { @"/../folder",     @"/folder" },
            { @"/../folder/",    @"/folder/" },
            { @"/../folder/..",  @"/" },
            { @"/../folder/../",  @"/" },

            { @"/../../folder",        @"/folder" },
            { @"/../../folder/",       @"/folder/" },
            { @"/../../folder/../..",  @"/" },
            { @"/../../folder/../../", @"/" },

            // Combined
            { @"/.././",     @"/" },
            { @"/./../",     @"/" },

            // Duplicate separators
            { @"///",      @"/" },
            { @"//home//", @"/home/" },
            { @"//.//",    @"/" },
            { @"//..//",   @"/" },

            // Unqualified unmodified
            { @"home",   @"home" },
            { @"home/",  @"home/" },
            { @"./home", @"./home" },

            // Unqualified Modified

            //Single
            { @".",      @"." },
            { @"./",     @"./" },
            { @"./.",    @"." },
            { @"././",   @"./" },

            { @"folder/.",     @"folder" },
            { @"folder/./",    @"folder/" },
            { @"folder/./.",   @"folder" },
            { @"folder/././",  @"folder/" },

            { @"./folder",     @"./folder" },
            { @"./folder/",    @"./folder/" },
            { @"././folder",   @"./folder" },
            { @"././folder/",  @"./folder/" },

            { @"././folder/./.",   @"./folder" },
            { @"././folder/././",  @"./folder/" },

            // Double
            { @"..",     @".." },
            { @"../",    @"../" },
            { @"../..",  @"../.." },
            { @"../../", @"../../" },
            { @"../.", @".." },
            { @".././", @"../" },

            { @"folder/..",      @"" },
            { @"folder/../",     @"" },
            { @"foder/../..",    @".." },
            { @"folder/../../",  @"../" },

            { @"../folder",      @"../folder" },
            { @"../folder/",     @"../folder/" },
            { @"../../folder",   @"../../folder" },
            { @"../../folder/",  @"../../folder/" },

            // Combined
            { @"folder/./..",        @"" },
            { @"folder/../.",        @"." },

            { @"./folder/..",  @"." },
            { @"./folder/../", @"./" },

            { @"folder/subfolder/./",       @"folder/subfolder/" },
            { @"folder/./subfolder",        @"folder/subfolder" },
            { @"folder/../subfolder",       @"subfolder" },
            { @"folder/../../subfolder",    @"../subfolder" },
            { @"folder/../subfolder/../.",  @"." },
            { @"folder/./subfolder/../..",  @"" },
            { @"folder/./subfolder/../../", @"" },

            // Special cases from Windows do not apply here:
            // "...", "....", "dot." or "name\more" are valid segment names
            
            { @"/home/....",     @"/home/...." },
            { @"/home/..../",    @"/home/..../" },
            { @"/..../folder",   @"/..../folder" },
            { @"/home/dot.",     @"/home/dot." },
            { @"/home/dot./",    @"/home/dot./" },
            { @"/home/.dot",     @"/home/.dot" },
            { @"/home/.dot/",    @"/home/.dot/" },

            { @"/home/folder\same/subfolder",        @"/home/folder\same/subfolder" },
            { @"/home/folder\same/subfolder/..",     @"/home/folder\same" },
            { @"/home/folder\same/subfolder/../",    @"/home/folder\same/" },
            { @"/home/folder\same/subfolder/../..",  @"/home" },
            { @"/home/folder\same/subfolder/../../", @"/home/" },

            { @"....",           @"...." },
            { @"..../",          @"..../" },
            { @".dot",           @".dot" },
            { @".dot/",          @".dot/" },
            { @"dot.",           @"dot." },
            { @"dot./",          @"dot./" },

            { @"/home/..../.",   @"/home/...." },
            { @"/home/...././",  @"/home/..../" },
            { @"/home/..../..",  @"/home" },
            { @"/home/..../../", @"/home/" },

            { @"../../../../../../../../../../a/../b/c///d./././..//////////////xerea", @"../../../../../../../../../../b/c/xerea" },
            { @"/some/existing/path/without/relative/segments", @"/some/existing/path/without/relative/segments" },
            { @"/lte128/some/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge", @"/lte128/some/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge"},
            { @"/gt128/some/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo/to/test/some/of/usually/not/used/simd/branch/this/sentence/must/be/longer/than/128/characters/", @"/gt128/some/existing/path/without/relative/segments/with/a/lot/of/very/long/no/meaning/so/long/meaningless/hoge/fuga/piyo/to/test/some/of/usually/not/used/simd/branch/this/sentence/must/be/longer/than/128/characters/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_segment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_segment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_segment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_segment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            
            // split by 64 chars
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/./ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/./ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/../ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/../ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/./", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/.", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/..", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/../", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/" },

            // split by 63 chars
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/./ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/./ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/../ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/../ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/./", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/.", @"/first_segment_length_128_ultimatelylong_very_very_very_first_se/g/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/..", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg" },
            { @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/ment_length_128_ultimatelylong_very_very_very_scarely_longer_xyz/../", @"/first_segment_length_128_ultimatelylong_very_very_very_first_seg/" },

            // repeat parent simd
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../..", @"/ab0/ab1/ab2/ab3/ab4/ab5" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../..", @"/ab0/ab1/ab2/ab3/ab4" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../..", @"/ab0/ab1/ab2/ab3" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../../..", @"/ab0/ab1/ab2" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../../../..", @"/ab0/ab1" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../../../../..", @"/ab0" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../../../../../..", @"/" },
            { @"/ab0/ab1/ab2/ab3/ab4/ab5/ab6/ab7/ab8/ab9/a10/a11/a12/a13/a14/a15/../../../../../../../../../../../../../../../../..", @"/" },
            { @"../../../../../../../../../../../../../../../../000/001/002/003/004/005/006/007/008/009/00a/00b/00c/00d/00e/00f/010/011/012/../../../../../../../../../../../../../../../../", @"../../../../../../../../../../../../../../../../000/001/002/" },
            { @"../../../../../../../../../../../../../../../../000/001/002/003/004/005/006/007/008/009/00a/00b/00c/00d/00e/00f/010/011/012/013/014/015/016/017/../../../../../../../../../../../../../../../../", @"../../../../../../../../../../../../../../../../000/001/002/003/004/005/006/007/" },

            { @"/1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a/0123456789/abcd", @"/1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a/0123456789/abcd" },
            { @"////1//ultra_long/chars/heiufhugaehu//////wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/../../gwe/vr/./awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars///wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/./0123456789/abcd", @"/1/ultra_long/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/gwe/vr/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/w3a1025/chars/heiufhugaehu/wafwfre/bve/gwe/vr/ge/awafwbe/wffawfw/vaw/awjfoe/awvnt/awf3/4thrd/4ghbse3q/vge3tg4rg/0123456789/abcd" },
        };
    #endregion
}