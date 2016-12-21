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

            if (!IsPathRooted(expanded)) {
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

            //
            // Expand to make sure the path is rooted before calling GetFullPath
            var expanded = Environment.ExpandEnvironmentVariables(path);

            if (!IsPathRooted(expanded)) {
                return false;
            }

            bool ret = false;
            try {
                ret = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
                          .Equals(Path.GetFullPath(expanded), StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException) {
                //
                // Argument exception for invalid paths such as '////' (Invalid network share format)
                return false;
            }

            return ret;
        }

        public static bool IsPathRooted(string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (!Path.IsPathRooted(path)) {
                return false;
            }

            // Prevent cases such as 'c:'
            if (path.IndexOfAny(SEPARATORS) == -1) {
                return false;
            }

            return true;
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

        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                        name.IndexOfAny(InvalidFileNameChars) == -1 &&
                        !name.EndsWith(".");
        }
    }
}
