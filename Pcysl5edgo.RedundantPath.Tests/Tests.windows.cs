// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

public class RedundantSegmentsTests_Windows : RedundantSegmentsTestsBase
{
    #region Tests

    [Fact]
    public void DoubleSlash()
    {
        TestWindows("//", @"\\");
    }

    [Fact]
    public void BenchmarkTest()
    {
        TestWindows(@"//.\D:\abc..\def.../..///....", @"\\.\D:\abc..\....");
    }

    private const string LongFolderName = "どうしようもなく異常に長いパスの文字列をこれからつらつら書いてみんとてこのような無様の謗りを免れないフォルダ名を日本語にて記述せざるをえぬのじゃー世知辛いのじゃー本当は、本当は、日本語ではなく🤣などのような絵文字を使うべきなのでしょう。まあこのような長パスだけで全てを網羅することはできませぬが";
    private const string LongSubfolderName = "フォルダーとフォルダ、この2単語の語感の微妙な差異に籠められた情感を読者諸氏は感じ取ることができるだろうか。日本の昭和時代に翻訳された技術文書では単語の末にある伸ばし棒が省略される慣習が存在した。故に令和のこの時代に生きる我々はフォルダという言葉に昭和のかほりを嗅ぎ取ることとなるのである。昭和……";

    #region Qualified NoRedundancy

