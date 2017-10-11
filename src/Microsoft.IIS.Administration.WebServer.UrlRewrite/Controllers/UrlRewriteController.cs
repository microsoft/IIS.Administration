// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Microsoft.IIS.Administration.Core.Utils;
    using Sites;
    using System.Net;
    using System.Threading.Tasks;
    using Web.Administration;

    [RequireWebServer]
    public class UrlRewriteController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.UrlRewriteName)]
        [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = RewriteHelper.ToJsonModel(site, path);
            return LocationChanged(RewriteHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.UrlRewriteName)]
        [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return RewriteHelper.ToJsonModel(site, rewriteId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.UrlRewriteName)]
        public async Task<object> Post()
        {
            var featureManager = new UrlRewriteFeatureManager();

            if (featureManager.IsInstalled()) {
                throw new AlreadyExistsException(RewriteHelper.DISPLAY_NAME);
            }

            if (Os.IsNanoServer) {
                throw new ApiNotAllowedException("Action not supported on current platform", null);
            }

            await featureManager.Install();

            dynamic settings = RewriteHelper.ToJsonModel(null, null);
            return Created(RewriteHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            RewriteId rewriteId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (rewriteId.SiteId != null) ? SiteHelper.GetSite(rewriteId.SiteId.Value) : null;

            var featureManager = new UrlRewriteFeatureManager();

            // When target is webserver, uninstall
            if (rewriteId.SiteId == null && featureManager.IsInstalled()) {

                if (Os.IsNanoServer) {
                    throw new ApiNotAllowedException("Action not supported on current platform", null);
                }

                await featureManager.Uninstall();
            }
        }
    }
}

