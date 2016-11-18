// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System.Collections.Generic;

    public class FileOptions
    {
        private bool _searchedForAll;
        private AllowedRoot _allPaths;

        public List<AllowedRoot> Allowed_Roots { get; set; }

        public AllowedRoot WildCardRoot {
            get {
                if (_searchedForAll) {
                    return _allPaths;
                }

                foreach (var allowedRoot in Allowed_Roots) {
                    if (allowedRoot.Path.Equals("*")) {
                        _allPaths = allowedRoot;
                        _searchedForAll = true;
                    }
                }

                return _allPaths;
            }
        }

        public static FileOptions EmptyOptions()
        {
            return new FileOptions() {
                Allowed_Roots = new List<AllowedRoot>()
            };
        }
    }
}
