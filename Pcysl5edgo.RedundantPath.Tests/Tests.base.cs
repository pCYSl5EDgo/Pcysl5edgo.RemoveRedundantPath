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

public class RedundantSegmentsTestsBase
{
    #region Helpers
    protected void TestWindows(string original, string expected)
    {
        TestWindowsEach(original, expected);
        var actual = ReversePath.RemoveRedundantSegmentsWindows(original, false);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(original, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsWindows(actual, false)));
    }

    private static void TestWindowsEach(string original, string expected)
    {
        var actual = ReversePath.RemoveRedundantSegmentsWindows(original, true);
        if (ReferenceEquals(original, expected))
        {
            Assert.True(ReferenceEquals(original, actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }

        Assert.True(ReferenceEquals(actual, ReversePath.RemoveRedundantSegmentsWindows(actual, true)));
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
