// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications {
    using AppPools;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Files;
    using Sites;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;


    [RequireWebServer]
    public class ApplicationsController : ApiBaseController
    {
        private IFileProvider _fileProvider;

        public ApplicationsController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WebAppsName)]
        public object Get() {

            IEnumerable<ApplicationInfo> apps = null;

            //
            // Filter by AppPool
            string appPoolUuid = Context.Request.Query[AppPools.Defines.IDENTIFIER];

            if (!string.IsNullOrEmpty(appPoolUuid)) {
                ApplicationPool pool = AppPoolHelper.GetAppPool(AppPoolId.CreateFromUuid(appPoolUuid).Name);

                if (pool == null) {
                    return NotFound();
                }

                apps = ApplicationHelper.GetApplications(pool);
            }

            //
            // Filter by Site
             if (apps == null) {
                Site site = SiteHelper.ResolveSite();

                if (site == null) {
                    return NotFound();
                }

                apps = site.Applications.Select(app => new ApplicationInfo { Application = app, Site = site });
            }


            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(apps.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                webapps = apps.Select(app => ApplicationHelper.ToJsonModelRef(app.Application, app.Site, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WebAppName)]
        public object Get(string id)
        {
            // Cut off the notion of uuid from beginning of request
            ApplicationId appId = new ApplicationId(id);

            Site site = SiteHelper.GetSite(appId.SiteId);
            
            // Get the application using data encoded in uuid
            Application app = ApplicationHelper.GetApplication(appId.Path, site);

            if (app == null) {
                return NotFound();
            }

            return ApplicationHelper.ToJsonModel(app, site, Context.Request.GetFields());
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.WebAppName)]
        public object Post([FromBody] dynamic model)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }

            Site site = SiteHelper.ResolveSite(model);
            if(site == null) {
                throw new ApiArgumentException("website");
            }

            // Create app
            Application app = ApplicationHelper.CreateApplication(model, site, _fileProvider);

            // Check case of duplicate app. Adding duplicate app would result in System.Exception which we don't want to catch
            if (site.Applications.Any(a => a.Path.Equals(app.Path))) {
                throw new AlreadyExistsException("path");
            }

            // Add to site
            app = site.Applications.Add(app);

            // Save it
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic application = (dynamic) ApplicationHelper.ToJsonModel(app, site, Context.Request.GetFields());
            return Created((string)ApplicationHelper.GetLocation(application.id), application);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.WebAppName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            // Cut off the notion of uuid from beginning of request
            ApplicationId appId = new ApplicationId(id);

            Site site = SiteHelper.GetSite(appId.SiteId);

            Application app = ApplicationHelper.GetApplication(appId.Path, site);

            if (app == null) {
                return NotFound();
            }

            ApplicationHelper.UpdateApplication(app, site, model, _fileProvider);

            // Save changes
            ManagementUnit.Current.Commit();
            

            //
            // Create response
            dynamic application = ApplicationHelper.ToJsonModel(app, site, Context.Request.GetFields());

            // The Id could change by changing path.
            if (application.id != id) {
                return LocationChanged(ApplicationHelper.GetLocation(application.id), application);
            }

            return application;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            // Cut off the notion of uuid from beginning of request
            ApplicationId appId = new ApplicationId(id);
            
            Site site = SiteHelper.GetSite(appId.SiteId);
            
            Application app = ApplicationHelper.GetApplication(appId.Path, site);

            if(app != null) {

                ApplicationHelper.DeleteApplication(app, site);

                // Save changes
                ManagementUnit.Current.Commit();
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
