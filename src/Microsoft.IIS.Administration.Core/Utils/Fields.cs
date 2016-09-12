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
        public static readonly Fields Empty = new Fields(string.Empty);

        public bool HasFields { get; private set; }

        public Fields(params string[] fields)
        {
            if (fields == null) {
                _allFields = true;
                return;
            }

            this._fields = new SortedSet<string>();

            // Never leave out id
            this._fields.Add("id");

            foreach (string s in fields) {
                Add(s);
            }

        }

        private Fields() {
            this._fields = new SortedSet<string>();
            this._fields.Add("id");
        }

        public bool Exists(string field)
        {
            return _allFields || _fields.Any(f => f.Equals(field, StringComparison.OrdinalIgnoreCase) || f.StartsWith($"{field}.", StringComparison.OrdinalIgnoreCase));
        }

        public Fields Filter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) {
                throw new ArgumentNullException(nameof(filter));
            }

            if (this._fields == null) {
                return Empty;
            }

            filter = filter + ".";

            var newFields = new Fields();

            foreach (var field in this._fields) {
                if (field.StartsWith(filter, StringComparison.OrdinalIgnoreCase)) {
                    newFields.Add(field.Substring(filter.Length));
                }
            }

            return newFields;
        }

        private void Add(string field)
        {
            field = field.Trim();

            if (field == string.Empty) {
                return;
            }

            HasFields = true;

            if (field == "*") {
                _allFields = true;
            }

            _fields.Add(field);
        }
    }
}
