// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Applications;
    using AspNetCore.Mvc;
    using Sites;
    using System.Net;
    using Web.Administration;
    using Core.Http;
    using Core;

    [RequireGlobalModule("StaticCompressionModule", "Compression")]
    [RequireGlobalModule("DynamicCompressionModule", "Compression")]
    public class CompressionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.CompressionName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = CompressionHelper.ToJsonModel(site, path);
            return LocationChanged(CompressionHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CompressionName)]
        public object Get(string id)
        {
            CompressionId compId = new CompressionId(id);

            Site site = compId.SiteId == null ? null : SiteHelper.GetSite(compId.SiteId.Value);

            return CompressionHelper.ToJsonModel(site, compId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.CompressionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            CompressionId compId = new CompressionId(id);

            Site site = compId.SiteId == null ? null : SiteHelper.GetSite(compId.SiteId.Value);

            if (compId.SiteId != null && site == null) {
                // Targetting section for a site, but unable to find that site
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            CompressionHelper.UpdateSettings(model, site, compId.Path, configPath);

            ManagementUnit.Current.Commit();          

            return CompressionHelper.ToJsonModel(site, compId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            CompressionId compId = new CompressionId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (compId.SiteId != null) ? SiteHelper.GetSite(compId.SiteId.Value) : null;

            if (site == null) {
                return;
            }
            
            CompressionHelper.GetUrlCompressionSection(site, compId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            // Http compression section is not reverted because it only allows definition in apphost

            ManagementUnit.Current.Commit();
        }
    }
}
