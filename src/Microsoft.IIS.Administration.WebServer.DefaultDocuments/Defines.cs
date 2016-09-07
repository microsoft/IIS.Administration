// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "default-documents";
        private const string FILES_ENDPOINT = "files";

        // Top level resource for plugin
        public const string DefaultDocumentsName = "Microsoft.WebServer.DefaultDocuments";
        public static readonly ResDef Resource = new ResDef("default_document", new Guid("3EFCD639-13A9-40F9-87BA-E7F0C85C2855"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "default_document.id";

        // File
        public const string EntriesName = "Microsoft.WebServer.DefaultDocuments.Files";
        public const string EntryName = "Microsoft.WebServer.DefaultDocuments.File";
        public static readonly ResDef FilesResource = new ResDef("files", new Guid("1C770C5E-8D48-4E87-9108-75B7A41F6D94"), FILES_ENDPOINT);
        public static readonly string FILES_PATH = $"{PATH}/{FILES_ENDPOINT}";
        public const string FILE_IDENTIFIER = "file.id";
    }
}
