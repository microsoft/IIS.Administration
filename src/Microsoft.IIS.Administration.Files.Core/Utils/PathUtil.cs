// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Utils;
    using System;
    using System.IO;
    using System.Security.Cryptography;

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
                throw new ArgumentException("Path must be rooted", nameof(path));
            }

            try {
                return Path.GetFullPath(expanded);
            }
            catch (NotSupportedException e) {
                // Path contains a colon (":") that is not part of a volume identifier (for example, "c:\").
                throw new ArgumentException(string.Empty, e);
            }
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

        /// <summary>
        /// Given a file path, returns a path in the same directory with a temporary name.
        /// </summary>
        public static string GetTempFilePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !IsFullPath(path)) {
                throw new ArgumentException(nameof(path));
            }

            string tempPath = null;
            var info = new System.IO.DirectoryInfo(path);

            if (info.Parent == null) {
                throw new ArgumentException("path", "Parent cannot be null.");
            }

            do {
                tempPath = Path.Combine(info.Parent.FullName, GetTempName(info.Name));
            }
            while (File.Exists(tempPath) || Directory.Exists(tempPath));

            return tempPath;
        }

        public static bool IsAncestor(string parent, string child)
        {
            var parentDir = new System.IO.DirectoryInfo(parent);
            var curParent = Directory.GetParent(child);

            while (curParent != null) {
                if (curParent.FullName.Equals(parentDir.FullName, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
                curParent = curParent.Parent;
            }

            return false;
        }

        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                        name.IndexOfAny(InvalidFileNameChars) == -1 &&
                        !name.EndsWith(".");
        }

        public static string GetName(string path)
        {
            return new System.IO.DirectoryInfo(path).Name;
        }

        public static string GetParentPath(string path)
        {
            var parent = new System.IO.DirectoryInfo(path).Parent;
            return parent == null ? null : parent.FullName;
        }

        private static string GetTempName(string name)
        {
            var bytes = new byte[4];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
                return Base64.Encode(bytes) + name;
            }
        }
    }
}
