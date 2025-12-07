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
    public void UnixReverseTest(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsUnix(original);
        var actualEach = ReversePath.RemoveRedundantSegmentsForceEach(original);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(expected, actual));
            Assert.True(ReferenceEquals(expected, actualEach));
        }

        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsUnix(actual)));
        Assert.True(ReferenceEquals(actualEach, ReversePath.RemoveRedundantSegmentsForceEach(actualEach)));
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
        };
    #endregion
}