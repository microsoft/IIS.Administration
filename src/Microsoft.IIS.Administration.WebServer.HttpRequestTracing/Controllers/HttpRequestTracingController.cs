// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Files;
    using Sites;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule("TracingModule", "Http Request Tracing")]
    [RequireGlobalModule("FailedRequestsTracingModule", "Http Request Tracing")]
    public class HttpRequestTracingController : ApiBaseController
    {
        private IFileProvider _fileProvider;

        public HttpRequestTracingController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HttpRequestTracingName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = Helper.ToJsonModel(site, path);
            return LocationChanged(Helper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HttpRequestTracingName)]
        public object Get(string id)
        {
            var hrtId = new HttpRequestTracingId(id);

            Site site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            return Helper.ToJsonModel(site, hrtId.Path);
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.HttpRequestTracingName)]
        public object Patch(string id, dynamic model)
        {
            var hrtId = new HttpRequestTracingId(id);

            Site site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            if (hrtId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            Helper.UpdateSettings(model, _fileProvider, site, hrtId.Path, configPath);

            ManagementUnit.Current.Commit();

            return Helper.ToJsonModel(site, hrtId.Path);
        }


        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var hrtId = new HttpRequestTracingId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (hrtId.SiteId != null) ? SiteHelper.GetSite(hrtId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            Helper.GetTraceFailedRequestsSection(site, hrtId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
            Helper.GetTraceProviderDefinitionSection(site, hrtId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
