// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    public interface IDownload
    {
        string Id { get; }

        string PhysicalPath { get; set; }

        string Href { get; }
    }
}
