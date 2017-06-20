// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.IIS.Administration.Core.Security;
    using System.Collections.Generic;
    using System.Threading.Tasks;


    interface IApiKeyStorage {
        Task<IEnumerable<ApiKey>> GetAllKeys();
        Task<ApiKey> GetKeyByHash(string hash);
        Task<ApiKey> GetKeyById(string id);
        Task SaveKey(ApiKey key);
        Task<bool> RemoveKey(ApiKey key);
    }
}
