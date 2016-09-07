// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using AspNetCore.Mvc;
    using Web.Administration;
    using Core;
    using AppPools;
    using Core.Utils;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Core.Http;

    public class SitesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.WebsitesName)]
        public object Get() {

            IEnumerable<Site> sites = null;

            //
            // Filter by AppPool
            string appPoolUuid = Context.Request.Query[AppPools.Defines.IDENTIFIER];

            if (!string.IsNullOrEmpty(appPoolUuid)) {
                ApplicationPool pool = AppPoolHelper.GetAppPool(AppPoolId.CreateFromUuid(appPoolUuid).Name);

                if (pool == null) {
                    return NotFound();
                }

                sites = SiteHelper.GetSites(pool);
            }

            //
            // Get All Sites
            if (sites == null) {
                sites = ManagementUnit.ServerManager.Sites;
            }


            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(sites.Count());

            Fields fields = Context.Request.GetFields();

            // Return the site reference model collection
            return new {
                websites = sites.Select(s => SiteHelper.ToJsonModelRef(s, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WebsiteName)]
        public object Get(string id)
        {
            Site site = SiteHelper.GetSite(new SiteId(id).Id);

            if (site == null) {
                return NotFound();
            }
            
            return SiteHelper.ToJsonModel(site, Context.Request.GetFields());
        }

        // TODO should set location header on entity creation.
        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.WebsiteName)]
        public object Post([FromBody] dynamic model)
        {
            // Create Site
            Site site = SiteHelper.CreateSite(model);

            // Check if site with name already exists
            if(ManagementUnit.ServerManager.Sites.Any(s => s.Name.Equals(site.Name))) {
                throw new AlreadyExistsException("name");
            }

            // Save it
            ManagementUnit.ServerManager.Sites.Add(site);
            ManagementUnit.Current.Commit();


            // Refresh
            site = SiteHelper.GetSite(site.Id);

            WaitForSiteStatusResolve(site);

            //
            // Create response
            dynamic website = (dynamic) SiteHelper.ToJsonModel(site);
            return Created((string)SiteHelper.GetLocation(website.id), website);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.WebsiteName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            // Set settings
            Site site = SiteHelper.UpdateSite(new SiteId(id).Id, model);
            if (site == null) {
                return NotFound();
            }

            // Start/Stop
            if (model.status != null) {
                Status status = DynamicHelper.To<Status>(model.status);
                try {
                    switch (status) {
                        case Status.Stopped:
                            site.Stop();
                            break;
                        case Status.Started:
                            site.Start();
                            break;
                    }
                }
                catch (COMException e) {
                    
                    // If site is fresh and status is still unknown then COMException will be thrown when manipulating status
                    throw new ApiException("Error setting site status", e);
                }
                catch (ServerManagerException e) {
                    throw new ApiException(e.Message, e);
                }
            }

            // Update changes
            ManagementUnit.Current.Commit();

            // Refresh data
            site = ManagementUnit.ServerManager.Sites[site.Name];

            return SiteHelper.ToJsonModel(site);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            Site site = SiteHelper.GetSite(new SiteId(id).Id);

            if (site != null) {
                SiteHelper.DeleteSite(site);
                ManagementUnit.Current.Commit();
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }


        private void WaitForSiteStatusResolve(Site site)
        {
            // Delay to get proper status of newly created site
            int n = 10;
            for (int i = 0; i < n; i++) {
                try {
                    StatusExtensions.FromObjectState(site.State);
                    break;
                }
                catch (COMException) {
                    if (i < n - 1) {
                        Thread.Sleep(10 / n);
                    }
                }
            }
        }
    }
}
