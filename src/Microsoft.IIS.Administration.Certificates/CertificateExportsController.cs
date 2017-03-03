// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Files;
    using Newtonsoft.Json.Linq;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public class CertificateExportsController : ApiBaseController
    {
        IFileProvider _provider;

        public CertificateExportsController(IFileProvider fileProvider)
        {
            _provider = fileProvider;
        }

        [HttpPost]
        public async Task<object> Post(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string password = DynamicHelper.Value(model.password);
            if (string.IsNullOrEmpty(password)) {
                throw new ApiArgumentException("password");
            }

            string name = DynamicHelper.Value(model.name);
            if (string.IsNullOrEmpty(name) || !PathUtil.IsValidFileName(name)) {
                throw new ApiArgumentException("name");
            }

            bool persistKey = DynamicHelper.To<bool>(model.persist_key) ?? false;

            byte[] content = null;
            string path = GetFile(model);
            using (Cert cert = GetCertificate(model)) {

                if (persistKey && !Interop.IsPrivateKeyExportable(cert.Certificate)) {
                    throw new ApiArgumentException("persist_key");
                }
                
                if (persistKey) {
                    content = cert.Certificate.Export(X509ContentType.Pfx, password);
                    path = Path.Combine(path, name) + ".pfx";
                }
                else {
                    content = cert.Certificate.Export(X509ContentType.Cert, password);
                    path = Path.Combine(path, name) + ".cer";
                }
            }

            if (_provider.FileExists(path)) {
                throw new AlreadyExistsException(path);
            }

            using (Stream ms = new MemoryStream(content))
            using (Stream stream = _provider.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                await ms.CopyToAsync(stream);
            }

            return null;
        }



        private Cert GetCertificate(dynamic model)
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

            Cert cert = CertificateHelper.GetCert(id.Thumbprint, id.StoreName, id.StoreLocation);

            if (cert == null) {
                throw new NotFoundException("certificate");
            }

            return cert;
        }

        private string GetFile(dynamic model)
        {
            if (model.file == null) {
                throw new ApiArgumentException("file");
            }
            if (!(model.file is JObject)) {
                throw new ApiArgumentException("file", ApiArgumentException.EXPECTED_OBJECT);
            }

            string fileUuid = DynamicHelper.Value(model.file.id);
            if (fileUuid == null) {
                throw new ApiArgumentException("file.id");
            }

            FileId id = FileId.FromUuid(fileUuid);

            if (!_provider.FileExists(id.PhysicalPath)) {
                throw new NotFoundException("file");
            }

            return id.PhysicalPath;
        }
    }
}
