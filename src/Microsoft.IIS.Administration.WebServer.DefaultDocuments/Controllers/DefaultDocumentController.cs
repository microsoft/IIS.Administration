// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using AspNetCore.Mvc;
    using Core;
    using Web.Administration;
    using System.Net;
    using Sites;
    using Applications;
    using Core.Http;
    using System.Threading.Tasks;


    [RequireWebServer]
    public class DefaultDocumentController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Default Document";

        [HttpGet]
        [ResourceInfo(Name = Defines.DefaultDocumentsName)]
        [RequireGlobalModule(DefaultDocumentHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }            

            dynamic d = DefaultDocumentHelper.ToJsonModel(site, path);
            return LocationChanged(DefaultDocumentHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.DefaultDocumentsName)]
        [RequireGlobalModule(DefaultDocumentHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            // Decode default document id from uuid parameter
            DefaultDocumentId docId = new DefaultDocumentId(id);

            Site site = docId.SiteId == null ? null : SiteHelper.GetSite(docId.SiteId.Value);
            
            return DefaultDocumentHelper.ToJsonModel(site, docId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.DefaultDocumentsName)]
        [RequireGlobalModule(DefaultDocumentHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Decode default document id from uuid parameter
            DefaultDocumentId docId = new DefaultDocumentId(id);

            Site site = docId.SiteId == null ? null : SiteHelper.GetSite(docId.SiteId.Value);

            if (docId.SiteId != null && site == null) {
                // The document id specified a site but we couldn't find it, 
                // therefore we can't get the settings for that site
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            DefaultDocumentSection section = DefaultDocumentHelper.GetDefaultDocumentSection(site, docId.Path, configPath);

            DefaultDocumentHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            return DefaultDocumentHelper.ToJsonModel(site, docId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.DefaultDocumentsName)]
        public async Task<object> Post()
        {
            if (DefaultDocumentHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(DefaultDocumentHelper.FEATURE_NAME);
            }

            await DefaultDocumentHelper.SetFeatureEnabled(true);

            dynamic settings = DefaultDocumentHelper.ToJsonModel(null, null);
            return Created(DefaultDocumentHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            DefaultDocumentId docId = new DefaultDocumentId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (docId.SiteId != null) ? SiteHelper.GetSite(docId.SiteId.Value) : null;

            if (site != null) {
                var section = DefaultDocumentHelper.GetDefaultDocumentSection(site, docId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }

            // When target is webserver, uninstall
            if (docId.SiteId == null && DefaultDocumentHelper.IsFeatureEnabled()) {
                await DefaultDocumentHelper.SetFeatureEnabled(false);
            }
        }
    }
}
