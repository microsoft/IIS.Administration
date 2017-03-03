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
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public class CertificateImportsController : ApiBaseController
    {
        private const int InvalidFile = unchecked((int)0x80004005);
        private const int InvalidPassword = unchecked((int)0x80070056);
        private IFileProvider _provider;

        public CertificateImportsController(IFileProvider fileProvider)
        {
            _provider = fileProvider;
        }

        [HttpPost]
        public object Post(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string path = GetFile(model);

            string password = DynamicHelper.Value(model.password);
            if (string.IsNullOrEmpty(password)) {
                throw new ApiArgumentException("password");
            }

            bool persistKey = DynamicHelper.To<bool>(model.persist_key) ?? true;
            bool exportable = DynamicHelper.To<bool>(model.exportable) ?? false;

            X509Certificate2 cert = null;
            try {
                cert = new X509Certificate2(path, password, X509KeyStorageFlags.MachineKeySet
                                                            | (exportable ? X509KeyStorageFlags.Exportable : 0)
                                                            | (persistKey ? X509KeyStorageFlags.PersistKeySet : 0));
            }
            catch (CryptographicException e) {
                if (e.HResult == InvalidFile) {
                    throw new ApiArgumentException("file", "Invalid certificate file", e);
                }
                if (e.HResult == InvalidPassword) {
                    throw new ApiArgumentException("password", "Password is incorrect", e);
                }
            }

            using (cert)
            using (var store = CertificateHelper.GetStore("My", StoreLocation.LocalMachine, FileAccess.ReadWrite)) {
                if (store == null) {
                    throw new ApiArgumentException("store");
                }
                store.Add(cert);
            }

            return null;
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
