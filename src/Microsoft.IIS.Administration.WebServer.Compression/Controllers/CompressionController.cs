// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Files;
    using Sites;
    using System.Net;
    using System.Threading.Tasks;
    using Web.Administration;


    [RequireWebServer]
    public class CompressionController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Compression";
        private IFileProvider _fileProvider;

        public CompressionController(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CompressionName)]
        [RequireGlobalModule(CompressionHelper.STATIC_MODULE, DISPLAY_NAME)]
        [RequireGlobalModule(CompressionHelper.DYNAMIC_MODULE, DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = CompressionHelper.ToJsonModel(site, path);
            return LocationChanged(CompressionHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CompressionName)]
        [RequireGlobalModule(CompressionHelper.STATIC_MODULE, DISPLAY_NAME)]
        [RequireGlobalModule(CompressionHelper.DYNAMIC_MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            CompressionId compId = new CompressionId(id);

            Site site = compId.SiteId == null ? null : SiteHelper.GetSite(compId.SiteId.Value);

            return CompressionHelper.ToJsonModel(site, compId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.CompressionName)]
        [RequireGlobalModule(CompressionHelper.STATIC_MODULE, DISPLAY_NAME)]
        [RequireGlobalModule(CompressionHelper.DYNAMIC_MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            CompressionId compId = new CompressionId(id);

            Site site = compId.SiteId == null ? null : SiteHelper.GetSite(compId.SiteId.Value);

            if (compId.SiteId != null && site == null) {
                // Targetting section for a site, but unable to find that site
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            CompressionHelper.UpdateSettings(model, _fileProvider, site, compId.Path, configPath);

            ManagementUnit.Current.Commit();          

            return CompressionHelper.ToJsonModel(site, compId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.CompressionName)]
        public async Task<object> Post()
        {
            if (CompressionHelper.IsStaticEnabled() && CompressionHelper.IsDynamicEnabled()) {
                throw new AlreadyExistsException(DISPLAY_NAME);
            }

            await CompressionHelper.SetFeatureEnabled(true);

            dynamic compression = CompressionHelper.ToJsonModel(null, null);
            return Created(CompressionHelper.GetLocation(compression.id), compression);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            CompressionId compId = new CompressionId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (compId.SiteId != null) ? SiteHelper.GetSite(compId.SiteId.Value) : null;

            if (site != null) {
                // Http compression section is not reverted because it only allows definition in apphost
                CompressionHelper.GetUrlCompressionSection(site, compId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (compId.SiteId == null && (CompressionHelper.IsStaticEnabled() || CompressionHelper.IsDynamicEnabled())) {
                await CompressionHelper.SetFeatureEnabled(false);
            }
        }
    }
}
