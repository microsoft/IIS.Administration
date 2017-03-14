// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class UrlsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.UrlsName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            List<UrlRule> urls = UrlsHelper.GetUrls(site, reqId.Path);

            return new {
                urls = urls.Select(s => UrlsHelper.ToJsonModelRef(s, site, reqId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.UrlName)]
        public object Get(string id)
        {
            UrlId urlId = new UrlId(id);

            Site site = urlId.SiteId == null ? null : SiteHelper.GetSite(urlId.SiteId.Value);

            if (urlId.SiteId != null && site == null) {
                return NotFound();
            }

            UrlRule url = UrlsHelper.GetUrls(site, urlId.Path).FirstOrDefault(u => u.Url.Equals(urlId.Url, StringComparison.OrdinalIgnoreCase));

            if (url == null) {
                return NotFound();
            }

            return UrlsHelper.ToJsonModel(url, site, urlId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.UrlName)]
        public object Post([FromBody] dynamic model)
        {            
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.request_filtering == null) {
                throw new ApiArgumentException("request_filtering");
            }
            if (!(model.request_filtering is JObject)) {
                throw new ApiArgumentException(String.Empty, "request_filtering");
            }
            string reqUuid = DynamicHelper.Value(model.request_filtering.id);
            if (reqUuid == null) {
                throw new ApiArgumentException("request_filtering.id");
            }

            // Get the feature id
            RequestFilteringId reqId = new RequestFilteringId(reqUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            UrlRule url = UrlsHelper.CreateUrl(model, section);

            UrlsHelper.AddUrl(url, section);

            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic u = UrlsHelper.ToJsonModel(url, site, reqId.Path);

            return Created(UrlsHelper.GetLocation(u.id), u);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.UrlName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            UrlId urlId = new UrlId(id);

            Site site = urlId.SiteId == null ? null : SiteHelper.GetSite(urlId.SiteId.Value);

            if (urlId.SiteId != null && site == null) {
                return NotFound();
            }

            UrlRule url = UrlsHelper.GetUrls(site, urlId.Path).FirstOrDefault(u => u.Url.Equals(urlId.Url, StringComparison.OrdinalIgnoreCase));

            if (url == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            UrlsHelper.UpdateUrl(url, model, site, urlId.Path, configPath);

            ManagementUnit.Current.Commit();

            dynamic urlModel = UrlsHelper.ToJsonModel(url, site, urlId.Path);

            if(urlModel.id != id) {
                return LocationChanged(UrlsHelper.GetLocation(urlModel.id), urlModel);
            }

            return urlModel;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            UrlId urlId = new UrlId(id);

            Site site = urlId.SiteId == null ? null : SiteHelper.GetSite(urlId.SiteId.Value);

            if (urlId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            UrlRule url = UrlsHelper.GetUrls(site, urlId.Path).Where(u => u.Url.ToString().Equals(urlId.Url)).FirstOrDefault();

            if (url != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, urlId.Path, ManagementUnit.ResolveConfigScope());

                UrlsHelper.DeleteUrl(url, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }
    }
}
