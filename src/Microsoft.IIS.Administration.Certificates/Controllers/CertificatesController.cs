// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Http;
    using Core.Utils;
    using Core;
    using System.Threading.Tasks;

    public class CertificatesController : ApiBaseController
    {
        private ICertificateOptions _options;
        private ICertificateStoreProvider _storeProvider;

        public CertificatesController(ICertificateOptions options, ICertificateStoreProvider provider)
        {
            _options = options;
            _storeProvider = provider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CertificatesName)]
        public async Task<object> Get()
        {            
            Fields fields = Context.Request.GetFields();
            var certs = new List<ICertificate>();

            foreach (ICertificateStore store in _storeProvider.Stores) {
                certs.AddRange(await store.GetCertificates());
            }

            certs = Filter(certs);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(certs.Count());

            return new {
                certificates = certs.Select(cert => CertificateHelper.ToJsonModelRef(cert, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CertificateName)]
        public async Task<object> Get(string id)
        {
            CertificateId certId = new CertificateId(id);
            ICertificate cert = null;
            ICertificateStore store = _storeProvider.Stores.FirstOrDefault(s => s.Name.Equals(certId.StoreName, StringComparison.OrdinalIgnoreCase));

            if (store != null) {
                cert = await store.GetCertificate(certId.Thumbprint);
            }

            if (cert == null) {
                return NotFound();
            }

            return CertificateHelper.ToJsonModel(cert, Context.Request.GetFields());
        }



        private List<ICertificate> Filter(List<ICertificate> certs)
        {
            // Filter for selecting certificates with specific purpose.
            string intended_purpose = Context.Request.Query["intended_purpose"];
            string storeUuid = Context.Request.Query[Defines.StoreIdentifier];

            if (intended_purpose != null) {
                certs.RemoveAll((cert) => {
                    return !cert.Purposes.Any(s => s.Equals(intended_purpose, StringComparison.OrdinalIgnoreCase));
                });
            }

            if (storeUuid != null) {
                StoreId id = StoreId.FromUuid(storeUuid);

                certs.RemoveAll((cert) => {
                    return !cert.Store.Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase);
                });
            }

            return certs;
        }
    }
}
