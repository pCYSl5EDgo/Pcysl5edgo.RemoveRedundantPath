using System;
using System.Collections.Generic;
using System.IO;

namespace Pcysl5edgo.RedudantPath.Tests;

public class RedundantSegmentsTestsBase
{
    #region Helpers

    protected void TestString(string original, string expected)
    {
        //string actual = Path.RemoveRedundantSegments(original);
        //Assert.Equal(expected, actual);
    }

    protected void TestSpan(string original, string expected)
    {
        //string actual = Path.RemoveRedundantSegments(original.AsSpan());
        //Assert.Equal(expected, actual);
    }

    protected void TestTry(string original, string expected)
    {
        Span<char> actual = stackalloc char[expected.Length];
        
        //Assert.True(Path.TryRemoveRedundantSegments(original.AsSpan(), actual, out int charsWritten));
        //Assert.Equal(expected, actual.Slice(0, charsWritten).ToString());
    }

    protected void TestAll(string original, string expected)
    {
        TestString(original, expected);
        TestSpan(original, expected);
        TestTry(original, expected);
    }

    #endregion
}

internal static class RedundantSegmentTestsExtensions
{
    internal static void Add(this List<Tuple<string, string, string>> list, string original, string qualified, string unqualified) =>
        list.Add(new Tuple<string, string, string>(original, qualified, unqualified));
    internal static void Add(this List<Tuple<string, string, string, string>> list, string original, string qualified, string unqualified, string devicePrefix) =>
        list.Add(new Tuple<string, string, string, string>(original, qualified, unqualified, devicePrefix));
    internal static void Add(this List<Tuple<string, string, string, string, string>> list, string original, string qualified, string unqualified, string devicePrefixUnrooted, string devicePrefixRooted) =>
        list.Add(new Tuple<string, string, string, string, string>(original, qualified, unqualified, devicePrefixUnrooted, devicePrefixRooted));
}
