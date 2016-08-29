// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils {
    using System;
    using System.Collections.Generic;
    using System.Linq;


    public sealed class Fields
    {
        private SortedSet<string> _fields;
        private bool _allFields;

        public static readonly Fields All = new Fields("*");

        public bool HasFields { get; private set; }

        public Fields(params string[] fields)
        {
            if (fields == null) {
                _allFields = true;
                return;
            }

            HasFields = true;

            var f = new SortedSet<string>();

            foreach (string s in fields) {
                var str = s.Trim();

                if (str.Equals("*")) {
                    _allFields = true;
                    return;
                }

                f.Add(str);
            }

            // Never leave out id
            f.Add("id");

            _fields = f;
        }

        public bool Exists(string field)
        {
            return _allFields || _fields.Any(f => f.Equals(field, StringComparison.OrdinalIgnoreCase));
        }
    }
}
