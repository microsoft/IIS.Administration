// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core.Utils;
    using Microsoft.IIS.Administration.Core;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Dynamic;
    using Web.Administration;

    static class RewriteHelper
    {
        public const string DISPLAY_NAME = "IIS Url Rewrite";
        public const string MODULE = "RewriteModule";

        private static readonly Fields RefFields = new Fields("id", "scope");

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }

        public static object ToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(site, path, RefFields, false);
            }
            else {
                return ToJsonModel(site, path, fields, false);
            }
        }

        public static object ToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            RewriteId rewriteId = new RewriteId(site?.Id, path);

            //
            // id
            if (fields.Exists("id")) {
                obj.id = rewriteId.Uuid;
            }

            //
            // scope
            if (fields.Exists("scope")) {
                obj.scope = site == null ? string.Empty : site.Name + path;
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site);
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static RewriteId GetRewriteIdFromBody(dynamic model)
        {
            RewriteId rewriteId = null;

            if (model.url_rewrite != null) {

                if (!(model.url_rewrite is JObject)) {
                    throw new ApiArgumentException("url_rewrite", ApiArgumentException.EXPECTED_OBJECT);
                }

                string id = DynamicHelper.Value(model.url_rewrite.id);

                if (!string.IsNullOrEmpty(id)) {
                    rewriteId = new RewriteId(id);
                }
            }

            return rewriteId;
        }
    }
}

