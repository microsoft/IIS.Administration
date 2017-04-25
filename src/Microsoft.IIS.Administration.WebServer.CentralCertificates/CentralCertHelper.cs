// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using Certificates;
    using Core;
    using Core.Utils;
    using Files;
    using Newtonsoft.Json.Linq;
    using System.Dynamic;
    using System.IO;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Win32;
    using Win32.SafeHandles;

    class CentralCertHelper
    {
        private const string FEATURE_NAME = "IIS-CertProvider";

        public static bool FeatureEnabled {
            get {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\INETSTP\Components", false)) {

                    if (key != null) {
                        int featureInstalled = (int)key.GetValue("CertProvider", -1);

                        return (featureInstalled == 1);
                    }

                    return false;
                }
            }
        }

        public static object ToJsonModel()
        {
            var ccs = Startup.CentralCertificateStore;

            dynamic obj = new ExpandoObject();
            obj.id = new CentralCertConfigId().Uuid;
            obj.path = ccs.PhysicalPath;
            obj.identity = new {
                username = ccs.UserName
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static void Update(dynamic model, IFileProvider fileProvider)
        {
            string username, password, path, privateKeyPassword;
            ExtractModel(model, out username, out password, out path, out privateKeyPassword);
            string physicalPath = null;

            var ccs = Startup.CentralCertificateStore;

            // Validate path allowed
            if (!string.IsNullOrEmpty(path)) {
                path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var expanded = System.Environment.ExpandEnvironmentVariables(path);

                if (!PathUtil.IsFullPath(expanded)) {
                    throw new ApiArgumentException("physical_path");
                }
                if (!fileProvider.IsAccessAllowed(expanded, FileAccess.Read)) {
                    throw new ForbiddenArgumentException("physical_path", path);
                }
                if (!fileProvider.GetDirectory(expanded).Exists) {
                    throw new NotFoundException("physical_path");
                }
                physicalPath = expanded;
            }

            // Validate credentials
            if ((!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password)) && !TestConnection(path ?? ccs.PhysicalPath, username ?? ccs.UserName, password ?? ccs.Password)) {
                throw new ApiArgumentException("identity", "Cannot access certificate store");
            }

            //
            // Update ccs
            if (username != null) {
                ccs.UserName = username;
            }

            if (password != null) {
                ccs.Password = password;
            }

            if (physicalPath != null) {
                ccs.PhysicalPath = physicalPath;
            }

            if (privateKeyPassword != null) {
                ccs.PrivateKeyPassword = privateKeyPassword;
            }
        }

        public static async Task Enable(dynamic model, IFileProvider fileProvider)
        {
            string username, password, path, privateKeyPassword;
            ExtractModel(model, out username, out password, out path, out privateKeyPassword);

            //
            // Validate model and extract values
            if (string.IsNullOrEmpty(path)) {
                throw new ApiArgumentException("path");
            }

            if (string.IsNullOrEmpty(username)) {
                throw new ApiArgumentException("identity.username");
            }

            if (string.IsNullOrEmpty(password)) {
                throw new ApiArgumentException("identity.password");
            }

            await SetFeatureEnabled(true);

            var ccs = Startup.CentralCertificateStore;

            Update(model, fileProvider);

            ccs.Enabled = true;

            if (CertificateStoreProviderAccessor.Instance != null) {
                CertificateStoreProviderAccessor.Instance.AddStore(ccs);
            }
        }

        public static async Task Disable()
        {
            var ccs = Startup.CentralCertificateStore;

            ccs.Enabled = false;

            if (CertificateStoreProviderAccessor.Instance != null) {
                CertificateStoreProviderAccessor.Instance.RemoveStore(ccs);
            }

            await SetFeatureEnabled(false);
        }

        public static string GetLocation()
        {
            return $"/{Defines.PATH}/{new CentralCertConfigId().Uuid}";
        }

        private static void ExtractModel(dynamic model, out string username, out string password, out string path, out string privateKeyPassword)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (!(model is JObject)) {
                throw new ApiArgumentException("model", ApiArgumentException.EXPECTED_OBJECT);
            }

            dynamic identity = model.identity;
            if (identity != null && !(identity is JObject)) {
                throw new ApiArgumentException("identity", ApiArgumentException.EXPECTED_OBJECT);
            }

            //
            // Validate model and extract values
            path = DynamicHelper.Value(model.path);
            privateKeyPassword = DynamicHelper.Value(model.private_key_password);
            username = null;
            password = null;

            if (identity != null) {
                username = DynamicHelper.Value(identity.username);
                password = DynamicHelper.Value(identity.password);
            }
        }

        private static bool TestConnection(string path, string username, string password)
        {
            var ccs = Startup.CentralCertificateStore;

            try {
                using (SafeAccessTokenHandle ccsUser = CentralCertificateStore.LogonUser(username ?? ccs.UserName, password ?? ccs.Password)) {
                    WindowsIdentity.RunImpersonated(ccsUser, () => {
                        Directory.GetFiles(path);
                    });
                }
            }
            catch {
                return false;
            }
            return true;
        }

        private static async Task SetFeatureEnabled(bool enabled)
        {
            IWebServerFeatureManager featureManager = WebServerFeatureManagerAccessor.Instance;
            if (featureManager != null) {
                await (enabled ? featureManager.Enable(FEATURE_NAME) : featureManager.Disable(FEATURE_NAME));
            }
        }
    }
}
