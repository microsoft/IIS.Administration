// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.VirtualDirectories
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Files;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;


    [RequireWebServer]
    public class VirtualDirectoriesController : ApiBaseController
    {
        private const string HIDDEN_FIELDS = "model.identity.password";
        private IFileProvider _fileProvider;

        public VirtualDirectoriesController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.VirtualDirectoriesName)]
        public object Get()
        {
            List<Tuple<VirtualDirectory, Application, Site>> vDirsWithParents = new List<Tuple<VirtualDirectory, Application, Site>>();

            // Get List of virtual directories whilst associating them with their parent application and site
            ManagementUnit.ServerManager.Sites.ToList().ForEach(site => {
                site.Applications.ToList().ForEach(app => {
                    app.VirtualDirectories.ToList().ForEach(vdir => {
                        vDirsWithParents.Add(new Tuple<VirtualDirectory, Application, Site>(vdir, app, site));
                    });
                });
            });

            // Filter using site 
            if (Context.Request.Query.ContainsKey(Sites.Defines.IDENTIFIER)) {

                // Extract site uuid
                string siteUuid = Context.Request.Query[Sites.Defines.IDENTIFIER];
                Site site = SiteHelper.GetSite(new SiteId(siteUuid).Id);

                // No vdir can match on a non existant site
                if (site == null) {
                    return NotFound();
                }
                else {

                    vDirsWithParents = vDirsWithParents.Where((vdirTuple) => {
                        return vdirTuple.Item3.Id == site.Id;
                    }).ToList();
                }
            }

            // Filter using application 
            else {
                // Get site and app associated
                Site site = ApplicationHelper.ResolveSite();
                Application app = null;

                if (site != null) {
                    string path = ApplicationHelper.ResolvePath();
                    app = site.Applications.FirstOrDefault(a => a.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
                }                
                
                if (app == null) {
                    return NotFound();
                }
                else {
                    // App is not null, therefore site is not null
                    vDirsWithParents = vDirsWithParents.Where((vdirTuple) => {
                        return vdirTuple.Item2.Path.Equals(app.Path) && vdirTuple.Item3.Id == site.Id;
                    }).ToList();
                }
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(vDirsWithParents.Count());

            Fields fields = Context.Request.GetFields();

            // Return reference representations of all virtual directories
            return new {
                virtual_directories = vDirsWithParents.Select(tuple => {
                    return VDirHelper.ToJsonModelRef(tuple.Item1, tuple.Item2, tuple.Item3, fields);
                })
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.VirtualDirectoryName)]
        public object Get(string id)
        {
            // Cut off the notion of uuid from beginning of request
            VDirId vdirId = new VDirId(id);

            Site site = SiteHelper.GetSite(vdirId.SiteId);

            // Get the parent application using data encoded in uuid
            Application app = ApplicationHelper.GetApplication(vdirId.AppPath, site);

            // Get the target vdir from the id data
            VirtualDirectory vdir = VDirHelper.GetVDir(vdirId.Path, app);

            if (vdir == null) {
                return NotFound();
            }

            return VDirHelper.ToJsonModel(vdir, app, site, Context.Request.GetFields());
        }

        [HttpPost]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        [ResourceInfo(Name = Defines.VirtualDirectoryName)]
        public object Post([FromBody] dynamic model)
        {
            Site site = ApplicationHelper.ResolveSite(model);
            string path = ApplicationHelper.ResolvePath(model);

            if (site == null) {
                throw new ApiArgumentException("site/application");
            }

            Application app = ApplicationHelper.GetApplication(path, site);

            // Create VDir
            VirtualDirectory vdir = VDirHelper.CreateVDir(app, model, _fileProvider);

            // Check case of duplicate vdir. Adding duplicate vdir would result in System.Exception which we don't want to catch
            if (app.VirtualDirectories.Any(v => v.Path.Equals(vdir.Path, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("path");
            }

            // Add new virtual directory to application
            app.VirtualDirectories.Add(vdir);

            // Save it
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic virtualDir = (dynamic) VDirHelper.ToJsonModel(vdir, app, site, Context.Request.GetFields());

            return Created((string)VDirHelper.GetLocation(virtualDir.id), virtualDir);
        }

        [HttpPatch]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        [ResourceInfo(Name = Defines.VirtualDirectoryName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            // Cut off the notion of uuid from beginning of request
            VDirId vdirId = new VDirId(id);

            Site site = SiteHelper.GetSite(vdirId.SiteId);

            // Get the parent application using data encoded in uuid
            Application app = ApplicationHelper.GetApplication(vdirId.AppPath, site);

            // Get the target vdir from the id data
            VirtualDirectory vdir = VDirHelper.GetVDir(vdirId.Path, app);

            if (vdir == null) {
                return NotFound();
            }
            
            // Make changes
            VDirHelper.UpdateVirtualDirectory(vdir, model, _fileProvider);

            // Save
            ManagementUnit.Current.Commit();

            //            
            // Create response
            dynamic virtualDir = VDirHelper.ToJsonModel(vdir, app, site, Context.Request.GetFields());

            // Id can change if path is different
            if (virtualDir.id != id) {
                return LocationChanged(VDirHelper.GetLocation(virtualDir.id), virtualDir);
            }

            return virtualDir;
        }

        [HttpDelete]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        public void Delete(string id)
        {
            // Cut off the notion of uuid from beginning of request
            VDirId vdirId = new VDirId(id);

            Site site = SiteHelper.GetSite(vdirId.SiteId);

            // Get the parent application using data encoded in uuid
            Application app = ApplicationHelper.GetApplication(vdirId.AppPath, site);

            // Get the target vdir from the id data
            VirtualDirectory vdir = VDirHelper.GetVDir(vdirId.Path, app);

            if(vdir != null) {

                // Delete
                VDirHelper.DeleteVirtualDirectory(vdir, app);

                // Save
                ManagementUnit.Current.Commit();
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
