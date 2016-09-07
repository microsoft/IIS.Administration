// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications {
    using AspNetCore.Mvc;
    using Web.Administration;
    using System.Linq;
    using Core;
    using Sites;
    using System.Net;
    using System.Collections.Generic;
    using AppPools;
    using Core.Http;
    using Core.Utils;



    public class ApplicationsController : ApiBaseController {
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
            Application app = ApplicationHelper.CreateApplication(model, site);

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
            dynamic application = (dynamic) ApplicationHelper.ToJsonModel(app, site);
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

            ApplicationHelper.UpdateApplication(app, site, model);

            // Save changes
            ManagementUnit.Current.Commit();
            

            //
            // Create response
            dynamic application = ApplicationHelper.ToJsonModel(app, site);

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
