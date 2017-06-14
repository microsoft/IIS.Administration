// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Logging
{
    using Microsoft.IIS.Administration.Core;
    using System.Collections.Generic;
    using System.Linq;

    class NonsensitiveAuditingFields : INonsensitiveAuditingFields
    {
        private List<string> _approvedFields = new List<string>();

        public void Add(string field)
        {
            if (!this._approvedFields.Any(f => f.Equals(field))) {
                _approvedFields.Add(field);
            }
        }

        public IEnumerable<string> Value {
            get {
                return _approvedFields;
            }
        }
    }
}
