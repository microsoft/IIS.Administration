// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Mvc;
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Http;


    public class CertificatesController : ApiBaseController
    {
        [HttpGet]
        public object Get()
        {            
            List<object> refs = new List<object>();

            // Filter for selecting certificates with specific purpose.
            string intended_purpose = Context.Request.Query["intended_purpose"];

            var certs = CertificateHelper.GetCertificates(CertificateHelper.STORE_NAME, CertificateHelper.STORE_LOCATION);

            if (intended_purpose != null) {

                // Filter based on intended purpose, select only the certificates that contain a matching usage
                certs = certs.Where(cert => {

                    return CertificateHelper.GetEnhancedUsages(cert).Any(s => s.Equals(intended_purpose, StringComparison.OrdinalIgnoreCase));
                }).ToList();
            }

            // Build references in the scope of the store because references have dependence on store name and location
            foreach (X509Certificate2 cert in certs) {
                refs.Add(CertificateHelper.ToJsonModelRef(cert, CertificateHelper.STORE_NAME, CertificateHelper.STORE_LOCATION));
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
        public object Get(string id)
        {
            CertificateId certId = new CertificateId(id);

            using (X509Certificate2 cert = CertificateHelper.GetCert(certId.Thumbprint, certId.StoreName, certId.StoreLocation)) {
                if (cert == null) {
                    return NotFound();
                }

                return CertificateHelper.ToJsonModel(cert, certId.StoreName, certId.StoreLocation);
            }
        }
    }
}
