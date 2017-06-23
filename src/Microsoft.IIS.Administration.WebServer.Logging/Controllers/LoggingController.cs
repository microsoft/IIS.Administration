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
    using System.Threading.Tasks;
    using Web.Administration;


    [RequireWebServer]
    public class LoggingController : ApiBaseController
    {
        private const string DISPLAY_NAME = "IIS Logging Tools";
        private IFileProvider _fileProvider;

        public LoggingController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.LoggingName)]
        [RequireGlobalModule(LoggingHelper.HTTP_LOGGING_MODULE, DISPLAY_NAME)]
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
        [RequireGlobalModule(LoggingHelper.HTTP_LOGGING_MODULE, DISPLAY_NAME)]
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
        [RequireGlobalModule(LoggingHelper.HTTP_LOGGING_MODULE, DISPLAY_NAME)]
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

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.LoggingName)]
        public async Task<object> Post()
        {
            if (LoggingHelper.IsHttpEnabled() && LoggingHelper.IsCustomEnabled()) {
                throw new AlreadyExistsException(DISPLAY_NAME);
            }

            await LoggingHelper.SetFeatureEnabled(true);

            dynamic settings = LoggingHelper.ToJsonModel(null, null);
            return Created(LoggingHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            LoggingId logId = new LoggingId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (logId.SiteId != null) ? SiteHelper.GetSite(logId.SiteId.Value) : null;

            if (site != null) {
                var section = LoggingHelper.GetHttpLoggingSection(site, logId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (logId.SiteId == null && (LoggingHelper.IsHttpEnabled() || LoggingHelper.IsCustomEnabled())) {
                await LoggingHelper.SetFeatureEnabled(false);
            }
        }
    }
}
