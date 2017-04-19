// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using System;
    using System.Linq;

    public class CertificateStoresController : ApiBaseController
    {
        private ICertificateStoreProvider _provider;

        public CertificateStoresController(ICertificateStoreProvider provider)
        {
            _provider = provider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.StoresName)]
        public object Get()
        {
            Context.Response.SetItemsCount(_provider.Stores.Count());

            return new {
                stores = _provider.Stores.Select(s => CertificateHelper.StoreToJsonModelRef(s, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.StoresName)]
        public object Get(string id)
        {
            StoreId storeId = StoreId.FromUuid(id);

            ICertificateStore store = _provider.Stores.FirstOrDefault(s => s.Name.Equals(storeId.Name, StringComparison.OrdinalIgnoreCase));

            if (store == null) {
                return NotFound();
            }

            return CertificateHelper.StoreToJsonModel(store, Context.Request.GetFields());
        }
    }
}
