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
    public class CustomHeadersController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.CustomHeadersName)]
        public object Get()
        {
            string uuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(uuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            HttpResponseHeadersId id = new HttpResponseHeadersId(uuid);
            
            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<NameValueConfigurationElement> headers = CustomHeadersHelper.GetCustomHeaders(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(headers.Count());

            return new {
                custom_headers = headers.Select(h => CustomHeadersHelper.ToJsonModelRef(h, site, id.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CustomHeaderName)]
        public object Get(string id)
        {
            CustomHeaderId headerId = new CustomHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return NotFound();
            }

            NameValueConfigurationElement header = CustomHeadersHelper.GetCustomHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header == null) {
                return NotFound();
            }

            return CustomHeadersHelper.ToJsonModel(header, site, headerId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.CustomHeaderName)]
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
            
            NameValueConfigurationElement header = CustomHeadersHelper.Create(model, section);
            
            CustomHeadersHelper.Add(header, section);
            
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic ch = CustomHeadersHelper.ToJsonModel(header, site, id.Path);

            return Created(CustomHeadersHelper.GetLocation(ch.id), ch);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.CustomHeaderName)]
        public object Patch(string id, dynamic model)
        {
            CustomHeaderId headerId = new CustomHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return NotFound();
            }

            NameValueConfigurationElement header = CustomHeadersHelper.GetCustomHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header == null) {
                return NotFound();
            }

            var configScope = ManagementUnit.ResolveConfigScope(model);
            var section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, configScope);

            CustomHeadersHelper.Update(header, model, section);

            ManagementUnit.Current.Commit();

            dynamic ch = CustomHeadersHelper.ToJsonModel(header, site, headerId.Path);

            if (ch.id != id) {
                return LocationChanged(CustomHeadersHelper.GetLocation(ch.id), ch);
            }

            return ch;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var headerId = new CustomHeaderId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            NameValueConfigurationElement header = CustomHeadersHelper.GetCustomHeaders(site, headerId.Path).FirstOrDefault(h => h.Name.Equals(headerId.Name, StringComparison.OrdinalIgnoreCase));

            if (header != null) {

                var section = HttpResponseHeadersHelper.GetSection(site, headerId.Path, ManagementUnit.ResolveConfigScope());

                CustomHeadersHelper.Delete(header, section);
                ManagementUnit.Current.Commit();

            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
