// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Core;
    using Core.Utils;
    using AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Web.Http;
    using Web.Administration;
    using Core.Http;

    [RequireGlobalModule("StaticFileModule", "Static Content")]
    public class MimeMapsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.MimeMapsName)]
        public object Get()
        {
            string staticContentUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(staticContentUuid)) {
                return NotFound();
            }

            StaticContentId id = new StaticContentId(staticContentUuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<MimeMap> mappings = MimeMapHelper.GetMimeMaps(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(mappings.Count());

            Fields fields = Context.Request.GetFields();

            return new
            {
                mime_maps = mappings.Select(m => MimeMapHelper.ToJsonModelRef(m, site, id.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.MimeMapName)]
        public object Get(string id)
        {
            var mimeMapId = new MimeMapId(id);

            Site site = mimeMapId.SiteId == null ? null : SiteHelper.GetSite(mimeMapId.SiteId.Value);
            
            List<MimeMap> mimeMaps = MimeMapHelper.GetMimeMaps(site, mimeMapId.Path);

            MimeMap mimeMap = mimeMaps.FirstOrDefault(m => m.FileExtension.Equals(mimeMapId.FileExtension));

            if(mimeMap == null) {
                return NotFound();
            }

            return MimeMapHelper.ToJsonModel(mimeMap, site, mimeMapId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.MimeMapName)]
        public object Post([FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.static_content == null) {
                throw new ApiArgumentException("static_content");
            }
            if (!(model.static_content is JObject)) {
                throw new ApiArgumentException("static_content");
            }
            string staticContentUuid = DynamicHelper.Value(model.static_content.id);
            if (staticContentUuid == null) {
                throw new ApiArgumentException("static_content.id");
            }

            // Get the feature id
            StaticContentId staticContentId = new StaticContentId(staticContentUuid);

            Site site = staticContentId.SiteId == null ? null : SiteHelper.GetSite(staticContentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            StaticContentSection section = StaticContentHelper.GetSection(site, staticContentId.Path, configPath);

            // Create mime map
            MimeMap mimeMap = MimeMapHelper.CreateMimeMap(model, section);

            // Add it to the config file
            MimeMapHelper.AddMimeMap(mimeMap, section);

            // Save changes
            ManagementUnit.Current.Commit();


            // Show new mime map
            return MimeMapHelper.ToJsonModel(mimeMap, site, staticContentId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.MimeMapName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            var mimeMapId = new MimeMapId(id);

            Site site = mimeMapId.SiteId == null ? null : SiteHelper.GetSite(mimeMapId.SiteId.Value);

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            List<MimeMap> mimeMaps = MimeMapHelper.GetMimeMaps(site, mimeMapId.Path, configPath);

            MimeMap mimeMap = mimeMaps.FirstOrDefault(m => m.FileExtension.Equals(mimeMapId.FileExtension));

            if (mimeMap == null) {
                return new NotFoundResult();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = StaticContentHelper.GetSection(site, mimeMapId.Path, configPath);
            MimeMapHelper.UpdateMimeMap(model, mimeMap, section);

            ManagementUnit.Current.Commit();

            dynamic mm = MimeMapHelper.ToJsonModel(mimeMap, site, mimeMapId.Path);

            if (mm.id != id) {
                return LocationChanged(MimeMapHelper.GetLocation(mm.id), mm);
            }

            return mm;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var mimeMapId = new MimeMapId(id);

            Site site = mimeMapId.SiteId == null ? null : SiteHelper.GetSite(mimeMapId.SiteId.Value);

            if (mimeMapId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            MimeMap mimeMap = MimeMapHelper.GetMimeMaps(site, mimeMapId.Path).FirstOrDefault(m => m.FileExtension.Equals(mimeMapId.FileExtension));

            if (mimeMap != null) {

                var section = StaticContentHelper.GetSection(site, mimeMapId.Path, ManagementUnit.ResolveConfigScope());

                MimeMapHelper.DeleteMimeType(mimeMap, section);
                ManagementUnit.Current.Commit();
            }


            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
