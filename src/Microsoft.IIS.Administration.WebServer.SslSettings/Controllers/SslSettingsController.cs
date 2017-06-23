// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.SslSettings
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.Net;
    using Web.Administration;


    [RequireWebServer]
    public class SslSettingsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.SslSettingsName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            // Ssl settings cannot be configured at server level
            if(site == null) {
                return NotFound();
            }

            dynamic d = SslSettingsHelper.ToJsonModel(site, path);
            return LocationChanged(SslSettingsHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.SslSettingsName)]
        public object Get(string id)
        {
            SslSettingId settingId = new SslSettingId(id);

            // Ssl settings cannot be configured at server level
            if (settingId.SiteId == null) {
                return NotFound();
            }

            Site site = SiteHelper.GetSite(settingId.SiteId.Value);

            return SslSettingsHelper.ToJsonModel(site, settingId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.SslSettingsName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            SslSettingId settingsId = new SslSettingId(id);

            Site site = settingsId.SiteId == null ? null : SiteHelper.GetSite(settingsId.SiteId.Value);

            // Targetting section for a site, but unable to find that site
            if (settingsId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            SslSettingsHelper.UpdateSettings(model, site, settingsId.Path, configPath);

            ManagementUnit.Current.Commit();

            return SslSettingsHelper.ToJsonModel(site, settingsId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            SslSettingId settingId = new SslSettingId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (settingId.SiteId != null) ? SiteHelper.GetSite(settingId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            SslSettingsHelper.GetAccessSection(site, settingId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
