// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.SslSettings
{
    using Core;
    using Web.Administration;
    using Sites;
    using System;
    using System.IO;
    using Core.Utils;

    public static class SslSettingsHelper
    {

        internal static object ToJsonModel(Site site, string path)
        {
            if(site == null) {
                throw new ArgumentException("site");
            }

            var section = GetAccessSection(site, path);

            bool isLocal = section.IsLocallyStored;
            bool isLocked = section.IsLocked;
            OverrideMode overrideMode = section.OverrideMode;
            OverrideMode overrideModeEffective = section.OverrideModeEffective;

            HttpAccessSslFlags sslFlags = section.SslFlags;
            ClientCertificateSettings clientCertSettings;
            
            if (sslFlags.HasFlag(HttpAccessSslFlags.SslRequireCert)) {
                clientCertSettings = ClientCertificateSettings.REQUIRE;
            }
            else if (sslFlags.HasFlag(HttpAccessSslFlags.SslNegotiateCert)) {
                clientCertSettings = ClientCertificateSettings.ACCEPT;
            }
            else {
                clientCertSettings = ClientCertificateSettings.IGNORE;
            }

            bool hasHttpsBinding = false;
            if (!(site == null)) {
                foreach (Binding binding in site.Bindings) {
                    if (binding.Protocol.Equals("https", StringComparison.OrdinalIgnoreCase)) {
                        hasHttpsBinding = true;
                        break;
                    }
                }
            }

            SslSettingId id = new SslSettingId(site?.Id, path, isLocal);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(isLocal, isLocked, overrideMode, overrideModeEffective),
                require_ssl = sslFlags.HasFlag(HttpAccessSslFlags.Ssl) || sslFlags.HasFlag(HttpAccessSslFlags.Ssl128),
                client_certificates = Enum.GetName(typeof(ClientCertificateSettings), clientCertSettings).ToLower(),
                has_https_binding = hasHttpsBinding,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static void UpdateSettings(dynamic model, Site site, string path, string configScope = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            AccessSection section = GetAccessSection(site, path, configScope);

            int newFlags = (int)section.SslFlags;

            bool? requireSsl = DynamicHelper.To<bool>(model.require_ssl);
            if(requireSsl != null) {

                if(requireSsl.Value) {
                    SetFlag(ref newFlags, (int)HttpAccessSslFlags.Ssl);
                }
                else {
                    ClearFlag(ref newFlags, (int)HttpAccessSslFlags.Ssl);
                    ClearFlag(ref newFlags, (int)HttpAccessSslFlags.Ssl128);
                }
            }

            ClientCertificateSettings? certSettings = DynamicHelper.To<ClientCertificateSettings>(model.client_certificates);
            if(certSettings != null) {

                // Client certificate settings
                switch (certSettings.Value) {
                    case ClientCertificateSettings.ACCEPT:
                        SetFlag(ref newFlags, (int)HttpAccessSslFlags.SslNegotiateCert);
                        ClearFlag(ref newFlags, (int)HttpAccessSslFlags.SslRequireCert);
                        break;
                    case ClientCertificateSettings.IGNORE:
                        ClearFlag(ref newFlags, (int)HttpAccessSslFlags.SslNegotiateCert);
                        ClearFlag(ref newFlags, (int)HttpAccessSslFlags.SslRequireCert);
                        break;
                    case ClientCertificateSettings.REQUIRE:
                        SetFlag(ref newFlags, (int)HttpAccessSslFlags.SslNegotiateCert);
                        SetFlag(ref newFlags, (int)HttpAccessSslFlags.SslRequireCert);
                        break;
                    default:
                        break;
                }

            }

            try {

                if (model.metadata != null) {

                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => {
                        section.OverrideMode = v;
                    });
                }

                section.SslFlags = (HttpAccessSslFlags)newFlags;
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        private static void SetFlag(ref int value, int flag)
        {
            value |= flag;
        }

        private static void ClearFlag(ref int value, int flag)
        {
            value &= ~flag;
        }

        public static AccessSection GetAccessSection(Site site, string path, string configScope = null)
        {
            return (AccessSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           SslGlobals.AccessModesSectionName,
                                                                           typeof(AccessSection),
                                                                           configScope);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 SslGlobals.AccessModesSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }

        public class AccessSection : ConfigurationSection
        {

            private const string SslFlagsAttribute = "sslFlags";

            public AccessSection()
            {
            }

            public HttpAccessSslFlags SslFlags
            {
                get
                {
                    return (HttpAccessSslFlags)base[SslFlagsAttribute];
                }
                set
                {
                    base[SslFlagsAttribute] = (int)value;
                }
            }
        }
    }
}
