// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System.IO;

    public interface IAccessControl
    {
        FileAccess GetFileAccess(string path);
    }
}
