// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
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


    [RequireWebServer]
    public class RedirectHeadersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.RedirectHeadersName)]
        public object Get()
        {
            string uuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(uuid)) {
                return NotFound();
            }

            HttpResponseHeadersId id = new HttpResponseHeadersId(uuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<NameValueConfigurationElement> headers = RedirectHeadersHelper.GetRedirectHeaders(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(headers.Count());

            return new {
                redirect_headers = headers.Select(h => RedirectHeadersHelper.ToJsonModelRef(h, site, id.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RedirectHeaderName)]
        public object Get(string id)
        {
            RedirectHeaderId headerId = new RedirectHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return NotFound();
            }

            NameValueConfigurationElement header = RedirectHeadersHelper.GetRedirectHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header == null) {
                return NotFound();
            }

            return RedirectHeadersHelper.ToJsonModel(header, site, headerId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.RedirectHeaderName)]
        public object Post([FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.http_response_headers == null) {
                throw new ApiArgumentException("http_response_headers");
            }
            if (!(model.http_response_headers is JObject)) {
                throw new ApiArgumentException("http_response_headers");
            }

            string uuid = DynamicHelper.Value(model.http_response_headers.id);
            if (uuid == null) {
                throw new ApiArgumentException("http_response_headers.id");
            }

            HttpResponseHeadersId id = new HttpResponseHeadersId(uuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            HttpProtocolSection section = HttpResponseHeadersHelper.GetSection(site, id.Path, configPath);

            NameValueConfigurationElement header = RedirectHeadersHelper.Create(model, section);

            RedirectHeadersHelper.Add(header, section);

            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic h = RedirectHeadersHelper.ToJsonModel(header, site, id.Path);

            return Created(RedirectHeadersHelper.GetLocation(h.id), h);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.RedirectHeaderName)]
        public object Patch(string id, dynamic model)
        {
            RedirectHeaderId headerId = new RedirectHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return NotFound();
            }

            NameValueConfigurationElement header = RedirectHeadersHelper.GetRedirectHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header == null) {
                return NotFound();
            }

            var configScope = ManagementUnit.ResolveConfigScope(model);
            var section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, configScope);

            RedirectHeadersHelper.Update(header, model, section);

            ManagementUnit.Current.Commit();

            //
            // Create Response
            dynamic redirectHeader = RedirectHeadersHelper.ToJsonModel(header, site, headerId.Path);

            if (redirectHeader.id != id) {
                return LocationChanged(RedirectHeadersHelper.GetLocation(redirectHeader.id), redirectHeader);
            }

            return redirectHeader;
        }


        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var headerId = new RedirectHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            NameValueConfigurationElement header = RedirectHeadersHelper.GetRedirectHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header != null) {

                var section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, ManagementUnit.ResolveConfigScope());

                RedirectHeadersHelper.Delete(header, section);
                ManagementUnit.Current.Commit();

            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
