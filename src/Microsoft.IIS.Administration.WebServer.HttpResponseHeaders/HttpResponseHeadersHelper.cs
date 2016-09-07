// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using System;
    using Microsoft.Web.Administration;
    using Core;
    using System.Collections.Generic;
    using Sites;
    using System.IO;
    using Core.Utils;
    using System.Linq;

    public static class HttpResponseHeadersHelper
    {

        public static void UpdateFeatureSettings(dynamic model, HttpProtocolSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            try {

                DynamicHelper.If<bool>((object)model.allow_keep_alive, v => section.AllowKeepAlive = v);

                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
                }

            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        internal static object ToJsonModel(Site site, string path)
        {
            HttpProtocolSection section = GetSection(site, path);

            HttpResponseHeadersId id = new HttpResponseHeadersId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                allow_keep_alive = section.AllowKeepAlive,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetSection(site, path);

            HttpResponseHeadersId id = new HttpResponseHeadersId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static HttpProtocolSection GetSection(Site site, string path, string configScope = null)
        {

            return (HttpProtocolSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           HttpHeadersGlobals.HttpHeadersSectionName,
                                                                           typeof(HttpProtocolSection),
                                                                           configScope);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 HttpHeadersGlobals.HttpHeadersSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }
    }
}