    [Theory]
    [MemberData(nameof(MemberData_DevicePrefix))]
    public void Unmodified(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_NoRedundancy_DriveAndRoot))]
    public void Qualified_NoRedundancy(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_NoRedundancy_DriveAndRoot_EdgeCases))]
    public void Qualified_NoRedundancy_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot))]
    public void Qualified_NoRedundancy_Prefix(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot_EdgeCases))]
    public void Qualified_NoRedundancy_Prefix_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_ServerShare_NoRedundancy))]
    public void Qualified_NoRedundancy_ServerShare(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_ServerShare_NoRedundancy_EdgeCases))]
    public void Qualified_NoRedundancy_ServerShare_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_UNC_NoRedundancy))]
    public void Qualified_NoRedundancy_UNC(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_UNC_NoRedundancy_EdgeCases))]
    public void Qualified_NoRedundancy_UNC_EdgeCases(string original, string expected) => TestWindows(original, expected);

    #endregion

    #region Qualified redundant

    [Theory]
    [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_SingleDot))]
    [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot))]
    [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_Combined))]
    public void Qualified_Redundant(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot_EdgeCases))]
    public void Qualified_Redundant_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot))]
    [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot))]
    [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_Combined))]
    public void Qualified_Redundant_Prefix(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot_EdgeCases))]
    public void Qualified_Redundant_Prefix_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_ServerShare_Redundant_SingleDot))]
    [MemberData(nameof(MemberData_ServerShare_Redundant_DoubleDot))]
    [MemberData(nameof(MemberData_ServerShare_Redundant_Combined))]
    public void Qualified_Redundant_ServerShare(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_ServerShare_Redundant_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_ServerShare_Redundant_DoubleDot_EdgeCases))]
    public void Qualified_Redundant_ServerShare_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_UNC_Redundant_SingleDot))]
    [MemberData(nameof(MemberData_UNC_Redundant_DoubleDot))]
    [MemberData(nameof(MemberData_UNC_Redundant_Combined))]
    public void Qualified_Redundant_UNC(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_UNC_Redundant_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_UNC_Redundant_DoubleDot_EdgeCases))]
    public void Qualified_Redundant_UNC_EdgeCases(string original, string expected) => TestWindows(original, expected);

    #endregion

    #region Unqualified NoRedundancy

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy))]
    public void Unqualified_NoRedundancy(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_EdgeCases))]
    public void Unqualified_NoRedundancy_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DrivelessRoot))]
    public void Unqualified_NoRedundancy_DrivelessRoot(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DrivelessRoot_EdgeCases))]
    public void Unqualified_NoRedundancy_DrivelessRoot_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DriveRootless))]
    public void Unqualified_NoRedundancy_DriveRootless(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_DriveRootless_EdgeCases))]
    public void Unqualified_NoRedundancy_DriveRootless_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless))]
    public void Unqualified_NoRedundancy_Prefix_DriveRootless(string original) => TestWindows(original, original);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeCases))]
    public void Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeData(string original, string expected) => TestWindows(original, expected);

    #endregion

    #region Unqualified redundant

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_SingleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DoubleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Combined))]
    public void Unqualified_Redundant(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DoubleDot_EdgeCases))]
    public void Unqualified_Redundant_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot))]
    public void Unqualified_Redundant_DrivelessRoot(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot_EdgeCases))]
    public void Unqualified_Redundant_DrivelessRoot_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_SingleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_DoubleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_Combined))]
    public void Unqualified_Redundant_DriveRootless(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_DriveRootless_DoubleDot_EdgeCases))]
    public void Unqualified_Redundant_DriveRootless_EdgeCases(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_Combined))]
    public void Unqualified_Redundant_Prefix_DriveRootless(string original, string expected) => TestWindows(original, expected);

    [Theory]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot_EdgeCases))]
    [MemberData(nameof(MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot_EdgeCases))]
    public void Unqualified_Redundant_Prefix_DriveRootless_EdgeCases(string original, string expected) => TestWindows(original, expected);

    #endregion

    #endregion

    #region Test data

    private const string ServerShare = @"\\Server\Share\";
    private const string UNCServerShare = @"UNC\Server\Share\";
    private const string Prefix_Windows_Drive_Rootless = "C:";
    private const string Prefix_Windows_Driveless_Root = @"\";
    private const string Prefix_Windows_Drive_Root = Prefix_Windows_Drive_Rootless + Prefix_Windows_Driveless_Root;
    private static readonly string DevicePrefix = @"\\.\";


    #region Device prefix

    // No matter what the string is, if it's preceded by a device prefix, we don't do anything
    private static readonly string[] Suffixes =
    [
        @"",
        @"\", @"\\",
        @"/", @"//", @"\/", @"/\",
        @".",
        @".\", @".\\",
        @"./", @".//",
        @"\.", @"\\.", @"\.\", @"\\.\\",
        @"/.", @"//.", @"/./", @"//.//",
        @"\.\.", @"\\.\\.", @"\.\.\", @"\\.\\.\\",
        @"\..", @"\\..", @"\..\", @"\\..\\",
        @"\..\..", @"\\..\\..", @"\..\..\", @"\\..\\..\\",
        @"\.\..", @"\\.\\..", @"\.\..\", @"\\.\\..\\",
        @"\..\.", @"\\..\\.", @"\..\.\", @"\\..\\.\\"
    ];
    private static readonly string[] ExtendedPrefixes =
    [
        @"\\?\",
        @"\??\"
    ];
    private static readonly string[] TestPaths_DevicePrefix =
    [
        @"C",
        @"C:",
        @"C:\",
        @"C:/",
        @"C:\folder",
        @"C:/folder",
        $@"C:\{LongFolderName}",
        $@"C:/{LongFolderName}",
        @"C:A",
        @"C:A",
        @"C:A\folder",
        @"C:A/folder",
        $@"C:A\{LongFolderName}",
        $@"C:A/{LongFolderName}",
    ];
    public static IEnumerable<object[]> MemberData_DevicePrefix =>
        from prefix in ExtendedPrefixes
        from s in TestPaths_DevicePrefix
        from suffix in Suffixes
        select new object[] { prefix + s + suffix };

    private static readonly string[] TestPaths_DevicePrefix_UNC =
    [
        @"UNC",
        @"UNC\Server",
        @"UNC/Server",
        @"UNC\Server\Share",
        @"UNC/Server/Share",
        @"UNC\Server\Share\folder",
        @"UNC/Server/Share/folder",
        $@"UNC\Server\Share\{LongFolderName}",
        $@"UNC/Server/Share/{LongFolderName}",
    ];
    public static IEnumerable<object[]> MemberData_DevicePrefix_UNC =>
        from prefix in ExtendedPrefixes
        from s in TestPaths_DevicePrefix_UNC
        from suffix in Suffixes
        select new object[] { prefix + s + suffix };

    #endregion

    #region No redundancy

    private static readonly string[] TestPaths_NoRedundancy =
    [
        @"folder",
        @"folder\",
        @"folder\file.txt",
        @"folder\subfolder",
        @"folder\subfolder\",
        @"folder\subfolder\file.txt",
        $@"{LongFolderName}",
        $@"{LongFolderName}\",
        $@"{LongFolderName}\file.txt",
        $@"{LongFolderName}\{LongSubfolderName}",
        $@"{LongFolderName}\{LongSubfolderName}\",
        $@"{LongFolderName}\{LongSubfolderName}\file.txt"
    ];
    public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_DriveAndRoot =>
        from s in TestPaths_NoRedundancy
        select new object[] { Prefix_Windows_Drive_Root + s };
    public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot =>
        from s in TestPaths_NoRedundancy
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + s };
    public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveRootless =>
        from s in TestPaths_NoRedundancy
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + s };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DriveRootless =>
        from s in TestPaths_NoRedundancy
        select new object[] { Prefix_Windows_Drive_Rootless + s };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless =>
        from s in TestPaths_NoRedundancy
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + s };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy =>
        from s in TestPaths_NoRedundancy
        select new object[] { s };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DrivelessRoot =>
        from s in TestPaths_NoRedundancy
        select new object[] { Prefix_Windows_Driveless_Root + s };
    public static IEnumerable<object[]> MemberData_ServerShare_NoRedundancy =>
        from s in TestPaths_NoRedundancy
        select new object[] { ServerShare + s };
    public static IEnumerable<object[]> MemberData_UNC_NoRedundancy =>
        from s in TestPaths_NoRedundancy
        select new object[] { DevicePrefix + UNCServerShare + s };

    #endregion

    #region Single dot

    private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_SingleDot = new()
    {
        // The original and qualified strings must get the root string prefixed
        // Original | Qualified | Unqualified | Device prefix
        { @".",      @"",    @".",  @"." },
        { @".\",     @"",    @".\", @".\" },
        { @".\.",    @"",    @".",  @".\" },
        { @".\.\",   @"",    @".\", @".\" },

        { @".\folder",       @"folder",      @".\folder",   @".\folder" },
        { @".\folder\",      @"folder\",     @".\folder\",  @".\folder\" },
        { @".\folder\.",     @"folder",      @".\folder",   @".\folder" },
        { @".\folder\.\",    @"folder\",     @".\folder\",  @".\folder\" },
        { @".\folder\.\.",   @"folder",      @".\folder",   @".\folder" },
        { @".\folder\.\.\",  @"folder\",     @".\folder\",  @".\folder\" },
        { $@".\{LongFolderName}",       $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\{LongFolderName}\",      $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },
        { $@".\{LongFolderName}\.",     $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\{LongFolderName}\.\",    $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },
        { $@".\{LongFolderName}\.\.",   $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\{LongFolderName}\.\.\",  $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },

        { @".\.\folder",         @"folder",      @".\folder",   @".\folder" },
        { @".\.\folder\",        @"folder\",     @".\folder\",  @".\folder\" },
        { @".\.\folder\.",       @"folder",      @".\folder",   @".\folder" },
        { @".\.\folder\.\",      @"folder\",     @".\folder\",  @".\folder\" },
        { @".\.\folder\.\.",     @"folder",      @".\folder",   @".\folder" },
        { @".\.\folder\.\.\",    @"folder\",     @".\folder\",  @".\folder\" },
        { $@".\.\{LongFolderName}",         $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\.\{LongFolderName}\",        $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },
        { $@".\.\{LongFolderName}\.",       $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\.\{LongFolderName}\.\",      $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },
        { $@".\.\{LongFolderName}\.\.",     $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\.\{LongFolderName}\.\.\",    $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },

        { @"folder\.",       @"folder",      @"folder",     @"folder\" },
        { @"folder\.\",      @"folder\",     @"folder\",    @"folder\" },
        { @"folder\.\.",     @"folder",      @"folder",     @"folder\" },
        { @"folder\.\.\",    @"folder\",     @"folder\",    @"folder\" },
        { $@"{LongFolderName}\.",       $@"{LongFolderName}",      $@"{LongFolderName}",     $@"{LongFolderName}\" },
        { $@"{LongFolderName}\.\",      $@"{LongFolderName}\",     $@"{LongFolderName}\",    $@"{LongFolderName}\" },
        { $@"{LongFolderName}\.\.",     $@"{LongFolderName}",      $@"{LongFolderName}",     $@"{LongFolderName}\" },
        { $@"{LongFolderName}\.\.\",    $@"{LongFolderName}\",     $@"{LongFolderName}\",    $@"{LongFolderName}\" },

        { @"folder\subfolder\.",     @"folder\subfolder",  @"folder\subfolder",     @"folder\subfolder" },
        { @"folder\subfolder\.\",    @"folder\subfolder\", @"folder\subfolder\",    @"folder\subfolder\" },
        { @"folder\subfolder\.\.",   @"folder\subfolder",  @"folder\subfolder",     @"folder\subfolder" },
        { @"folder\subfolder\.\.\",  @"folder\subfolder\", @"folder\subfolder\",    @"folder\subfolder\" },
        { $@"{LongFolderName}\{LongSubfolderName}\.",     $@"{LongFolderName}\{LongSubfolderName}",  $@"{LongFolderName}\{LongSubfolderName}",     $@"{LongFolderName}\{LongSubfolderName}" },
        { $@"{LongFolderName}\{LongSubfolderName}\.\",    $@"{LongFolderName}\{LongSubfolderName}\", $@"{LongFolderName}\{LongSubfolderName}\",    $@"{LongFolderName}\{LongSubfolderName}\" },
        { $@"{LongFolderName}\{LongSubfolderName}\.\.",   $@"{LongFolderName}\{LongSubfolderName}",  $@"{LongFolderName}\{LongSubfolderName}",     $@"{LongFolderName}\{LongSubfolderName}" },
        { $@"{LongFolderName}\{LongSubfolderName}\.\.\",  $@"{LongFolderName}\{LongSubfolderName}\", $@"{LongFolderName}\{LongSubfolderName}\",    $@"{LongFolderName}\{LongSubfolderName}\" },

        { @".\folder\subfolder\.",       @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\subfolder\.\",      @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { @".\folder\subfolder\.\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\subfolder\.\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.",       $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.\",      $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.\.",     $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.\.\",    $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },

        { @".\.\folder\subfolder\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\.\folder\subfolder\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { @".\.\folder\subfolder\.\.",   @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\.\folder\subfolder\.\.\",  @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.",     $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.\",    $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.\.",   $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.\.\",  $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },

        { @".\folder\.\subfolder\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\.\subfolder\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { @".\folder\.\subfolder\.\.",   @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\.\subfolder\.\.\",  @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.",     $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.\",    $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.\.",   $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.\.\",  $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },

        { @".\folder\.\.\subfolder\.",       @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\.\.\subfolder\.\",      @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { @".\folder\.\.\subfolder\.\.",     @"folder\subfolder",    @".\folder\subfolder",     @".\folder\subfolder" },
        { @".\folder\.\.\subfolder\.\.\",    @"folder\subfolder\",   @".\folder\subfolder\",    @".\folder\subfolder\" },
        { $@".\{LongFolderName}\.\.\{LongSubfolderName}\.",       $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\.\.\{LongSubfolderName}\.\",      $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },
        { $@".\{LongFolderName}\.\.\{LongSubfolderName}\.\.",     $@"{LongFolderName}\{LongSubfolderName}",    $@".\{LongFolderName}\{LongSubfolderName}",     $@".\{LongFolderName}\{LongSubfolderName}" },
        { $@".\{LongFolderName}\.\.\{LongSubfolderName}\.\.\",    $@"{LongFolderName}\{LongSubfolderName}\",   $@".\{LongFolderName}\{LongSubfolderName}\",    $@".\{LongFolderName}\{LongSubfolderName}\" },

        { @".\file.txt",     @"file.txt",    @".\file.txt",  @".\file.txt" },
        { @".\.\file.txt",   @"file.txt",    @".\file.txt",  @".\file.txt" },

        { @".\folder\file.txt",      @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { @".\folder\.\file.txt",    @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { @".\folder\.\.\file.txt",  @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { $@".\{LongFolderName}\file.txt",      $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },
        { $@".\{LongFolderName}\.\file.txt",    $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },
        { $@".\{LongFolderName}\.\.\file.txt",  $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },

        { @".\.\folder\file.txt",        @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { @".\.\folder\.\file.txt",      @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { @".\.\folder\.\.\file.txt",    @"folder\file.txt",     @".\folder\file.txt",  @".\folder\file.txt" },
        { $@".\.\{LongFolderName}\file.txt",        $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },
        { $@".\.\{LongFolderName}\.\file.txt",      $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },
        { $@".\.\{LongFolderName}\.\.\file.txt",    $@"{LongFolderName}\file.txt",     $@".\{LongFolderName}\file.txt",  $@".\{LongFolderName}\file.txt" },

        { @"folder\.\file.txt",      @"folder\file.txt",     @"folder\file.txt",  @"folder\file.txt" },
        { @"folder\.\.\file.txt",    @"folder\file.txt",     @"folder\file.txt",  @"folder\file.txt" },
        { $@"{LongFolderName}\.\file.txt",      $@"{LongFolderName}\file.txt",     $@"{LongFolderName}\file.txt",  $@"{LongFolderName}\file.txt" },
        { $@"{LongFolderName}\.\.\file.txt",    $@"{LongFolderName}\file.txt",     $@"{LongFolderName}\file.txt",  $@"{LongFolderName}\file.txt" },

        { @"folder\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @"folder\subfolder\file.txt",  @"folder\subfolder\file.txt" },
        { @"folder\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @"folder\subfolder\file.txt",  @"folder\subfolder\file.txt" },
        { $@"{LongFolderName}\{LongSubfolderName}\.\file.txt",    $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@"{LongFolderName}\{LongSubfolderName}\file.txt",  $@"{LongFolderName}\{LongSubfolderName}\file.txt" },
        { $@"{LongFolderName}\{LongSubfolderName}\.\.\file.txt",  $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@"{LongFolderName}\{LongSubfolderName}\file.txt",  $@"{LongFolderName}\{LongSubfolderName}\file.txt" },

        { @".\folder\subfolder\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { @".\folder\subfolder\.\.\file.txt", @"folder\subfolder\file.txt",  @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.\file.txt",   $@"{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },
        { $@".\{LongFolderName}\{LongSubfolderName}\.\.\file.txt", $@"{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },

        { @".\.\folder\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { @".\.\folder\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.\file.txt",    $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },
        { $@".\.\{LongFolderName}\{LongSubfolderName}\.\.\file.txt",  $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },

        { @".\folder\.\subfolder\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { @".\folder\.\subfolder\.\.\file.txt",  @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.\file.txt",    $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\.\.\file.txt",  $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },

        { @".\.\folder\.\.\subfolder\.\file.txt",      @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { @".\.\folder\.\.\subfolder\.\.\file.txt",    @"folder\subfolder\file.txt",   @".\folder\subfolder\file.txt",  @".\folder\subfolder\file.txt" },
        { $@".\.\{LongFolderName}\.\.\{LongSubfolderName}\.\file.txt",      $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },
        { $@".\.\{LongFolderName}\.\.\{LongSubfolderName}\.\.\file.txt",    $@"{LongFolderName}\{LongSubfolderName}\file.txt",   $@".\{LongFolderName}\{LongSubfolderName}\file.txt",  $@".\{LongFolderName}\{LongSubfolderName}\file.txt" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_ServerShare_Redundant_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
    public static IEnumerable<object[]> MemberData_UNC_Redundant_SingleDot =>
        from t in TestPaths_Redundant_SingleDot
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

    #endregion

    #region Double dot

    private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_DoubleDot = new()
    {
        // The original and qualified strings must get the root string prefixed
        // Original | Qualified | Unqualified | Device prefix
        { @"..",     @"",    @"..",     @".." },
        { @"..\",    @"",    @"..\",    @"..\" },
        { @"..\..",  @"",    @"..\..",  @"..\.." },
        { @"..\..\", @"",    @"..\..\", @"..\..\" },

        { @"..\folder",          @"folder",      @"..\folder",  @"..\folder" },
        { @"..\folder\",         @"folder\",     @"..\folder\", @"..\folder\" },
        { @"..\folder\..",       @"",            @"..",         @"..\" },
        { @"..\folder\..\",      @"",            @"..\",        @"..\" },
        { @"..\folder\..\..",    @"",            @"..\..",      @"..\.." },
        { @"..\folder\..\..\",   @"",            @"..\..\",     @"..\..\" },
        { $@"..\{LongFolderName}",          $@"{LongFolderName}",      $@"..\{LongFolderName}",  $@"..\{LongFolderName}" },
        { $@"..\{LongFolderName}\",         $@"{LongFolderName}\",     $@"..\{LongFolderName}\", $@"..\{LongFolderName}\" },
        { $@"..\{LongFolderName}\..",       @"",            @"..",         @"..\" },
        { $@"..\{LongFolderName}\..\",      @"",            @"..\",        @"..\" },
        { $@"..\{LongFolderName}\..\..",    @"",            @"..\..",      @"..\.." },
        { $@"..\{LongFolderName}\..\..\",   @"",            @"..\..\",     @"..\..\" },

        { @"..\..\folder",           @"folder",      @"..\..\folder",   @"..\..\folder" },
        { @"..\..\folder\",          @"folder\",     @"..\..\folder\",  @"..\..\folder\" },
        { @"..\..\folder\..",        @"",            @"..\..",          @"..\.." },
        { @"..\..\folder\..\",       @"",            @"..\..\",         @"..\..\" },
        { @"..\..\folder\..\..",     @"",            @"..\..\..",       @"..\..\.." },
        { @"..\..\folder\..\..\",    @"",            @"..\..\..\",      @"..\..\..\" },
        { $@"..\..\{LongFolderName}",           $@"{LongFolderName}",      $@"..\..\{LongFolderName}",   $@"..\..\{LongFolderName}" },
        { $@"..\..\{LongFolderName}\",          $@"{LongFolderName}\",     $@"..\..\{LongFolderName}\",  $@"..\..\{LongFolderName}\" },
        { $@"..\..\{LongFolderName}\..",        @"",            @"..\..",          @"..\.." },
        { $@"..\..\{LongFolderName}\..\",       @"",            @"..\..\",         @"..\..\" },
        { $@"..\..\{LongFolderName}\..\..",     @"",            @"..\..\..",       @"..\..\.." },
        { $@"..\..\{LongFolderName}\..\..\",    @"",            @"..\..\..\",      @"..\..\..\" },

        { @"folder\..",          @"",    @"",       @"folder\.." },
        { @"folder\..\",         @"",    @"",       @"folder\..\" },
        { @"folder\..\..",       @"",    @"..",     @"folder\..\.." },
        { @"folder\..\..\",      @"",    @"..\",    @"folder\..\..\" },
        { @"folder\..\..\..",    @"",    @"..\..",  @"folder\..\..\.." },
        { @"folder\..\..\..\",   @"",    @"..\..\", @"folder\..\..\..\" },
        { $@"{LongFolderName}\..",          @"",    @"",       $@"{LongFolderName}\.." },
        { $@"{LongFolderName}\..\",         @"",    @"",       $@"{LongFolderName}\..\" },
        { $@"{LongFolderName}\..\..",       @"",    @"..",     $@"{LongFolderName}\..\.." },
        { $@"{LongFolderName}\..\..\",      @"",    @"..\",    $@"{LongFolderName}\..\..\" },
        { $@"{LongFolderName}\..\..\..",    @"",    @"..\..",  $@"{LongFolderName}\..\..\.." },
        { $@"{LongFolderName}\..\..\..\",   @"",    @"..\..\", $@"{LongFolderName}\..\..\..\" },

        { @"folder\subfolder\..",        @"folder",      @"folder",     @"folder\" },
        { @"folder\subfolder\..\",       @"folder\",     @"folder\",    @"folder\" },
        { @"folder\subfolder\..\..",     @"",            @"",           @"folder\.." },
        { @"folder\subfolder\..\..\",    @"",            @"",           @"folder\..\" },
        { @"folder\subfolder\..\..\..",  @"",            @"..",         @"folder\..\.." },
        { @"folder\subfolder\..\..\..\", @"",            @"..\",        @"folder\..\..\" },
        { $@"{LongFolderName}\{LongSubfolderName}\..",        $@"{LongFolderName}",      $@"{LongFolderName}",     $@"{LongFolderName}\" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\",       $@"{LongFolderName}\",     $@"{LongFolderName}\",    $@"{LongFolderName}\" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..",     @"",            @"",           $@"{LongFolderName}\.." },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..\",    @"",            @"",           $@"{LongFolderName}\..\" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..\..",  @"",            @"..",         $@"{LongFolderName}\..\.." },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..\..\", @"",            @"..\",        $@"{LongFolderName}\..\..\" },

        { @"..\folder\subfolder\..",         @"folder",      @"..\folder",  @"..\folder" },
        { @"..\folder\subfolder\..\",        @"folder\",     @"..\folder\", @"..\folder\" },
        { @"..\folder\subfolder\..\..",      @"",            @"..",         @"..\" },
        { @"..\folder\subfolder\..\..\",     @"",            @"..\",        @"..\" },
        { @"..\folder\subfolder\..\..\..",   @"",            @"..\..",      @"..\.." },
        { @"..\folder\subfolder\..\..\..\",  @"",            @"..\..\",     @"..\..\" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..",         $@"{LongFolderName}",      $@"..\{LongFolderName}",  $@"..\{LongFolderName}" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\",        $@"{LongFolderName}\",     $@"..\{LongFolderName}\", $@"..\{LongFolderName}\" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..",      @"",            @"..",         @"..\" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..\",     @"",            @"..\",        @"..\" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..\..",   @"",            @"..\..",      @"..\.." },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..\..\",  @"",            @"..\..\",     @"..\..\" },

        { @"..\folder\..\subfolder\..",          @"",    @"..",         @"..\" },
        { @"..\folder\..\subfolder\..\",         @"",    @"..\",        @"..\" },
        { @"..\folder\..\subfolder\..\..",       @"",    @"..\..",      @"..\.." },
        { @"..\folder\..\subfolder\..\..\",      @"",    @"..\..\",     @"..\..\" },
        { @"..\folder\..\subfolder\..\..\..",    @"",    @"..\..\..",   @"..\..\.." },
        { @"..\folder\..\subfolder\..\..\..\",   @"",    @"..\..\..\",  @"..\..\..\" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..",          @"",    @"..",         @"..\" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\",         @"",    @"..\",        @"..\" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..",       @"",    @"..\..",      @"..\.." },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..\",      @"",    @"..\..\",     @"..\..\" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..\..",    @"",    @"..\..\..",   @"..\..\.." },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..\..\",   @"",    @"..\..\..\",  @"..\..\..\" },

        { @"..\folder\..\..\subfolder\..",           @"",    @"..\..",          @"..\.." },
        { @"..\folder\..\..\subfolder\..\",          @"",    @"..\..\",         @"..\..\" },
        { @"..\folder\..\..\subfolder\..\..",        @"",    @"..\..\..",       @"..\..\.." },
        { @"..\folder\..\..\subfolder\..\..\",       @"",    @"..\..\..\",      @"..\..\..\" },
        { @"..\folder\..\..\subfolder\..\..\..",     @"",    @"..\..\..\..",    @"..\..\..\.." },
        { @"..\folder\..\..\subfolder\..\..\..\",    @"",    @"..\..\..\..\",   @"..\..\..\..\" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..",           @"",    @"..\..",          @"..\.." },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\",          @"",    @"..\..\",         @"..\..\" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..",        @"",    @"..\..\..",       @"..\..\.." },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..\",       @"",    @"..\..\..\",      @"..\..\..\" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..\..",     @"",    @"..\..\..\..",    @"..\..\..\.." },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..\..\",    @"",    @"..\..\..\..\",   @"..\..\..\..\" },

        { @"..\file.txt",    @"file.txt",    @"..\file.txt",    @"..\file.txt" },
        { @"..\..\file.txt", @"file.txt",    @"..\..\file.txt", @"..\..\file.txt" },

        { @"..\folder\file.txt",         @"folder\file.txt",     @"..\folder\file.txt", @"..\folder\file.txt" },
        { @"..\folder\..\file.txt",      @"file.txt",            @"..\file.txt",        @"..\file.txt" },
        { @"..\folder\..\..\file.txt",   @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },
        { $@"..\{LongFolderName}\file.txt",         $@"{LongFolderName}\file.txt",     $@"..\{LongFolderName}\file.txt", $@"..\{LongFolderName}\file.txt" },
        { $@"..\{LongFolderName}\..\file.txt",      @"file.txt",            @"..\file.txt",        @"..\file.txt" },
        { $@"..\{LongFolderName}\..\..\file.txt",   @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },

        { @"..\..\folder\file.txt",          @"folder\file.txt",     @"..\..\folder\file.txt",  @"..\..\folder\file.txt" },
        { @"..\..\folder\..\file.txt",       @"file.txt",            @"..\..\file.txt",         @"..\..\file.txt" },
        { @"..\..\folder\..\..\file.txt",    @"file.txt",            @"..\..\..\file.txt",      @"..\..\..\file.txt" },
        { $@"..\..\{LongFolderName}\file.txt",          $@"{LongFolderName}\file.txt",     $@"..\..\{LongFolderName}\file.txt",  $@"..\..\{LongFolderName}\file.txt" },
        { $@"..\..\{LongFolderName}\..\file.txt",       @"file.txt",            @"..\..\file.txt",         @"..\..\file.txt" },
        { $@"..\..\{LongFolderName}\..\..\file.txt",    @"file.txt",            @"..\..\..\file.txt",      @"..\..\..\file.txt" },

        { @"folder\..\file.txt",         @"file.txt",    @"file.txt",       @"folder\..\file.txt" },
        { @"folder\..\..\file.txt",      @"file.txt",    @"..\file.txt",    @"folder\..\..\file.txt" },
        { @"folder\..\..\..\file.txt",   @"file.txt",    @"..\..\file.txt", @"folder\..\..\..\file.txt" },
        { $@"{LongFolderName}\..\file.txt",         @"file.txt",    @"file.txt",       $@"{LongFolderName}\..\file.txt" },
        { $@"{LongFolderName}\..\..\file.txt",      @"file.txt",    @"..\file.txt",    $@"{LongFolderName}\..\..\file.txt" },
        { $@"{LongFolderName}\..\..\..\file.txt",   @"file.txt",    @"..\..\file.txt", $@"{LongFolderName}\..\..\..\file.txt" },

        { @"folder\subfolder\..\file.txt",       @"folder\file.txt",     @"folder\file.txt",    @"folder\file.txt" },
        { @"folder\subfolder\..\..\file.txt",    @"file.txt",            @"file.txt",           @"folder\..\file.txt" },
        { @"folder\subfolder\..\..\..\file.txt", @"file.txt",            @"..\file.txt",        @"folder\..\..\file.txt" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\file.txt",       $@"{LongFolderName}\file.txt",     $@"{LongFolderName}\file.txt",    $@"{LongFolderName}\file.txt" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..\file.txt",    @"file.txt",            @"file.txt",           $@"{LongFolderName}\..\file.txt" },
        { $@"{LongFolderName}\{LongSubfolderName}\..\..\..\file.txt", @"file.txt",            @"..\file.txt",        $@"{LongFolderName}\..\..\file.txt" },

        { @"..\folder\subfolder\..\file.txt",        @"folder\file.txt",     @"..\folder\file.txt", @"..\folder\file.txt" },
        { @"..\folder\subfolder\..\..\file.txt",     @"file.txt",            @"..\file.txt",        @"..\file.txt" },
        { @"..\folder\subfolder\..\..\..\file.txt",  @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\file.txt",        $@"{LongFolderName}\file.txt",     $@"..\{LongFolderName}\file.txt", $@"..\{LongFolderName}\file.txt" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..\file.txt",     @"file.txt",            @"..\file.txt",        @"..\file.txt" },
        { $@"..\{LongFolderName}\{LongSubfolderName}\..\..\..\file.txt",  @"file.txt",            @"..\..\file.txt",     @"..\..\file.txt" },

        { @"..\folder\..\subfolder\..\file.txt",         @"file.txt",    @"..\file.txt",        @"..\file.txt" },
        { @"..\folder\..\subfolder\..\..\file.txt",      @"file.txt",    @"..\..\file.txt",     @"..\..\file.txt" },
        { @"..\folder\..\subfolder\..\..\..\file.txt",   @"file.txt",    @"..\..\..\file.txt",  @"..\..\..\file.txt" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\file.txt",         @"file.txt",    @"..\file.txt",        @"..\file.txt" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..\file.txt",      @"file.txt",    @"..\..\file.txt",     @"..\..\file.txt" },
        { $@"..\{LongFolderName}\..\{LongSubfolderName}\..\..\..\file.txt",   @"file.txt",    @"..\..\..\file.txt",  @"..\..\..\file.txt" },

        { @"..\folder\..\..\subfolder\..\file.txt",          @"file.txt",    @"..\..\file.txt",         @"..\..\file.txt" },
        { @"..\folder\..\..\subfolder\..\..\file.txt",       @"file.txt",    @"..\..\..\file.txt",      @"..\..\..\file.txt" },
        { @"..\folder\..\..\subfolder\..\..\..\file.txt",    @"file.txt",    @"..\..\..\..\file.txt",   @"..\..\..\..\file.txt" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\file.txt",          @"file.txt",    @"..\..\file.txt",         @"..\..\file.txt" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..\file.txt",       @"file.txt",    @"..\..\..\file.txt",      @"..\..\..\file.txt" },
        { $@"..\{LongFolderName}\..\..\{LongSubfolderName}\..\..\..\file.txt",    @"file.txt",    @"..\..\..\..\file.txt",   @"..\..\..\..\file.txt" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_ServerShare_Redundant_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
    public static IEnumerable<object[]> MemberData_UNC_Redundant_DoubleDot =>
        from t in TestPaths_Redundant_DoubleDot
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

    #endregion

    #region Combined: single + double dot

    private static readonly List<Tuple<string, string, string, string>> TestPaths_Redundant_Combined = new()
    {
        // The original and qualified strings must get the root string prefixed
        // Original | Qualified | Unqualified | Device prefix
        { @"..\.",     @"",    @"..",       @"..\" },
        { @"..\.\",    @"",    @"..\",      @"..\" },
        { @"..\..\.",  @"",    @"..\..",    @"..\.." },
        { @"..\..\.\", @"",    @"..\..\",   @"..\..\" },

        //{ @".\..\.",     @"",    @".\..",       @".\.." },
        //{ @".\..\.\",    @"",    @".\..\",      @".\..\" },
        //{ @".\..\..\.",  @"",    @".\..\..",    @".\..\.." },
        //{ @".\..\..\.\", @"",    @".\..\..\",   @".\..\..\" },
        { @".\..\.",     @"",    @"..",       @".\.." },
        { @".\..\.\",    @"",    @"..\",      @".\..\" },
        { @".\..\..\.",  @"",    @"..\..",    @".\..\.." },
        { @".\..\..\.\", @"",    @"..\..\",   @".\..\..\" },

        { @"..\.\file.txt",         @"file.txt",    @"..\file.txt",     @"..\file.txt" },
        { @"..\.\..\file.txt",      @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },
        { @"..\..\.\file.txt",      @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },
        { @"..\.\..\.\file.txt",    @"file.txt",    @"..\..\file.txt",  @"..\..\file.txt" },

        //{ @".\..\.\file.txt",         @"file.txt",    @".\..\file.txt",     @".\..\file.txt" },
        //{ @".\..\.\..\file.txt",      @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },
        //{ @".\..\..\.\file.txt",      @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },
        //{ @".\..\.\..\.\file.txt",    @"file.txt",    @".\..\..\file.txt",  @".\..\..\file.txt" },
        { @".\..\.\file.txt",         @"file.txt",    @"..\file.txt",     @".\..\file.txt" },
        { @".\..\.\..\file.txt",      @"file.txt",    @"..\..\file.txt",  @".\..\..\file.txt" },
        { @".\..\..\.\file.txt",      @"file.txt",    @"..\..\file.txt",  @".\..\..\file.txt" },
        { @".\..\.\..\.\file.txt",    @"file.txt",    @"..\..\file.txt",  @".\..\..\file.txt" },

        { @"..\.\folder",          @"folder",      @"..\folder",    @"..\folder" },
        { @"..\.\folder\",         @"folder\",     @"..\folder\",   @"..\folder\" },
        { @"..\.\folder\..",       @"",            @"..",           @"..\" },
        { @"..\.\folder\..\",      @"",            @"..\",          @"..\" },
        { @"..\.\folder\..\..",    @"",            @"..\..",        @"..\.." },
        { @"..\.\folder\..\..\",   @"",            @"..\..\",       @"..\..\" },
        { $@"..\.\{LongFolderName}",          $@"{LongFolderName}",      $@"..\{LongFolderName}",    $@"..\{LongFolderName}" },
        { $@"..\.\{LongFolderName}\",         $@"{LongFolderName}\",     $@"..\{LongFolderName}\",   $@"..\{LongFolderName}\" },
        { $@"..\.\{LongFolderName}\..",       @"",            @"..",           @"..\" },
        { $@"..\.\{LongFolderName}\..\",      @"",            @"..\",          @"..\" },
        { $@"..\.\{LongFolderName}\..\..",    @"",            @"..\..",        @"..\.." },
        { $@"..\.\{LongFolderName}\..\..\",   @"",            @"..\..\",       @"..\..\" },

        { @"..\folder\.",          @"folder",      @"..\folder",    @"..\folder" },
        { @"..\folder\.\",         @"folder\",     @"..\folder\",   @"..\folder\" },
        { @"..\folder\.\..",       @"",            @"..",           @"..\" },
        { @"..\folder\.\..\",      @"",            @"..\",          @"..\" },
        { @"..\folder\.\..\..",    @"",            @"..\..",        @"..\.." },
        { @"..\folder\.\..\..\",   @"",            @"..\..\",       @"..\..\" },
        { $@"..\{LongFolderName}\.",          $@"{LongFolderName}",      $@"..\{LongFolderName}",    $@"..\{LongFolderName}" },
        { $@"..\{LongFolderName}\.\",         $@"{LongFolderName}\",     $@"..\{LongFolderName}\",   $@"..\{LongFolderName}\" },
        { $@"..\{LongFolderName}\.\..",       @"",            @"..",           @"..\" },
        { $@"..\{LongFolderName}\.\..\",      @"",            @"..\",          @"..\" },
        { $@"..\{LongFolderName}\.\..\..",    @"",            @"..\..",        @"..\.." },
        { $@"..\{LongFolderName}\.\..\..\",   @"",            @"..\..\",       @"..\..\" },

        { @"folder\.\subfolder\..",        @"folder",      @"folder",   @"folder\" },
        { @"folder\.\subfolder\..\",       @"folder\",     @"folder\",  @"folder\" },
        { @"folder\.\subfolder\..\..",     @"",            @"",         @"folder\.." },
        { @"folder\.\subfolder\..\..\",    @"",            @"",         @"folder\..\" },
        { @"folder\.\subfolder\..\..\..",  @"",            @"..",       @"folder\..\.." },
        { @"folder\.\subfolder\..\..\..\", @"",            @"..\",      @"folder\..\..\" },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..",        $@"{LongFolderName}",      $@"{LongFolderName}",   $@"{LongFolderName}\" },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..\",       $@"{LongFolderName}\",     $@"{LongFolderName}\",  $@"{LongFolderName}\" },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..\..",     @"",            @"",         $@"{LongFolderName}\.." },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..\..\",    @"",            @"",         $@"{LongFolderName}\..\" },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..\..\..",  @"",            @"..",       $@"{LongFolderName}\..\.." },
        { $@"{LongFolderName}\.\{LongSubfolderName}\..\..\..\", @"",            @"..\",      $@"{LongFolderName}\..\..\" },

        { @".\folder\.\subfolder\..",        @"folder",      @".\folder",   @".\folder" },
        { @".\folder\.\subfolder\..\",       @"folder\",     @".\folder\",  @".\folder\" },
        { @".\folder\.\subfolder\..\..",     @"",            @".",          @".\" },
        { @".\folder\.\subfolder\..\..\",    @"",            @".\",         @".\" },
        //{ @".\folder\.\subfolder\..\..\..",  @"",            @".\..",       @".\.." },
        //{ @".\folder\.\subfolder\..\..\..\", @"",            @".\..\",      @".\..\" },
        { @".\folder\.\subfolder\..\..\..",  @"",            @"..",       @".\.." },
        { @".\folder\.\subfolder\..\..\..\", @"",            @"..\",      @".\..\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..",        $@"{LongFolderName}",      $@".\{LongFolderName}",   $@".\{LongFolderName}" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..\",       $@"{LongFolderName}\",     $@".\{LongFolderName}\",  $@".\{LongFolderName}\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..\..",     @"",            @".",          @".\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..\..\",    @"",            @".\",         @".\" },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..\..\..",  @"",            @"..",       @".\.." },
        { $@".\{LongFolderName}\.\{LongSubfolderName}\..\..\..\", @"",            @"..\",      @".\..\" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item3 };
    public static IEnumerable<object[]> MemberData_ServerShare_Redundant_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 };
    public static IEnumerable<object[]> MemberData_UNC_Redundant_Combined =>
        from t in TestPaths_Redundant_Combined
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item2 };

    #endregion

    #region Edge cases: more than two dots, paths with trailing dot

    private static readonly List<Tuple<string, string, string, string>> TestPaths_NoRedundancy_EdgeCases = new()
    {
        // Original | Qualified | Unqualified | Device prefix
        
        // Trailing more than 2 dots
        { @"...",           @"",                @"",                @"..." },
        { @"...\",          @"...\",            @"...\",            @"...\" },
        { @"folder\...",    @"folder\",         @"folder\",         @"folder\..." },
        { @"folder\...\",   @"folder\...\",     @"folder\...\",     @"folder\...\" },
        { $@"{LongFolderName}\...",    $@"{LongFolderName}\",         $@"{LongFolderName}\",         $@"{LongFolderName}\..." },
        { $@"{LongFolderName}\...\",   $@"{LongFolderName}\...\",     $@"{LongFolderName}\...\",     $@"{LongFolderName}\...\" },

        { @"....",          @"",                @"",                @"...." },
        { @"....\",         @"....\",           @"....\",           @"....\" },
        { @"folder\....",   @"folder\",         @"folder\",         @"folder\...." },
        { @"folder\....\",  @"folder\....\",    @"folder\....\",    @"folder\....\" },
        { $@"{LongFolderName}\....",   $@"{LongFolderName}\",         $@"{LongFolderName}\",         $@"{LongFolderName}\...." },
        { $@"{LongFolderName}\....\",  $@"{LongFolderName}\....\",    $@"{LongFolderName}\....\",    $@"{LongFolderName}\....\" },

        // Starting with more than 2 dots
        { @"...\subfolder",             @"...\subfolder",           @"...\subfolder",          @"...\subfolder" },
        { @"...\subfolder\",            @"...\subfolder\",          @"...\subfolder\",         @"...\subfolder\" },
        { @"...\file.txt",              @"...\file.txt",            @"...\file.txt",           @"...\file.txt" },
        { @"...\subfolder\file.txt",    @"...\subfolder\file.txt",  @"...\subfolder\file.txt", @"...\subfolder\file.txt" },
        { $@"...\{LongSubfolderName}",             $@"...\{LongSubfolderName}",           $@"...\{LongSubfolderName}",          $@"...\{LongSubfolderName}" },
        { $@"...\{LongSubfolderName}\",            $@"...\{LongSubfolderName}\",          $@"...\{LongSubfolderName}\",         $@"...\{LongSubfolderName}\" },
        { $@"...\{LongSubfolderName}\file.txt",    $@"...\{LongSubfolderName}\file.txt",  $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt" },

        { @"....\subfolder",            @"....\subfolder",          @"....\subfolder",          @"....\subfolder" },
        { @"....\subfolder\",           @"....\subfolder\",         @"....\subfolder\",         @"....\subfolder\" },
        { @"....\file.txt",             @"....\file.txt",           @"....\file.txt",           @"....\file.txt" },
        { @"....\subfolder\file.txt",   @"....\subfolder\file.txt", @"....\subfolder\file.txt", @"....\subfolder\file.txt" },
        { $@"....\{LongSubfolderName}",            $@"....\{LongSubfolderName}",          $@"....\{LongSubfolderName}",          $@"....\{LongSubfolderName}" },
        { $@"....\{LongSubfolderName}\",           $@"....\{LongSubfolderName}\",         $@"....\{LongSubfolderName}\",         $@"....\{LongSubfolderName}\" },
        { $@"....\{LongSubfolderName}\file.txt",   $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt" },

        // file/folder ending in dot
        { @"dot.",               @"dot",            @"dot",             @"dot." },
        { @"dot.\",              @"dot\",           @"dot\",            @"dot.\" },
        { @"folder\dot.",        @"folder\dot",     @"folder\dot",      @"folder\dot." },
        { @"folder\dot.\",       @"folder\dot\",    @"folder\dot\",     @"folder\dot.\" },
        { @"dot.\subfolder",     @"dot\subfolder",  @"dot\subfolder",   @"dot.\subfolder" },
        { @"dot.\subfolder\",    @"dot\subfolder\", @"dot\subfolder\",  @"dot.\subfolder\" },
        { $@"{LongFolderName}\dot.",  $@"{LongFolderName}\dot",  $@"{LongFolderName}\dot",  $@"{LongFolderName}\dot." },
        { $@"{LongFolderName}\dot.\", $@"{LongFolderName}\dot\", $@"{LongFolderName}\dot\", $@"{LongFolderName}\dot.\" },
        { $@"dot.\{LongSubfolderName}",  $@"dot\{LongSubfolderName}",  $@"dot\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}" },
        { $@"dot.\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\" },

        { @"dot.\file.txt",              @"dot\file.txt",            @"dot\file.txt",           @"dot.\file.txt" },
        { @"dot.\subfolder\file.txt",    @"dot\subfolder\file.txt",  @"dot\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        { $@"dot.\{LongSubfolderName}\file.txt", $@"dot\{LongSubfolderName}\file.txt",  $@"dot\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_DriveAndRoot_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_NoRedundancy_Prefix_DriveAndRoot_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DriveRootless_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_Prefix_DriveRootless_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_NoRedundancy_DrivelessRoot_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_ServerShare_NoRedundancy_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
    public static IEnumerable<object[]> MemberData_UNC_NoRedundancy_EdgeCases =>
        from t in TestPaths_NoRedundancy_EdgeCases
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item4 };

    #endregion

    #region Edge cases + single dot

    private static readonly List<Tuple<string, string, string, string, string>> TestPaths_Redundant_SingleDot_EdgeCases = new()
    {
        // The original and qualified strings must get the root string prefixed
        // Original | Qualified | Unqualified | Device unrooted | Device rooted

        // Folder with 3 dots
        { @"...\.",      @"",     @"",      @"...\",  @"..." },
        { @"...\.\",     @"...\", @"...\",  @"...\",  @"...\" },
        { @"...\.\.",    @"",     @"",      @"...\",  @"..." },
        { @"...\.\.\",   @"...\", @"...\",  @"...\",  @"...\" },

        { @"...\subfolder\.",        @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
        { @"...\subfolder\.\",       @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
        { @"...\subfolder\.\.",      @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
        { @"...\subfolder\.\.\",     @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
        { @"...\.\subfolder\.\",     @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
        { @"...\.\subfolder\.\.",    @"...\subfolder",     @"...\subfolder",    @"...\subfolder",   @"...\subfolder" },
        { @"...\.\subfolder\.\.\",   @"...\subfolder\",    @"...\subfolder\",   @"...\subfolder\",  @"...\subfolder\" },
        { $@"...\{LongSubfolderName}\.",     $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}" },
        { $@"...\{LongSubfolderName}\.\",    $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\" },
        { $@"...\{LongSubfolderName}\.\.",   $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}" },
        { $@"...\{LongSubfolderName}\.\.\",  $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\" },
        { $@"...\.\{LongSubfolderName}\.\",   $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\" },
        { $@"...\.\{LongSubfolderName}\.\.",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}",  $@"...\{LongSubfolderName}" },
        { $@"...\.\{LongSubfolderName}\.\.\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\", $@"...\{LongSubfolderName}\" },

        { @"...\.\file.txt",                 @"...\file.txt",              @"...\file.txt",             @"...\file.txt",            @"...\file.txt" },
        { @"...\.\.\file.txt",               @"...\file.txt",              @"...\file.txt",             @"...\file.txt",            @"...\file.txt" },
        { @"...\subfolder\.\file.txt",       @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
        { @"...\subfolder\.\.\file.txt",     @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
        { @"...\.\subfolder\.\file.txt",     @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
        { @"...\.\subfolder\.\.\file.txt",   @"...\subfolder\file.txt",    @"...\subfolder\file.txt",   @"...\subfolder\file.txt",  @"...\subfolder\file.txt" },
        { $@"...\{LongSubfolderName}\.\file.txt",   $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt" },
        { $@"...\{LongSubfolderName}\.\.\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt" },
        { $@"...\.\{LongSubfolderName}\.\file.txt",   $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt" },
        { $@"...\.\{LongSubfolderName}\.\.\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt", $@"...\{LongSubfolderName}\file.txt" },

        // Folder with 4 dots
        { @"....\.",     @"",      @"",    @"....\",   @"...." },
        { @"....\.\",    @"....\", @"....\",    @"....\",   @"....\" },
        { @"....\.\.",   @"",      @"",    @"....\",   @"...." },
        { @"....\.\.\",  @"....\", @"....\",    @"....\",   @"....\" },

        { @"....\subfolder\.",       @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
        { @"....\subfolder\.\",      @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
        { @"....\subfolder\.\.",     @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
        { @"....\subfolder\.\.\",    @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
        { @"....\.\subfolder\.\",    @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
        { @"....\.\subfolder\.\.",   @"....\subfolder",     @"....\subfolder",      @"....\subfolder",   @"....\subfolder" },
        { @"....\.\subfolder\.\.\",  @"....\subfolder\",    @"....\subfolder\",     @"....\subfolder\",  @"....\subfolder\" },
        { $@"....\{LongSubfolderName}\.",    $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}" },
        { $@"....\{LongSubfolderName}\.\",   $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\" },
        { $@"....\{LongSubfolderName}\.\.",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}" },
        { $@"....\{LongSubfolderName}\.\.\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\" },
        { $@"....\.\{LongSubfolderName}\.\",   $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\" },
        { $@"....\.\{LongSubfolderName}\.\.",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}",  $@"....\{LongSubfolderName}" },
        { $@"....\.\{LongSubfolderName}\.\.\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\", $@"....\{LongSubfolderName}\" },

        { @"....\.\file.txt",                @"....\file.txt",              @"....\file.txt",               @"....\file.txt",            @"....\file.txt" },
        { @"....\.\.\file.txt",              @"....\file.txt",              @"....\file.txt",               @"....\file.txt",            @"....\file.txt" },
        { @"....\subfolder\.\file.txt",      @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
        { @"....\subfolder\.\.\file.txt",    @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
        { @"....\.\subfolder\.\file.txt",    @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
        { @"....\.\subfolder\.\.\file.txt",  @"....\subfolder\file.txt",    @"....\subfolder\file.txt",     @"....\subfolder\file.txt",  @"....\subfolder\file.txt" },
        { $@"....\{LongSubfolderName}\.\file.txt",   $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt" },
        { $@"....\{LongSubfolderName}\.\.\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt" },
        { $@"....\.\{LongSubfolderName}\.\file.txt",   $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt" },
        { $@"....\.\{LongSubfolderName}\.\.\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt", $@"....\{LongSubfolderName}\file.txt" },
        
        // file/folder ending in dot
        { @"dot.\.",     @"dot",    @"dot",     @"dot.\",  @"dot." },
        { @"dot.\.\",    @"dot\",   @"dot\",    @"dot.\",  @"dot.\" },
        { @"dot.\.\.",   @"dot",    @"dot",     @"dot.\",  @"dot." },
        { @"dot.\.\.\",  @"dot\",   @"dot\",    @"dot.\",  @"dot.\" },

        { @"dot.\subfolder\.",       @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
        { @"dot.\subfolder\.\",      @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
        { @"dot.\subfolder\.\.",     @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
        { @"dot.\subfolder\.\.\",    @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
        { @"dot.\.\subfolder\.\",    @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
        { @"dot.\.\subfolder\.\.",   @"dot\subfolder",     @"dot\subfolder",    @"dot.\subfolder",   @"dot.\subfolder" },
        { @"dot.\.\subfolder\.\.\",  @"dot\subfolder\",    @"dot\subfolder\",   @"dot.\subfolder\",  @"dot.\subfolder\" },
        { $@"dot.\{LongSubfolderName}\.",    $@"dot\{LongSubfolderName}",  $@"dot\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}" },
        { $@"dot.\{LongSubfolderName}\.\",   $@"dot\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\" },
        { $@"dot.\{LongSubfolderName}\.\.",  $@"dot\{LongSubfolderName}",  $@"dot\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}" },
        { $@"dot.\{LongSubfolderName}\.\.\", $@"dot\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\" },
        { $@"dot.\.\{LongSubfolderName}\.\",   $@"dot\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\" },
        { $@"dot.\.\{LongSubfolderName}\.\.",  $@"dot\{LongSubfolderName}",  $@"dot\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}",  $@"dot.\{LongSubfolderName}" },
        { $@"dot.\.\{LongSubfolderName}\.\.\", $@"dot\{LongSubfolderName}\", $@"dot\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\", $@"dot.\{LongSubfolderName}\" },

        { @"dot.\.\file.txt",                @"dot\file.txt",              @"dot\file.txt",             @"dot.\file.txt",           @"dot.\file.txt" },
        { @"dot.\.\.\file.txt",              @"dot\file.txt",              @"dot\file.txt",             @"dot.\file.txt",           @"dot.\file.txt" },
        { @"dot.\subfolder\.\file.txt",      @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        { @"dot.\subfolder\.\.\file.txt",    @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        { @"dot.\.\subfolder\.\file.txt",    @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        { @"dot.\.\subfolder\.\.\file.txt",  @"dot\subfolder\file.txt",    @"dot\subfolder\file.txt",   @"dot.\subfolder\file.txt", @"dot.\subfolder\file.txt" },
        { $@"dot.\{LongSubfolderName}\.\file.txt",     $@"dot\{LongSubfolderName}\file.txt", $@"dot\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt" },
        { $@"dot.\{LongSubfolderName}\.\.\file.txt",   $@"dot\{LongSubfolderName}\file.txt", $@"dot\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt" },
        { $@"dot.\.\{LongSubfolderName}\.\file.txt",   $@"dot\{LongSubfolderName}\file.txt", $@"dot\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt" },
        { $@"dot.\.\{LongSubfolderName}\.\.\file.txt", $@"dot\{LongSubfolderName}\file.txt", $@"dot\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt", $@"dot.\{LongSubfolderName}\file.txt" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item5 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_ServerShare_Redundant_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
    public static IEnumerable<object[]> MemberData_UNC_Redundant_SingleDot_EdgeCases =>
        from t in TestPaths_Redundant_SingleDot_EdgeCases
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item5 };

    #endregion

    #region Edge cases + double dot

    private static readonly List<Tuple<string, string, string, string, string>> TestPaths_Redundant_DoubleDot_EdgeCases = new()
    {
        // Original | Qualified | Unqualified | Device prefix unrooted | Device prefix rooted
        // Folder with 3 dots
        { @"...\..",     @"",    @"",       @"...\..",      @"" },
        { @"...\..\",    @"",    @"",       @"...\..\",     @"" },
        { @"...\..\..",  @"",    @"..",     @"...\..\..",   @"" },
        { @"...\..\..\", @"",    @"..\",    @"...\..\..\",  @"" },

        { @"...\subfolder\..",           @"",        @"",       @"...\",       @"..." },
        { @"...\subfolder\..\",          @"...\",    @"...\",   @"...\",       @"...\" },
        { @"...\subfolder\..\..",        @"",        @"",       @"...\..",     @"" },
        { @"...\subfolder\..\..\",       @"",        @"",       @"...\..\",    @"" },
        { @"...\..\subfolder\..\",       @"",        @"",       @"...\..\",    @"" },
        { @"...\..\subfolder\..\..",     @"",        @"..",     @"...\..\..",  @"" },
        { @"...\..\subfolder\..\..\",    @"",        @"..\",    @"...\..\..\", @"" },
        { $@"...\{LongSubfolderName}\..",           @"",        @"",       @"...\",       @"..." },
        { $@"...\{LongSubfolderName}\..\",          @"...\",    @"...\",   @"...\",       @"...\" },
        { $@"...\{LongSubfolderName}\..\..",        @"",        @"",       @"...\..",     @"" },
        { $@"...\{LongSubfolderName}\..\..\",       @"",        @"",       @"...\..\",    @"" },
        { $@"...\..\{LongSubfolderName}\..\",       @"",        @"",       @"...\..\",    @"" },
        { $@"...\..\{LongSubfolderName}\..\..",     @"",        @"..",     @"...\..\..",  @"" },
        { $@"...\..\{LongSubfolderName}\..\..\",    @"",        @"..\",    @"...\..\..\", @"" },

        { @"...\..\file.txt",                    @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
        { @"...\..\..\file.txt",                 @"file.txt",        @"..\file.txt",    @"...\..\..\file.txt", @"file.txt" },
        { @"...\subfolder\..\file.txt",          @"...\file.txt",    @"...\file.txt",   @"...\file.txt",       @"...\file.txt" },
        { @"...\subfolder\..\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
        { @"...\..\subfolder\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
        { @"...\..\subfolder\..\..\file.txt",    @"file.txt",        @"..\file.txt",    @"...\..\..\file.txt", @"file.txt" },
        { $@"...\{LongSubfolderName}\..\file.txt",          @"...\file.txt",    @"...\file.txt",   @"...\file.txt",       @"...\file.txt" },
        { $@"...\{LongSubfolderName}\..\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
        { $@"...\..\{LongSubfolderName}\..\file.txt",       @"file.txt",        @"file.txt",       @"...\..\file.txt",    @"file.txt" },
        { $@"...\..\{LongSubfolderName}\..\..\file.txt",    @"file.txt",        @"..\file.txt",    @"...\..\..\file.txt", @"file.txt" },

        // Folder with 4 dots
        { @"....\..",        @"",    @"",       @"....\..",     @"" },
        { @"....\..\",       @"",    @"",       @"....\..\",    @"" },
        { @"....\..\..",     @"",    @"..",     @"....\..\..",  @"" },
        { @"....\..\..\",    @"",    @"..\",    @"....\..\..\", @"" },

        { @"....\subfolder\..",          @"",       @"",        @"....\",       @"...." },
        { @"....\subfolder\..\",         @"....\",  @"....\",   @"....\",       @"....\" },
        { @"....\subfolder\..\..",       @"",       @"",        @"....\..",     @"" },
        { @"....\subfolder\..\..\",      @"",       @"",        @"....\..\",    @"" },
        { @"....\..\subfolder\..\",      @"",       @"",        @"....\..\",    @"" },
        { @"....\..\subfolder\..\..",    @"",       @"..",      @"....\..\..",  @"" },
        { @"....\..\subfolder\..\..\",   @"",       @"..\",     @"....\..\..\", @"" },
        { $@"....\{LongSubfolderName}\..",          @"",       @"",        @"....\",       @"...." },
        { $@"....\{LongSubfolderName}\..\",         @"....\",  @"....\",   @"....\",       @"....\" },
        { $@"....\{LongSubfolderName}\..\..",       @"",       @"",        @"....\..",     @"" },
        { $@"....\{LongSubfolderName}\..\..\",      @"",       @"",        @"....\..\",    @"" },
        { $@"....\..\{LongSubfolderName}\..\",      @"",       @"",        @"....\..\",    @"" },
        { $@"....\..\{LongSubfolderName}\..\..",    @"",       @"..",      @"....\..\..",  @"" },
        { $@"....\..\{LongSubfolderName}\..\..\",   @"",       @"..\",     @"....\..\..\", @"" },

        { @"....\..\file.txt",                   @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
        { @"....\..\..\file.txt",                @"file.txt",        @"..\file.txt",    @"....\..\..\file.txt", @"file.txt" },
        { @"....\subfolder\..\file.txt",         @"....\file.txt",   @"....\file.txt",  @"....\file.txt",       @"....\file.txt" },
        { @"....\subfolder\..\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
        { @"....\..\subfolder\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
        { @"....\..\subfolder\..\..\file.txt",   @"file.txt",        @"..\file.txt",    @"....\..\..\file.txt", @"file.txt" },
        { $@"....\{LongSubfolderName}\..\file.txt",         @"....\file.txt",   @"....\file.txt",  @"....\file.txt",       @"....\file.txt" },
        { $@"....\{LongSubfolderName}\..\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
        { $@"....\..\{LongSubfolderName}\..\file.txt",      @"file.txt",        @"file.txt",       @"....\..\file.txt",    @"file.txt" },
        { $@"....\..\{LongSubfolderName}\..\..\file.txt",   @"file.txt",        @"..\file.txt",    @"....\..\..\file.txt", @"file.txt" },
        
        // file/folder ending in dot
        { @"dot.\..",     @"",    @"",      @"dot.\..",      @"" },
        { @"dot.\..\",    @"",    @"",      @"dot.\..\",      @"" },
        { @"dot.\..\..",  @"",    @"..",    @"dot.\..\..",   @"" },
        { @"dot.\..\..\", @"",    @"..\",   @"dot.\..\..\",  @"" },

        { @"dot.\subfolder\..",          @"dot",     @"dot",    @"dot.\",       @"dot." },
        { @"dot.\subfolder\..\",         @"dot\",    @"dot\",   @"dot.\",       @"dot.\" },
        { @"dot.\subfolder\..\..",       @"",        @"",       @"dot.\..",     @"" },
        { @"dot.\subfolder\..\..\",      @"",        @"",       @"dot.\..\",    @"" },
        { @"dot.\..\subfolder\..\",      @"",        @"",       @"dot.\..\",    @"" },
        { @"dot.\..\subfolder\..\..",    @"",        @"..",     @"dot.\..\..",  @"" },
        { @"dot.\..\subfolder\..\..\",   @"",        @"..\",    @"dot.\..\..\", @"" },
        { $@"dot.\{LongSubfolderName}\..",          @"dot",     @"dot",    @"dot.\",       @"dot." },
        { $@"dot.\{LongSubfolderName}\..\",         @"dot\",    @"dot\",   @"dot.\",       @"dot.\" },
        { $@"dot.\{LongSubfolderName}\..\..",       @"",        @"",       @"dot.\..",     @"" },
        { $@"dot.\{LongSubfolderName}\..\..\",      @"",        @"",       @"dot.\..\",    @"" },
        { $@"dot.\..\{LongSubfolderName}\..\",      @"",        @"",       @"dot.\..\",    @"" },
        { $@"dot.\..\{LongSubfolderName}\..\..",    @"",        @"..",     @"dot.\..\..",  @"" },
        { $@"dot.\..\{LongSubfolderName}\..\..\",   @"",        @"..\",    @"dot.\..\..\", @"" },

        { @"dot.\..\file.txt",                   @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
        { @"dot.\..\..\file.txt",                @"file.txt",       @"..\file.txt",    @"dot.\..\..\file.txt",  @"file.txt" },
        { @"dot.\subfolder\..\file.txt",         @"dot\file.txt",   @"dot\file.txt",   @"dot.\file.txt",        @"dot.\file.txt" },
        { @"dot.\subfolder\..\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
        { @"dot.\..\subfolder\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
        { @"dot.\..\subfolder\..\..\file.txt",   @"file.txt",       @"..\file.txt",    @"dot.\..\..\file.txt",  @"file.txt" },
        { $@"dot.\{LongSubfolderName}\..\file.txt",         @"dot\file.txt",   @"dot\file.txt",   @"dot.\file.txt",        @"dot.\file.txt" },
        { $@"dot.\{LongSubfolderName}\..\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
        { $@"dot.\..\{LongSubfolderName}\..\file.txt",      @"file.txt",       @"file.txt",       @"dot.\..\file.txt",     @"file.txt" },
        { $@"dot.\..\{LongSubfolderName}\..\..\file.txt",   @"file.txt",       @"..\file.txt",    @"dot.\..\..\file.txt",  @"file.txt" },
    };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_DriveAndRoot_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { Prefix_Windows_Drive_Root + t.Item1, Prefix_Windows_Drive_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_Qualified_Redundant_Prefix_DriveAndRoot_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Root + t.Item1, DevicePrefix + Prefix_Windows_Drive_Root + t.Item5 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DriveRootless_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { Prefix_Windows_Drive_Rootless + t.Item1, Prefix_Windows_Drive_Rootless + t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_Prefix_DriveRootless_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item1, DevicePrefix + Prefix_Windows_Drive_Rootless + t.Item4 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { t.Item1, t.Item3 };
    public static IEnumerable<object[]> MemberData_Unqualified_Redundant_DrivelessRoot_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { Prefix_Windows_Driveless_Root + t.Item1, Prefix_Windows_Driveless_Root + t.Item2 };
    public static IEnumerable<object[]> MemberData_ServerShare_Redundant_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { ServerShare + t.Item1, ServerShare + t.Item2 }; // Qualified but not a device path
    public static IEnumerable<object[]> MemberData_UNC_Redundant_DoubleDot_EdgeCases =>
        from t in TestPaths_Redundant_DoubleDot_EdgeCases
        select new object[] { DevicePrefix + UNCServerShare + t.Item1, DevicePrefix + UNCServerShare + t.Item5 };

    #endregion

    #endregion
}