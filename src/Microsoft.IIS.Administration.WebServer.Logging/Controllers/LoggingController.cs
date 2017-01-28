// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Files;
    using Sites;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule("HttpLoggingModule", "IIS Logging Tools")]
    [RequireGlobalModule("CustomLoggingModule", "IIS Logging Tools")]
    public class LoggingController : ApiBaseController
    {
        private IFileProvider _fileProvider;

        public LoggingController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.LoggingName)]
        public object Get()
        {
            Site site = SiteHelper.ResolveSite();
            string path = SiteHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = LoggingHelper.ToJsonModel(site, path);
            return LocationChanged(LoggingHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.LoggingName)]
        public object Get(string id)
        {
            LoggingId logId = new LoggingId(id);

            Site site = logId.SiteId == null ? null : SiteHelper.GetSite(logId.SiteId.Value);

            if (logId.SiteId != null && site == null) {
                return NotFound();
            }

            return LoggingHelper.ToJsonModel(site, logId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.LoggingName)]
        public object Patch(string id, dynamic model)
        {
            LoggingId logId = new LoggingId(id);

            Site site = logId.SiteId == null ? null : SiteHelper.GetSite(logId.SiteId.Value);

            if (logId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Check for config_scope
            string configScope = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            LoggingHelper.Update(model, _fileProvider, site, logId.Path, configScope);

            ManagementUnit.Current.Commit();

            return LoggingHelper.ToJsonModel(site, logId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            LoggingId logId = new LoggingId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (logId.SiteId != null) ? SiteHelper.GetSite(logId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            var section = LoggingHelper.GetHttpLoggingSection(site, logId.Path, ManagementUnit.ResolveConfigScope());

            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
