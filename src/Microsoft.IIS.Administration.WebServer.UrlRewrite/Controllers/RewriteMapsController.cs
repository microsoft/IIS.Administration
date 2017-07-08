// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Http;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System;
    using System.Linq;
    using System.Net;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class RewriteMapsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.RewriteMapsName)]
        public object Get()
        {
            string rewriteMapsId = Context.Request.Query[Defines.REWRITE_MAPS_SECTION_IDENTIFIER];

            if (string.IsNullOrEmpty(rewriteMapsId)) {
                rewriteMapsId = Context.Request.Query[Defines.IDENTIFIER];
            }

            if (string.IsNullOrEmpty(rewriteMapsId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(rewriteMapsId);

            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            RewriteMapCollection maps = RewriteMapsHelper.GetSection(site, sectionId.Path).RewriteMaps;

            this.Context.Response.SetItemsCount(maps.Count());

            return new {
                maps = maps.Select(map => RewriteMapsHelper.MapToJsonModelRef(map, site, sectionId.Path, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RewriteMapName)]
        public object Get(string id)
        {
            var rewriteMapId = new RewriteMapId(id);

            Site site = rewriteMapId.SiteId == null ? null : SiteHelper.GetSite(rewriteMapId.SiteId.Value);

            if (rewriteMapId.SiteId != null && site == null) {
                return NotFound();
            }

            RewriteMap map = RewriteMapsHelper.GetSection(site, rewriteMapId.Path).RewriteMaps.FirstOrDefault(m => m.Name.Equals(rewriteMapId.Name, StringComparison.OrdinalIgnoreCase));

            if (map == null) {
                return NotFound();
            }

            return RewriteMapsHelper.MapToJsonModel(map, site, rewriteMapId.Path, Context.Request.GetFields());
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.RewriteMapName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var rewriteMapId = new RewriteMapId(id);

            Site site = rewriteMapId.SiteId == null ? null : SiteHelper.GetSite(rewriteMapId.SiteId.Value);

            if (rewriteMapId.SiteId != null && site == null) {
                return NotFound();
            }

            RewriteMapsSection section = RewriteMapsHelper.GetSection(site, rewriteMapId.Path);
            RewriteMap map = section.RewriteMaps.FirstOrDefault(r => r.Name.Equals(rewriteMapId.Name, StringComparison.OrdinalIgnoreCase));

            if (map == null) {
                return NotFound();
            }

            RewriteMapsHelper.UpdateMap(model, map, section);

            ManagementUnit.Current.Commit();

            dynamic updatedMap = RewriteMapsHelper.MapToJsonModel(map, site, rewriteMapId.Path, Context.Request.GetFields(), true);

            if (updatedMap.id != id) {
                return LocationChanged(RewriteMapsHelper.GetMapLocation(updatedMap.id), updatedMap);
            }

            return updatedMap;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.RewriteMapName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model) ?? RewriteMapsHelper.GetSectionIdFromBody(model);

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RewriteMapsSection section = RewriteMapsHelper.GetSection(site, parentId.Path, configPath);

            RewriteMap map = RewriteMapsHelper.CreateMap(model, section);

            RewriteMapsHelper.AddMap(map, section);

            ManagementUnit.Current.Commit();

            dynamic r = RewriteMapsHelper.MapToJsonModel(map, site, parentId.Path, Context.Request.GetFields(), true);
            return Created(RewriteMapsHelper.GetMapLocation(r.id), r);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            RewriteMap map = null;
            var rewriteMapId = new RewriteMapId(id);

            Site site = rewriteMapId.SiteId == null ? null : SiteHelper.GetSite(rewriteMapId.SiteId.Value);

            if (rewriteMapId.SiteId == null || site != null) {
                map = RewriteMapsHelper.GetSection(site, rewriteMapId.Path).RewriteMaps.FirstOrDefault(m => m.Name.Equals(rewriteMapId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (map != null) {
                var section = RewriteMapsHelper.GetSection(site, rewriteMapId.Path, ManagementUnit.ResolveConfigScope());

                RewriteMapsHelper.DeleteMap(map, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
