using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Pcysl5edgo.RemoveRedundantPath;

/// <summary>Contains public path helpers that are shared between many projects.</summary>
public static partial class PathInternal
{
    public const char DirectorySeparatorChar = '/';
    public const char AltDirectorySeparatorChar = '/';
    public const char VolumeSeparatorChar = '/';
    public const char PathSeparator = ':';
    public const string DirectorySeparatorCharAsString = "/";
    public const string ParentDirectoryPrefix = @"../";

    public static int GetRootLength(ReadOnlySpan<char> path)
    {
        return path.Length > 0 && IsDirectorySeparator(path[0]) ? 1 : 0;
    }

    public static bool IsDirectorySeparator(char c)
    {
        // The alternate directory separator char is the same as the directory separator,
        // so we only need to check one.
        Debug.Assert(DirectorySeparatorChar == AltDirectorySeparatorChar);
        return c == DirectorySeparatorChar;
    }

    /// <summary>
    /// Normalize separators in the given path. Compresses forward slash runs.
    /// </summary>
    [return: NotNullIfNotNull("path")]
    public static string? NormalizeDirectorySeparators(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Make a pass to see if we need to normalize so we can potentially skip allocating
        bool normalized = true;

        for (int i = 0; i < path.Length; i++)
        {
            if (IsDirectorySeparator(path[i])
                && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
            {
                normalized = false;
                break;
            }
        }

        if (normalized)
            return path;

        StringBuilder builder = new StringBuilder(path.Length);

        for (int i = 0; i < path.Length; i++)
        {
            char current = path[i];

            // Skip if we have another separator following
            if (IsDirectorySeparator(current)
                && (i + 1 < path.Length && IsDirectorySeparator(path[i + 1])))
                continue;

            builder.Append(current);
        }

        return builder.ToString();
    }

    public static bool IsPartiallyQualified(ReadOnlySpan<char> path)
    {
        // This is much simpler than Windows where paths can be rooted, but not fully qualified (such as Drive Relative)
        // As long as the path is rooted in Unix it doesn't use the current directory and therefore is fully qualified.
        return !Path.IsPathRooted(path);
    }

    /// <summary>
    /// Returns true if the path is effectively empty for the current OS.
    /// For unix, this is empty or null. For Windows, this is empty, null, or
    /// just spaces ((char)32).
    /// </summary>
    public static bool IsEffectivelyEmpty(string? path)
    {
        return string.IsNullOrEmpty(path);
    }

    public static bool IsEffectivelyEmpty(ReadOnlySpan<char> path)
    {
        return path.IsEmpty;
    }
}