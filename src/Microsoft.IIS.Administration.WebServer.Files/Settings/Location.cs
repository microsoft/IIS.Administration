// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using System.Collections.Generic;

    class Location : ILocation
    {
        public string Alias { get; set; }
        public string Path { get; set; }
        public IEnumerable<string> Claims { get; set; }
    }
}
