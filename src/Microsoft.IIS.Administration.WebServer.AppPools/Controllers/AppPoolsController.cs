// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.AppPools
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Utils;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Web.Administration;
    using Core.Http;
    using System;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;


    [RequireWebServer]
    public class AppPoolsController : ApiBaseController {
        private const string AUDIT_FIELDS = "*,model.recycling.log_events.private_memory,model.recycling.periodic_restart.private_memory";
        private const string MASKED_FIELDS = "model.identity.password";
        private IAuthorizationService _authorization;

        public AppPoolsController(IAuthorizationService svc) {
            _authorization = svc;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AppPoolsName)]
        public object Get() {
            Fields fields = Context.Request.GetFields();

            // Get reference models for app pool collection
            var pools = ManagementUnit.ServerManager.ApplicationPools.Select(pool => 
                            AppPoolHelper.ToJsonModelRef(pool, fields));

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(pools.Count());

            // Return the app pool reference model collection
            return new {
                app_pools = pools
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AppPoolName)]
        public object Get(string id) {
            // Extract the name of the target app pool from the uuid specified in the request
            string name = AppPoolId.CreateFromUuid(id).Name;

            ApplicationPool pool = AppPoolHelper.GetAppPool(name);

            if (pool == null) {
                return NotFound();
            }

            return AppPoolHelper.ToJsonModel(pool, Context.Request.GetFields());
        }

        [HttpPost]
        [Audit(fields: AUDIT_FIELDS, maskedFields: MASKED_FIELDS)]
        [ResourceInfo(Name = Defines.AppPoolName)]
        public async Task<object> Post([FromBody]dynamic model)
        {
            // Create AppPool
            ApplicationPool pool = AppPoolHelper.CreateAppPool(model);

            if (!await IsAppPoolIdentityAllowed(pool)) {
                return null;
            }

            // Save it
            ManagementUnit.ServerManager.ApplicationPools.Add(pool);
            ManagementUnit.Current.Commit();

            // Refresh
            pool = AppPoolHelper.GetAppPool(pool.Name);

            //
            // Create response
            dynamic appPool = (dynamic) AppPoolHelper.ToJsonModel(pool, Context.Request.GetFields());

            // A newly created application should default to started state
            var state = WaitForPoolStatusResolve(pool);
            appPool.status = Enum.GetName(typeof(Status), state == Status.Unknown ? Status.Started : state).ToLower();

            return Created((string)AppPoolHelper.GetLocation(appPool.id), appPool);
        }

        [HttpDelete]
        [Audit(fields: AUDIT_FIELDS, maskedFields: MASKED_FIELDS)]
        public void Delete(string id)
        {
            string name = AppPoolId.CreateFromUuid(id).Name;

            ApplicationPool pool = AppPoolHelper.GetAppPool(name);

            if (pool != null) {
                AppPoolHelper.DeleteAppPool(pool);
                ManagementUnit.Current.Commit();
            }

            // Sucess
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }


        [HttpPatch]
        [Audit(fields: AUDIT_FIELDS, maskedFields: MASKED_FIELDS)]
        [ResourceInfo(Name = Defines.AppPoolName)]
        public async Task<object> Patch(string id, [FromBody] dynamic model)
        {
            // Cut off the notion of uuid from beginning of request
            string name = AppPoolId.CreateFromUuid(id).Name;

            // Set settings
            ApplicationPool appPool = AppPoolHelper.UpdateAppPool(name, model);
            if(appPool == null) {
                return NotFound();
            }

            if (model.identity != null && !await IsAppPoolIdentityAllowed(appPool)) {
                return new ForbidResult();
            }

            // Start/Stop
            if (model.status != null) {
                Status status = DynamicHelper.To<Status>(model.status);
                try {
                    switch (status) {
                        case Status.Stopped:
                            appPool.Stop();
                            break;
                        case Status.Started:
                            appPool.Start();
                            break;
                        case Status.Recycling:
                            appPool.Recycle();
                            break;
                    }
                }
                catch(COMException e) {

                    // If pool is fresh and status is still unknown then COMException will be thrown when manipulating status
                    throw new ApiException("Error setting application pool status", e);
                }
            }

            // Update changes
            ManagementUnit.Current.Commit();

            // Refresh data
            appPool = ManagementUnit.ServerManager.ApplicationPools[appPool.Name];

            //
            // Create response
            dynamic pool = AppPoolHelper.ToJsonModel(appPool, Context.Request.GetFields());

            // The Id could change by changing apppool name
            if (pool.id != id) {
                return LocationChanged(AppPoolHelper.GetLocation(pool.id), pool);
            }

            return pool;
        }


        private Status WaitForPoolStatusResolve(ApplicationPool pool)
        {
            // Delay to get proper status of newly created pool
            int n = 10;
            for (int i = 0; i < n; i++) {
                try {
                    return StatusExtensions.FromObjectState(pool.State);
                }
                catch (COMException) {
                    if (i < n - 1) {
                        Thread.Sleep(10 / n);
                    }
                }
            }

            return Status.Unknown;
        }

        private async Task<bool> IsAppPoolIdentityAllowed(ApplicationPool pool) {
            if (pool.ProcessModel.IdentityType != ProcessModelIdentityType.LocalSystem) {
                return true;
            }

            return (await _authorization.AuthorizeAsync(Context.User, null, "system")).Succeeded;
        }
    }
}
