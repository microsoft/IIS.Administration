// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class ProvidersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ProvidersName)]
        public object Get()
        {
            string providersId = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(providersId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(providersId);

            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            ProvidersCollection providers = ProvidersHelper.GetSection(site, sectionId.Path).Providers;

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(providers.Count());

            return new {
                entries = providers.Select(provider => ProvidersHelper.ProviderToJsonModelRef(provider, site, sectionId.Path, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ProviderName)]
        public object Get(string id)
        {
            var providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);

            if (providerId.SiteId != null && site == null) {
                return NotFound();
            }

            Provider provider = ProvidersHelper.GetSection(site, providerId.Path).Providers.FirstOrDefault(p => p.Name.Equals(providerId.Name, StringComparison.OrdinalIgnoreCase));

            if (provider == null) {
                return NotFound();
            }

            return ProvidersHelper.ProviderToJsonModel(provider, site, providerId.Path, Context.Request.GetFields());
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.ProviderName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);

            if (providerId.SiteId != null && site == null) {
                return NotFound();
            }

            ProvidersSection section = ProvidersHelper.GetSection(site, providerId.Path);
            Provider provider = section.Providers.FirstOrDefault(r => r.Name.Equals(providerId.Name, StringComparison.OrdinalIgnoreCase));

            if (provider == null) {
                return NotFound();
            }

            ProvidersHelper.UpdateProvider(model, provider, section);

            ManagementUnit.Current.Commit();

            dynamic updatedProvider = ProvidersHelper.ProviderToJsonModel(provider, site, providerId.Path, Context.Request.GetFields(), true);

            if (updatedProvider.id != id) {
                return LocationChanged(ProvidersHelper.GetProviderLocation(updatedProvider.id), updatedProvider);
            }

            return updatedProvider;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.ProviderName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model);

            if (parentId == null) {
                throw new ApiArgumentException("url_rewrite");
            }

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            ProvidersSection section = ProvidersHelper.GetSection(site, parentId.Path, configPath);

            Provider provider = ProvidersHelper.CreateProvider(model, section);

            // Add it
            ProvidersHelper.AddProvider(provider, section);

            // Save
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic p = ProvidersHelper.ProviderToJsonModel(provider, site, parentId.Path, Context.Request.GetFields(), true);
            return Created(ProvidersHelper.GetProviderLocation(p.id), p);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            Provider provider = null;
            var providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);

            if (providerId.SiteId == null || site != null) {
                provider = ProvidersHelper.GetSection(site, providerId.Path).Providers.FirstOrDefault(p => p.Name.Equals(providerId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (provider != null) {
                var section = ProvidersHelper.GetSection(site, providerId.Path, ManagementUnit.ResolveConfigScope());

                ProvidersHelper.DeleteProvider(provider, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
