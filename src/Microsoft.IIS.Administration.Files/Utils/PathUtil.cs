// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.IO;

    public static class PathUtil
    {
        public static readonly char[] SEPARATORS = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        public static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Expands environment variables and normalizes the path. The path must be rooted.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the path is not rooted.</exception>
        /// <returns>Fully expanded normalized path.</returns>
        public static string GetFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            var expanded = Environment.ExpandEnvironmentVariables(path);

            if (!Path.IsPathRooted(expanded)) {
                throw new ArgumentException("Path must be rooted.", nameof(path));
            }

            return Path.GetFullPath(expanded);
        }

        /// <summary>
        /// Tests whether a path is rooted, normalized, and expanded.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        public static bool IsFullPath(string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            var expanded = Environment.ExpandEnvironmentVariables(path);

            if (!Path.IsPathRooted(expanded)) {
                return false;
            }

            return path.Equals(Path.GetFullPath(expanded), StringComparison.OrdinalIgnoreCase);
        }

        public static int PrefixSegments(string prefix, string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Path.IsPathRooted(prefix) || !Path.IsPathRooted(path)) {
                throw new ArgumentException("Paths must be rooted.");
            }

            var prefixParts = prefix.TrimEnd(SEPARATORS).Split(SEPARATORS);
            var pathParts = path.TrimEnd(SEPARATORS).Split(SEPARATORS);

            if (prefixParts.Length > pathParts.Length) {
                return -1;
            }
            
            int index = 0;
            while (pathParts.Length > index && prefixParts.Length > index && prefixParts[index].Equals(pathParts[index], stringComparison)) {
                index++;
            }
            
            if (prefixParts.Length > index) {
                return -1;
            }

            return index == 0 ? -1 : index;
        }

        public static bool IsParentPath(string parent, string child)
        {
            var prefixSegments = PathUtil.PrefixSegments(parent, child);
            if (prefixSegments > 0 && NumberOfSegments(child) > NumberOfSegments(parent)) {
                return true;
            }
            return false;
        }

        public static int NumberOfSegments(string path)
        {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!path.StartsWith("/")) {
                throw new ArgumentException("Path must begin with '/'.");
            }

            return path.TrimEnd(SEPARATORS).Split(SEPARATORS).Length;
        }

        public static string TrimStart(this string val, string prefix, StringComparison stringComparision = StringComparison.Ordinal)
        {
            if (val.StartsWith(prefix, stringComparision)) {
                val = val.Remove(0, prefix.Length);
            }
            return val;
        }

        public static string RemoveLastSegment(string path)
        {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!path.StartsWith("/") || path == "/") {
                throw new ArgumentException(nameof(path));
            }

            var parts = path.TrimEnd(SEPARATORS).Split(SEPARATORS);
            parts[parts.Length - 1] = string.Empty;
            var ret = string.Join("/", parts);
            return ret == "/" ? ret : ret.TrimEnd('/');
        }

        public static bool PathStartsWith(string path, string prefix)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }
            if (string.IsNullOrEmpty(prefix)) {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (prefix.Length > path.Length) {
                return false;
            }

            var separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            var testParts = path.Split(separators);
            var prefixParts = prefix.TrimEnd(separators).Split(separators);

            if (prefixParts.Length > testParts.Length) {
                return false;
            }

            for (var i = 0; i < prefixParts.Length; i++) {
                if (!prefixParts[i].Equals(testParts[i], StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                        name.IndexOfAny(InvalidFileNameChars) == -1 &&
                        !name.EndsWith(".");
        }
    }
}
