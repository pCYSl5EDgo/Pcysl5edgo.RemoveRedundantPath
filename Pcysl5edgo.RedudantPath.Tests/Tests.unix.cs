namespace Pcysl5edgo.RemoveRedundantPath.Tests;

public class RedundantSegmentsTests_Unix : RedundantSegmentsTestsBase
{
    #region Tests

    [Theory]
    [MemberData(nameof(TestPaths_Unix))]
    public void UnixSimdSpanTest(string original, string expected)
    {
        Assert.Equal(expected, SimdPath.RemoveRedundantSegmentsSpan(original));
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

            {@"../../../../../../../../../../a/../b/c///d./././..//////////////xerea", @"../../../../../../../../../../b/c/xerea" }
        };
    #endregion
}