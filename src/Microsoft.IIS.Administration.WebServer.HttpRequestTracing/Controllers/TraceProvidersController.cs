// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(Helper.TRACING_MODULE, Helper.DISPLAY_NAME)]
    [RequireGlobalModule(Helper.FAILED_REQUEST_TRACING_MODULE, Helper.DISPLAY_NAME)]
    public class TraceProvidersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ProvidersName)]
        public object Get()
        {
            string hrtUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(hrtUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }
            
            HttpRequestTracingId hrtId = new HttpRequestTracingId(hrtUuid);           

            Site site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            var providers = ProvidersHelper.GetProviders(site, hrtId.Path);

            this.Context.Response.SetItemsCount(providers.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                providers = providers.Select(p => ProvidersHelper.ToJsonModelRef(p, site, hrtId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ProviderName)]
        public object Get(string id)
        {
            ProviderId providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);

            if (providerId.SiteId != null && site == null) {
                return NotFound();
            }

            TraceProviderDefinition provider = ProvidersHelper.GetProviders(site, providerId.Path).Where(p => p.Name.Equals(providerId.Name)).FirstOrDefault();

            if(provider == null) {
                return NotFound();
            }

            return ProvidersHelper.ToJsonModel(provider, site, providerId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.ProviderName)]
        public object Post(dynamic model)
        {
            TraceProviderDefinition provider = null;
            Site site = null;
            HttpRequestTracingId hrtId = null;

            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.request_tracing == null) {
                throw new ApiArgumentException("request_tracing");
            }
            if (!(model.request_tracing is JObject)) {
                throw new ApiArgumentException("request_tracing", ApiArgumentException.EXPECTED_OBJECT);
            }
            string hrtUuid = DynamicHelper.Value(model.request_tracing.id);
            if (hrtUuid == null) {
                throw new ApiArgumentException("request_tracing.id");
            }

            hrtId = new HttpRequestTracingId(hrtUuid);

            site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            var section = Helper.GetTraceProviderDefinitionSection(site, hrtId.Path, configPath);

            provider = ProvidersHelper.CreateProvider(model, section);

            ProvidersHelper.AddProvider(provider, section);

            ManagementUnit.Current.Commit();

            dynamic p = ProvidersHelper.ToJsonModel(provider, site, hrtId.Path);
            return Created(ProvidersHelper.GetLocation(p.id), p);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.ProviderName)]
        public object Patch(string id, dynamic model)
        {
            ProviderId providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);

            if (providerId.SiteId != null && site == null) {
                return NotFound();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string configPath = ManagementUnit.ResolveConfigScope(model);
            TraceProviderDefinition provider = ProvidersHelper.GetProviders(site, providerId.Path, configPath).Where(p => p.Name.ToString().Equals(providerId.Name)).FirstOrDefault();

            if (provider == null) {
                return NotFound();
            }


            provider = ProvidersHelper.UpdateProvider(provider, model, Helper.GetTraceProviderDefinitionSection(site, providerId.Path, configPath));

            ManagementUnit.Current.Commit();

            dynamic prov = ProvidersHelper.ToJsonModel(provider, site, providerId.Path);

            if (prov.id != id) {
                return LocationChanged(ProvidersHelper.GetLocation(prov.id), prov);
            }

            return prov;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            ProviderId providerId = new ProviderId(id);

            Site site = providerId.SiteId == null ? null : SiteHelper.GetSite(providerId.SiteId.Value);
            Application app = ApplicationHelper.GetApplication(providerId.Path, site);

            if (providerId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            TraceProviderDefinition provider = ProvidersHelper.GetProviders(site, providerId.Path).Where(r => r.Name.ToString().Equals(providerId.Name)).FirstOrDefault();

            if (provider != null) {

                var section = Helper.GetTraceProviderDefinitionSection(site, providerId.Path, ManagementUnit.ResolveConfigScope());

                ProvidersHelper.DeleteProvider(provider, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
