// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class ProvidersSectionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ProvidersSectionName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = ProvidersHelper.SectionToJsonModel(site, path);
            return LocationChanged(ProvidersHelper.GetSectionLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ProvidersSectionName)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return ProvidersHelper.SectionToJsonModel(site, rewriteId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.ProvidersSectionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var providersId = new RewriteId(id);

            Site site = providersId.SiteId == null ? null : SiteHelper.GetSite(providersId.SiteId.Value);

            if (providersId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            ProvidersHelper.UpdateSection(model, site, providersId.Path, configPath);

            ManagementUnit.Current.Commit();

            return ProvidersHelper.SectionToJsonModel(site, providersId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var providersId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (providersId.SiteId != null) ? SiteHelper.GetSite(providersId.SiteId.Value) : null;

            if (site != null) {
                var section = ProvidersHelper.GetSection(site, providersId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}

