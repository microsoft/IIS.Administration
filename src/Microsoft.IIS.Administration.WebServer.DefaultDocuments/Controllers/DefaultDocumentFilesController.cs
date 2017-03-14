// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using AspNetCore.Mvc;
    using Web.Administration;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Sites;
    using Newtonsoft.Json.Linq;
    using Core;
    using Core.Utils;
    using Core.Http;


    [RequireGlobalModule(DefaultDocumentHelper.MODULE, "Default Document")]
    public class DefaultDocumentFilesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.EntriesName)]
        public object Get()
        {
            string docUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(docUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }
            
            DefaultDocumentId docId = new DefaultDocumentId(docUuid);           

            // Get site and application file is for if applicable
            Site site = docId.SiteId == null ? null : SiteHelper.GetSite(docId.SiteId.Value);

            List<File> files = FilesHelper.GetFiles(site, docId.Path);

            return new {
                files = files.Select(f => FilesHelper.ToJsonModelRef(f, site, docId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Get(string id)
        {
            FileId fileId = new FileId(id);

            Site site = fileId.SiteId == null ? null : SiteHelper.GetSite(fileId.SiteId.Value);

            if (fileId.SiteId != null && site == null) {
                // The file id specified a site but we couldn't find it, 
                // therefore we can't get the file
                return NotFound();
            }

            File file = FilesHelper.GetFiles(site, fileId.Path).Where(f => f.Name.Equals(fileId.Name)).FirstOrDefault();

            if(file == null) {
                return NotFound();
            }

            return FilesHelper.ToJsonModel(file, site, fileId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = new FileId(id);

            Site site = fileId.SiteId == null ? null : SiteHelper.GetSite(fileId.SiteId.Value);

            if (fileId.SiteId != null && site == null) {
                // The file id specified a site but we couldn't find it, 
                // therefore we can't get the file
                return NotFound();
            }

            File file = FilesHelper.GetFiles(site, fileId.Path).Where(f => f.Name.Equals(fileId.Name)).FirstOrDefault();

            if (file == null) {
                return NotFound();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = DefaultDocumentHelper.GetDefaultDocumentSection(site, fileId.Path, ManagementUnit.ResolveConfigScope(model));
            FilesHelper.UpdateFile(file, model, section);

            ManagementUnit.Current.Commit();

            dynamic fle = FilesHelper.ToJsonModel(file, site, fileId.Path);

            if (fle.id != id) {
                return LocationChanged(FilesHelper.GetLocation(fle.id), fle);
            }

            return fle;
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Post([FromBody] dynamic model)
        {
            File file = null;            
            DefaultDocumentId docId = null;
            Site site = null;


            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.default_document == null) {
                throw new ApiArgumentException("default_document");
            }
            if (!(model.default_document is JObject)) {
                throw new ApiArgumentException("default_document");
            }
            // Creating a a file instance requires referencing the target feature
            string docUuid = DynamicHelper.Value(model.default_document.id);
            if (docUuid == null) {
                throw new ApiArgumentException("default_document.id");
            }
                
            // Get the default document feature id
            docId = new DefaultDocumentId(docUuid);

            site = docId.SiteId == null ? null : SiteHelper.GetSite(docId.SiteId.Value);

            // Get target configuration section for addition of file
            string configPath = ManagementUnit.ResolveConfigScope(model);
            DefaultDocumentSection section = DefaultDocumentHelper.GetDefaultDocumentSection(site, docId.Path, configPath);

            // Create default document
            file = FilesHelper.CreateFile(model, section);

            // Add it
            FilesHelper.AddFile(file, section);

            // Save
            ManagementUnit.Current.Commit();
           
            //
            // Create response
            dynamic f = FilesHelper.ToJsonModel(file, site, docId.Path);
            return Created(FilesHelper.GetLocation(f.id), f);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var fileId = new FileId(id);

            Site site = fileId.SiteId == null ? null : SiteHelper.GetSite(fileId.SiteId.Value);

            if (fileId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            File file = FilesHelper.GetFiles(site, fileId.Path).Where(f => f.Name.Equals(fileId.Name)).FirstOrDefault();

            if (file != null) {
                
                var section = DefaultDocumentHelper.GetDefaultDocumentSection(site, fileId.Path, ManagementUnit.ResolveConfigScope());

                FilesHelper.DeleteFile(file, section);
                ManagementUnit.Current.Commit();

            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
