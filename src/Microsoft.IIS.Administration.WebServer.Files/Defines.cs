// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Core;
    using System;

    public class Defines
    {
        //private const string DIRECTORIES_ENDPOINT = "directories";
        private const string FILES_ENDPOINT = "files";
        private const string CONTENT_ENDPOINT = "content";

        public static readonly string FILES_PATH = $"{WebServer.Defines.PATH}/{FILES_ENDPOINT}";
        public static readonly ResDef FilesResource = new ResDef("files", new Guid("CF0CF1C6-8913-4EF0-9833-C11820689252"), FILES_ENDPOINT);
        public static readonly string FILE_IDENTIFIER = "file.id";
        public static readonly string PARENT_IDENTIFIER = "parent.id";

        public static readonly string CONTENT_PATH = $"{FILES_PATH}/{CONTENT_ENDPOINT}";
        public static readonly ResDef ContentResource = new ResDef("content", new Guid("345F44E5-F4B5-4289-A551-6353D3A61215"), CONTENT_ENDPOINT);
        public const string CONTENT_IDENTIFIER = "content.id";
    }
}
