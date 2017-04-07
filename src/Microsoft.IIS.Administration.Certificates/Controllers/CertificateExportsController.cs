// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Files;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class CertificateExportsController : ApiBaseController
    {
        private IFileProvider _fileProvider;
        private ICertificateStoreProvider _storeProvider;

        public CertificateExportsController(IFileProvider fileProvider, ICertificateStoreProvider storeProvider)
        {
            _fileProvider = fileProvider;
            _storeProvider = storeProvider;
        }

        [HttpPost]
        public async Task<object> Post(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            string password = DynamicHelper.Value(model.password);
            bool persistKey = DynamicHelper.To<bool>(model.persist_key) ?? false;

            if (string.IsNullOrEmpty(name) || !PathUtil.IsValidFileName(name)) {
                throw new ApiArgumentException("name");
            }

            IFileInfo file = GetFile(model, name + (persistKey ? ".pfx" : ".cer"));

            ICertificate cert = await GetCertificate(model);

            if (persistKey && !cert.IsPrivateKeyExportable) {
                throw new ApiArgumentException("persist_key");
            }

            if (persistKey && string.IsNullOrEmpty(password)) {
                throw new ApiArgumentException("password");
            }

            using (Stream content = cert.Store.GetContent(cert, persistKey, persistKey ? password : null))
            using (Stream fsStream = _fileProvider.GetFileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read)) {

                if (content == null) {
                    return NotFound();
                }

                await content.CopyToAsync(fsStream);
            }

            return null;
        }



        private async Task<ICertificate> GetCertificate(dynamic model)
        {
            if (model.certificate == null) {
                throw new ApiArgumentException("certificate");
            }
            if (!(model.certificate is JObject)) {
                throw new ApiArgumentException("certificate", ApiArgumentException.EXPECTED_OBJECT);
            }

            string certificateUuid = DynamicHelper.Value(model.certificate.id);
            if (certificateUuid == null) {
                throw new ApiArgumentException("certificate.id");
            }

            CertificateId id = new CertificateId(certificateUuid);

            ICertificate cert = null;
            ICertificateStore store = _storeProvider.Stores.FirstOrDefault(s => s.Name.Equals(id.StoreName, StringComparison.OrdinalIgnoreCase));

            if (store != null) {
                cert = await store.GetCertificate(id.Thumbprint);
            }

            if (cert == null) {
                throw new NotFoundException("certificate");
            }

            return cert;
        }

        private IFileInfo GetFile(dynamic model, string name)
        {
            if (model.parent == null) {
                throw new ApiArgumentException("parent");
            }
            if (!(model.parent is JObject)) {
                throw new ApiArgumentException("parent", ApiArgumentException.EXPECTED_OBJECT);
            }

            string fileUuid = DynamicHelper.Value(model.parent.id);
            if (fileUuid == null) {
                throw new ApiArgumentException("parent.id");
            }

            IFileInfo parent = _fileProvider.GetDirectory(FileId.FromUuid(fileUuid).PhysicalPath);

            if (!parent.Exists) {
                throw new NotFoundException("parent");
            }

            return _fileProvider.GetFile(Path.Combine(parent.Path, name));
        }
    }
}
