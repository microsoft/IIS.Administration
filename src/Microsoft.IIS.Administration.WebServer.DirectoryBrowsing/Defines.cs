// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "directory-browsing";

        public const string DirectoryBrowsingName = "Microsoft.WebServer.DirectoryBrowsing";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("directory_browsing", new Guid("985F7E46-E92E-4E27-A06B-4007883160BF"), ENDPOINT);
        public const string IDENTIFIER = "directory_browsing.id";
    }
}
