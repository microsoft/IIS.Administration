// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Core;
    using System;

    public class Defines
    {
        private const string FILES_ENDPOINT = "files";

        public const string FilesName = "Microsoft.WebServer.Files";
        public const string FileName = "Microsoft.WebServer.File";
        public static readonly string FILES_PATH = $"{WebServer.Defines.PATH}/{FILES_ENDPOINT}";
        public static readonly ResDef FilesResource = new ResDef("files", new Guid("CF0CF1C6-8913-4EF0-9833-C11820689252"), FILES_ENDPOINT);
        public static readonly string PARENT_IDENTIFIER = "parent.id";
        
        public static readonly ResDef DirectoriesResource = new ResDef("directories", new Guid("E83B01EA-37A3-405F-8B5B-83D861DC9FAA"), FILES_ENDPOINT);
    }
}
