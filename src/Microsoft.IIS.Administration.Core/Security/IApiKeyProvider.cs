// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Security {
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public interface IApiKeyProvider {
        ApiToken GenerateKey(string purpose);

        Task<string> RenewToken(ApiKey key);

        ApiKey FindKey(string token);

        Task<IEnumerable<ApiKey>> GetAllKeys();

        ApiKey GetKey(string id);

        Task SaveKey(ApiKey key);

        Task DeleteKey(ApiKey key);
    }
}
