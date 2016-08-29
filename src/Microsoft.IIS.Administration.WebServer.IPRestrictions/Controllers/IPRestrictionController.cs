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

    [RequireGlobalModule("IpRestrictionModule", "IP and Domain Restrictions")]
    public class IPRestrictionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.IpRestrictionsName)]
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
        public object Get(string id)
        {
            IPRestrictionId ipId = new IPRestrictionId(id);

            Site site = ipId.SiteId == null ? null : SiteHelper.GetSite(ipId.SiteId.Value);

            return IPRestrictionsHelper.ToJsonModel(site, ipId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.IpRestrictionsName)]
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

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            IPRestrictionId ipId = new IPRestrictionId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (ipId.SiteId != null) ? SiteHelper.GetSite(ipId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            IPRestrictionsHelper.GetSection(site, ipId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            if (ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName)) {
                IPRestrictionsHelper.GetDynamicSecuritySection(site, ipId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
            }

            ManagementUnit.Current.Commit();
        }
    }
}
