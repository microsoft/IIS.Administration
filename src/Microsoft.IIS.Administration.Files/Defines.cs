// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;

    public class Defines
    {
        private const string DOWNLOADS_ENDPOINT = "downloads";

        public static readonly string DOWNLOAD_PATH = $"{DOWNLOADS_ENDPOINT}";
        public static readonly ResDef DownloadResource = new ResDef("download", new Guid("{9DAF09F0-197B-4164-81D5-B6A25154883A}"), DOWNLOADS_ENDPOINT);
        public const string DOWNLOAD_IDENTIFIER = "dl.id";
    }
}
