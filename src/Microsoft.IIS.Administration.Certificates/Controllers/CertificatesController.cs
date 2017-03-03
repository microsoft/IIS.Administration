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

    public class CertificatesController : ApiBaseController
    {
        private ICertificateOptions _options;

        public CertificatesController(ICertificateOptions options)
        {
            _options = options;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CertificatesName)]
        public object Get()
        {            
            List<object> refs = new List<object>();
            Fields fields = Context.Request.GetFields();

            // Filter for selecting certificates with specific purpose.
            string intended_purpose = Context.Request.Query["intended_purpose"];
            var certs = new List<Cert>();

            foreach (IStore store in _options.Stores) {
                if (string.IsNullOrEmpty(store.Path)) {
                    certs.AddRange(CertificateHelper.GetCertificates(store.Name, store.StoreLocation));
                }
            }

            if (intended_purpose != null) {
                certs = certs.Where(cert => {
                    return CertificateHelper.GetEnhancedUsages(cert.Certificate).Any(s => s.Equals(intended_purpose, StringComparison.OrdinalIgnoreCase));
                }).ToList();
            }

            // Build references in the scope of the store because references have dependence on store name and location
            foreach (Cert cert in certs) {
                refs.Add(CertificateHelper.ToJsonModelRef(cert, fields));
                cert.Dispose();
            }

            // All certs disposed.
            certs = null;

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(refs.Count());

            return new {
                certificates = refs
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CertificateName)]
        public object Get(string id)
        {
            CertificateId certId = new CertificateId(id);

            using (Cert cert = CertificateHelper.GetCert(certId.Thumbprint, certId.StoreName, certId.StoreLocation)) {
                if (cert == null) {
                    return NotFound();
                }

                return CertificateHelper.ToJsonModel(cert, Context.Request.GetFields());
            }
        }
    }
}
