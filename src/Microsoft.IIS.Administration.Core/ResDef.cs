// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System;

    public class ResDef
    {
        public string Name;
        public Guid Guid;
        public string Endpoint; 

        public ResDef(string resourceName, Guid resourceGuid, string endpoint)
        {
            this.Name = resourceName;
            this.Guid = resourceGuid;
            this.Endpoint = endpoint;
        }

        //Check if two ResDefs have any common fields.
        public bool Overlaps(ResDef r)
        {
            return this.Guid.Equals(r.Guid)
            || String.Equals(this.Name, r.Name, StringComparison.OrdinalIgnoreCase)
            || String.Equals(this.Endpoint, r.Endpoint, StringComparison.OrdinalIgnoreCase);
        }
    }
}
