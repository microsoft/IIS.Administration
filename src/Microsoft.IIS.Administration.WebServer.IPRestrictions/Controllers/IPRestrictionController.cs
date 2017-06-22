// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Applications;
    using AspNetCore.Mvc;
    using Web.Administration;
    using Sites;
    using System.Net;
    using Core.Http;
    using Core;
    using System.Threading.Tasks;


    [RequireWebServer]
    public class IPRestrictionController : ApiBaseController
    {
        private const string DISPLAY_NAME = "IP and Domain Restrictions";

        [HttpGet]
        [ResourceInfo(Name = Defines.IpRestrictionsName)]
        [RequireGlobalModule(IPRestrictionsHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = IPRestrictionsHelper.ToJsonModel(site, path);
            return LocationChanged(IPRestrictionsHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.IpRestrictionsName)]
        [RequireGlobalModule(IPRestrictionsHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            IPRestrictionId ipId = new IPRestrictionId(id);

            Site site = ipId.SiteId == null ? null : SiteHelper.GetSite(ipId.SiteId.Value);

            return IPRestrictionsHelper.ToJsonModel(site, ipId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.IpRestrictionsName)]
        [RequireGlobalModule(IPRestrictionsHelper.MODULE, DISPLAY_NAME)]
        public object Patch([FromBody] dynamic model, string id)
        {
            IPRestrictionId ipId = new IPRestrictionId(id);

            Site site = ipId.SiteId == null ? null : SiteHelper.GetSite(ipId.SiteId.Value);

            if (ipId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            IPRestrictionsHelper.SetFeatureSettings(model, site, ipId.Path, configPath);

            ManagementUnit.Current.Commit();

            return IPRestrictionsHelper.ToJsonModel(site, ipId.Path);
        }

        [HttpPost]
        [Audit]
        public async Task<object> Post()
        {
            if (IPRestrictionsHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(IPRestrictionsHelper.FEATURE_NAME);
            }

            await IPRestrictionsHelper.SetFeatureEnabled(true);

            dynamic settings = IPRestrictionsHelper.ToJsonModel(null, null);
            return Created(IPRestrictionsHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            IPRestrictionId ipId = new IPRestrictionId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (ipId.SiteId != null) ? SiteHelper.GetSite(ipId.SiteId.Value) : null;

            if (site != null) {
                IPRestrictionsHelper.GetSection(site, ipId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

                if (ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName)) {
                    IPRestrictionsHelper.GetDynamicSecuritySection(site, ipId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
                }

                ManagementUnit.Current.Commit();
            }

            if (ipId.SiteId == null && IPRestrictionsHelper.IsFeatureEnabled()) {
                await IPRestrictionsHelper.SetFeatureEnabled(false);
            }
        }
    }
}
