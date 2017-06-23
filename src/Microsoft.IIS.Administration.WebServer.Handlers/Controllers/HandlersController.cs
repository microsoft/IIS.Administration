// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Sites;
    using System.Net;
    using Web.Administration;
    using Core.Http;


    [RequireWebServer]
    public class HandlersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.HandlersName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = HandlersHelper.ToJsonModel(site, path);
            return LocationChanged(HandlersHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HandlersName)]
        public object Get(string id)
        {
            HandlersId handlersId = new HandlersId(id);

            Site site = handlersId.SiteId == null ? null : SiteHelper.GetSite(handlersId.SiteId.Value);

            if (handlersId.SiteId != null && site == null) {
                return NotFound();
            }

            return HandlersHelper.ToJsonModel(site, handlersId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.HandlersName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            HandlersId handlersId = new HandlersId(id);

            Site site = handlersId.SiteId == null ? null : SiteHelper.GetSite(handlersId.SiteId.Value);

            if (handlersId.SiteId != null && site == null) {
                return NotFound();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Check for config_scope
            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model); ;
            HandlersSection section = HandlersHelper.GetHandlersSection(site, handlersId.Path, configPath);

            HandlersHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            return HandlersHelper.ToJsonModel(site, handlersId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            HandlersId handlersId = new HandlersId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (handlersId.SiteId != null) ? SiteHelper.GetSite(handlersId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            HandlersSection section = HandlersHelper.GetHandlersSection(site, handlersId.Path, ManagementUnit.ResolveConfigScope());

            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
