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
        private const string _units = "certificates";
        private ICertificateOptions _options;
        private ICertificateStoreProvider _storeProvider;

        public CertificatesController(ICertificateOptions options, ICertificateStoreProvider provider)
        {
            _options = options;
            _storeProvider = provider;
        }

        [HttpHead]
        [ResourceInfo(Name = Defines.CertificatesName)]
        public async Task Head()
        {
            string storeUuid = Context.Request.Query[Defines.StoreIdentifier];
            int count = -1;

            if (storeUuid != null) {
                StoreId id = StoreId.FromUuid(storeUuid);

                ICertificateStore store = _storeProvider.Stores.FirstOrDefault(s => s.Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase));

                if (store == null) {
                    throw new NotFoundException(Defines.StoreIdentifier);
                }

                count = (await store.GetCertificates()).Count();
            }

            if (count == -1) {
                count = await SafeGetCertCount();
            }

            this.Context.Response.SetItemsCount(count);
            this.Context.Response.Headers[HeaderNames.AcceptRanges] = _units;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CertificatesName)]
        public async Task<object> Get()
        {            
            Fields fields = Context.Request.GetFields();
            string storeUuid = Context.Request.Query[Defines.StoreIdentifier];
            IEnumerable<ICertificate> certs = null;

            if (storeUuid != null) {
                StoreId id = StoreId.FromUuid(storeUuid);
                certs = await GetFromStore(id.Name);
            }

            if (certs == null) {
                certs = await SafeGetAllCertificates();
            }

            certs = Filter(certs);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(certs.Count());
            this.Context.Response.Headers[HeaderNames.AcceptRanges] = _units;

            // Invoke json models immediately to prevent errors during serialization
            var models = new List<object>();
            foreach (var cert in certs) {
                models.Add(CertificateHelper.ToJsonModelRef(cert, fields));
            }

            return new {
                certificates = models
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
                cert = await store.GetCertificate(certId.Id);
            }

            if (cert == null) {
                return NotFound();
            }

            return CertificateHelper.ToJsonModel(cert, Context.Request.GetFields());
        }



        private IEnumerable<ICertificate> Filter(IEnumerable<ICertificate> certs)
        {
            // Filter for selecting certificates with specific purpose.
            string intendedPurpose = Context.Request.Query["intended_purpose"];

            if (intendedPurpose != null) {
                certs = certs.Where(cert => cert.PurposesOID.Contains(intendedPurpose) || cert.Purposes.Contains(intendedPurpose, StringComparer.CurrentCultureIgnoreCase));
            }

            return certs;
        }

        private async Task<IEnumerable<ICertificate>> SafeGetAllCertificates()
        {
            IEnumerable<ICertificate> certs = null;

            if (Context.Request.Headers.ContainsKey(HeaderNames.Range)) {
                var certificates = new List<ICertificate>();
                long start, finish;

                foreach (var store in _storeProvider.Stores) {
                    certificates.AddRange(await store.GetCertificates());
                }

                if (!Context.Request.Headers.TryGetRange(out start, out finish, certificates.Count, _units)) {
                    throw new InvalidRangeException();
                }

                Context.Response.Headers.SetContentRange(start, finish, certificates.Count);

                certs = certificates.GetRange((int)start, (int)(finish - start + 1));
            }

            if (certs == null) {
                var certificates = new List<ICertificate>();

                foreach (ICertificateStore store in _storeProvider.Stores) {
                    certificates.AddRange(await SafeGetCertificates(store));
                }

                certs = certificates;
            }

            return certs;
        }

        private async Task<IEnumerable<ICertificate>> GetFromStore(string storeName)
        {
            IEnumerable<ICertificate> certs = null;
            ICertificateStore store = _storeProvider.Stores.FirstOrDefault(s => s.Name.Equals(storeName, StringComparison.OrdinalIgnoreCase));

            if (store == null) {
                throw new NotFoundException(Defines.StoreIdentifier);
            }

            if (Context.Request.Headers.ContainsKey(HeaderNames.Range)) {
                IEnumerable<ICertificate> certificates = await store.GetCertificates();
                long start, finish, max = certificates.Count();

                if (!Context.Request.Headers.TryGetRange(out start, out finish, max, _units)) {
                    throw new InvalidRangeException();
                }

                Context.Response.Headers.SetContentRange(start, finish, max);

                certs = certificates.Where((cert, index) => {
                    return index > start && index <= finish;
                });
            }

            if (certs == null) {
                certs = await store.GetCertificates();
            }

            return certs;
        }

        private async Task<int> SafeGetCertCount()
        {
            int count = 0;

            foreach (ICertificateStore store in _storeProvider.Stores) {
                count += (await SafeAccessStore(async () => (await store.GetCertificates()).Count()));
            }

            return count;
        }

        private async Task<IEnumerable<ICertificate>> SafeGetCertificates(ICertificateStore store)
        {
            return (await SafeAccessStore(async () => await store.GetCertificates())) ?? Enumerable.Empty<ICertificate>();
        }

        private async Task<T> SafeAccessStore<T>(Func<Task<T>> func)
        {
            try {
                return await func();
            }
            catch (ForbiddenArgumentException) {
            }
            catch (UnauthorizedArgumentException) {
            }

            return default(T);
        }
    }
}
