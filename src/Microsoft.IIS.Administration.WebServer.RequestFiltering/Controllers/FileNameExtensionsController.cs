// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Newtonsoft.Json.Linq;
    using Core.Utils;
    using Core;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class FileNameExtensionsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.FileExtensionsName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return NotFound();
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            List<Extension> extensions = ExtensionsHelper.GetExtensions(site, reqId.Path);

            return new {
                file_extensions = extensions.Select(e => ExtensionsHelper.ToJsonModelRef(e, site, reqId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.FileExtensionName)]
        public object Get(string id)
        {
            ExtensionId extId = new ExtensionId(id);

            Site site = extId.SiteId == null ? null : SiteHelper.GetSite(extId.SiteId.Value);

            if (extId.SiteId != null && site == null) {
                return NotFound();
            }

            Extension extension = ExtensionsHelper.GetExtensions(site, extId.Path).Where(e => e.FileExtension.Equals(extId.FileExtension)).FirstOrDefault();

            if (extension == null) {
                return NotFound();
            }

            return ExtensionsHelper.ToJsonModel(extension, site, extId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.FileExtensionName)]
        public object Post([FromBody] dynamic model)
        {
            Extension extension = null;
            Site site = null;

            RequestFilteringId reqId = null;
            
            if (model == null) {
                throw new ApiArgumentException("model");
            }
                
            if (model.request_filtering == null) {
                throw new ApiArgumentException("request_filtering");
            }
            if (!(model.request_filtering is JObject)) {
                throw new ApiArgumentException(String.Empty, "request_filtering");
            }
            string reqUuid = DynamicHelper.Value(model.request_filtering.id);
            if (reqUuid == null) {
                throw new ApiArgumentException("request_filtering.id");
            }

            // Get the feature id
            reqId = new RequestFilteringId(reqUuid);

            site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            extension = ExtensionsHelper.CreateExtension(model, section);

            ExtensionsHelper.AddExtension(extension, section);
                
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic ext = ExtensionsHelper.ToJsonModel(extension, site, reqId.Path);
            return Created(ExtensionsHelper.GetLocation(ext.id), ext);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.FileExtensionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            ExtensionId extId = new ExtensionId(id);

            Site site = extId.SiteId == null ? null : SiteHelper.GetSite(extId.SiteId.Value);

            if (extId.SiteId != null && site == null) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }
            
            if(model == null) {
                throw new ApiArgumentException("model");
            }
            
            string configPath = ManagementUnit.ResolveConfigScope(model);
            Extension extension = ExtensionsHelper.GetExtensions(site, extId.Path, configPath).
                FirstOrDefault(e => e.FileExtension.ToString().Equals(extId.FileExtension));

            if (extension == null) {
                return NotFound();
            }
            
            extension = ExtensionsHelper.UpdateExtension(extension, model);

            ManagementUnit.Current.Commit();

            dynamic ext = ExtensionsHelper.ToJsonModel(extension, site, extId.Path);

            if(ext.id != id) {
                return LocationChanged(ExtensionsHelper.GetLocation(ext.id), ext);
            }

            return ext;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            ExtensionId extId = new ExtensionId(id);

            Site site = extId.SiteId == null ? null : SiteHelper.GetSite(extId.SiteId.Value);

            if (extId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            Extension extension = ExtensionsHelper.GetExtensions(site, extId.Path).Where(e => e.FileExtension.ToString().Equals(extId.FileExtension)).FirstOrDefault();

            if (extension != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, extId.Path, ManagementUnit.ResolveConfigScope());

                ExtensionsHelper.DeleteExtension(extension, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
