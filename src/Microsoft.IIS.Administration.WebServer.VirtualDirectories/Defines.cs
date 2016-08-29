// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.VirtualDirectories
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "virtual-directories";

        public const string VirtualDirectoriesName = "Microsoft.WebServer.VirtualDirectories";
        public const string VirtualDirectoryName = "Microsoft.WebServer.VirtualDirectory";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("vdirs", new Guid("A35A1026-1DE2-4783-9E51-FE21B0EC6533"), ENDPOINT);
        public const string IDENTIFIER = "vdir.id";
    }
}
