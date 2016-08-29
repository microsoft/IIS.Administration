// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    public class UrlRule
    {
        public string Url { get; set; }
        public bool Allow { get; set; }
    }
}
