// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using Applications;
    using AspNetCore.Mvc;
    using Sites;
    using System.Net;
    using Web.Administration;
    using Core.Http;
    using Core;


    [RequireWebServer]
    public class HttpResponseHeadersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ResponseHeadersName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = HttpResponseHeadersHelper.ToJsonModel(site, path);
            return LocationChanged(HttpResponseHeadersHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ResponseHeadersName)]
        public object Get(string id)
        {
            HttpResponseHeadersId headerId = new HttpResponseHeadersId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            return HttpResponseHeadersHelper.ToJsonModel(site, headerId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.ResponseHeadersName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            HttpResponseHeadersId headerId = new HttpResponseHeadersId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            // Check for config_scope
            string configScope = ManagementUnit.ResolveConfigScope(model); ;
            HttpProtocolSection section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, configScope);

            HttpResponseHeadersHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            return HttpResponseHeadersHelper.ToJsonModel(site, headerId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            HttpResponseHeadersId headerId = new HttpResponseHeadersId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (headerId.SiteId != null) ? SiteHelper.GetSite(headerId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            HttpProtocolSection section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, ManagementUnit.ResolveConfigScope());

            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
