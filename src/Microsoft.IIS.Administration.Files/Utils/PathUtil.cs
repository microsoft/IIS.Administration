// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;

    public static class PathUtil
    {
        public static readonly char[] SEPARATORS = new char[] { '/', '\\' };

        public static int PrefixSegments(string prefix, string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!prefix.StartsWith("/") || !path.StartsWith("/")) {
                throw new ArgumentException("Paths must begin with '/'.");
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
    }
}
