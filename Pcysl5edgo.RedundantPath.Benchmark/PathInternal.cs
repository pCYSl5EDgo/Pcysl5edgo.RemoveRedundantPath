using System;
using System.Diagnostics.CodeAnalysis;

namespace Pcysl5edgo.RedundantPath.Benchmark;

/// <summary>Contains public path helpers that are shared between many projects.</summary>
public static partial class PathInternal
{
    /// <summary>
    /// Returns true if the path starts in a directory separator.
    /// </summary>
    public static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path) => path.Length > 0 && IsDirectorySeparator(path[0]);

#if MS_IO_REDIST
        public static string EnsureTrailingSeparator(string path)
            => EndsInDirectorySeparator(path) ? path : path + DirectorySeparatorCharAsString;
#else
    public static string EnsureTrailingSeparator(string path)
        => EndsInDirectorySeparator(path.AsSpan()) ? path : path + DirectorySeparatorCharAsString;
#endif

    public static bool IsRoot(ReadOnlySpan<char> path)
        => path.Length == GetRootLength(path);

    /// <summary>
    /// Get the common path length from the start of the string.
    /// </summary>
    public static int GetCommonPathLength(string first, string second, bool ignoreCase)
    {
        int commonChars = EqualStartingCharacterCount(first, second, ignoreCase: ignoreCase);

        // If nothing matches
        if (commonChars == 0)
            return commonChars;

        // Or we're a full string and equal length or match to a separator
        if (commonChars == first.Length
            && (commonChars == second.Length || IsDirectorySeparator(second[commonChars])))
            return commonChars;

        if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
            return commonChars;

        // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
        while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
            commonChars--;

        return commonChars;
    }

    /// <summary>
    /// Gets the count of common characters from the left optionally ignoring case
    /// </summary>
    public static unsafe int EqualStartingCharacterCount(string? first, string? second, bool ignoreCase)
    {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

        int commonChars = 0;

        fixed (char* f = first)
        fixed (char* s = second)
        {
            char* l = f;
            char* r = s;
            char* leftEnd = l + first.Length;
            char* rightEnd = r + second.Length;

            while (l != leftEnd && r != rightEnd
                && (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r))))
            {
                commonChars++;
                l++;
                r++;
            }
        }

        return commonChars;
    }

    /// <summary>
    /// Returns true if the two paths have the same root
    /// </summary>
    public static bool AreRootsEqual(string? first, string? second, StringComparison comparisonType)
    {
        int firstRootLength = GetRootLength(first.AsSpan());
        int secondRootLength = GetRootLength(second.AsSpan());

        return firstRootLength == secondRootLength
            && string.Compare(
                strA: first,
                indexA: 0,
                strB: second,
                indexB: 0,
                length: firstRootLength,
                comparisonType: comparisonType) == 0;
    }

    /// <summary>
    /// Trims one trailing directory separator beyond the root of the path.
    /// </summary>
    [return: NotNullIfNotNull("path")]
    public static string? TrimEndingDirectorySeparator(string? path) =>
        EndsInDirectorySeparator(path) && !IsRoot(path.AsSpan()) ?
            path!.Substring(0, path.Length - 1) :
            path;

    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    public static bool EndsInDirectorySeparator(string? path) =>
          !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);

    /// <summary>
    /// Trims one trailing directory separator beyond the root of the path.
    /// </summary>
    public static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) =>
        EndsInDirectorySeparator(path) && !IsRoot(path) ?
            path.Slice(0, path.Length - 1) :
            path;

    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    public static bool EndsInDirectorySeparator(ReadOnlySpan<char> path) =>
        path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
}