// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    public interface IDownloadService
    {
        IDownload Create(string physicalPath, int? timeout = null);

        void Remove(string id);

        IDownload Get(string id);
    }
}
