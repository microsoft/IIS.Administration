// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;

    public class Defines
    {
        private const string FILES_ENDPOINT = "files";
        private const string CONTENT_ENDPOINT = "content";
        private const string DOWNLOADS_ENDPOINT = "downloads";
        private const string COPY_ENDPOINT = "copy";

        public const string FilesName = "Microsoft.IIS.Administration.Files";
        public const string FileName = "Microsoft.IIS.Administration.File";
        public static readonly string FILES_PATH = $"{Globals.API_PATH}/{FILES_ENDPOINT}";
        public static readonly ResDef FilesResource = new ResDef("files", new Guid("FCA009EA-34A9-4C88-A270-1F65AFF885DA"), FILES_ENDPOINT);
        public const string PARENT_IDENTIFIER = "parent.id";

        public static readonly ResDef DirectoriesResource = new ResDef("directories", new Guid("0EC91657-563C-42B1-8080-CBCAEB963314"), FILES_ENDPOINT);

        public static readonly string CONTENT_PATH = $"{FILES_PATH}/{CONTENT_ENDPOINT}";
        public static readonly ResDef ContentResource = new ResDef("content", new Guid("84EB3443-E4F5-452C-A9C1-98682868927A"), CONTENT_ENDPOINT);

        public static readonly string API_DOWNLOAD_PATH = $"{FILES_PATH}/{DOWNLOADS_ENDPOINT}";
        public static readonly ResDef ApiDownloadResource = new ResDef("downloads", new Guid("A5F6DE75-3C3E-4BEA-9B16-B6692011BB46"), DOWNLOADS_ENDPOINT);

        public static readonly string COPY_PATH = $"{FILES_PATH}/{COPY_ENDPOINT}";
        public static readonly ResDef CopyResource = new ResDef("copy", new Guid("D86F5D18-EA37-419A-879E-B4BC2A6E11C4"), COPY_ENDPOINT);

        public static readonly string DOWNLOAD_PATH = $"{DOWNLOADS_ENDPOINT}";
        public static readonly ResDef DownloadResource = new ResDef("downloads", new Guid("9DAF09F0-197B-4164-81D5-B6A25154883A"), DOWNLOADS_ENDPOINT);
    }
}
