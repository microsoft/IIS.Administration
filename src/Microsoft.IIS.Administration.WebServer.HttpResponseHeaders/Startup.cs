// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using Applications;
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Sites;
    using Web.Administration;


    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            ConfigureCustomHeaders();
            ConfigureRedirectHeaders();
            ConfigureHttpResponseHeaders();
        }

        private void ConfigureCustomHeaders()
        {
            // Register all controller routes in mvc framework
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.CustomHeadersResource.Guid, $"{Defines.CUSTOM_HEADERS_PATH}/{{id?}}", new { controller = "customheaders" });

            // Provide self links for all plugin resources
            Environment.Hal.ProvideLink(Defines.CustomHeadersResource.Guid, "self", custHeader => new { href = CustomHeadersHelper.GetLocation(custHeader.id) });

            // Provide link for the custom header sub resource
            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.CustomHeadersResource.Name, custHeader => new { href = $"/{Defines.CUSTOM_HEADERS_PATH}?{Defines.IDENTIFIER}={custHeader.id}" });
        }

        private void ConfigureRedirectHeaders()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RedirectHeadersResource.Guid, $"{Defines.REDIRECT_HEADERS_PATH}/{{id?}}", new { controller = "redirectheaders" });

            Environment.Hal.ProvideLink(Defines.RedirectHeadersResource.Guid, "self", redHeader => new { href = RedirectHeadersHelper.GetLocation(redHeader.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.RedirectHeadersResource.Name, redHeader => new { href = $"/{Defines.REDIRECT_HEADERS_PATH}?{Defines.IDENTIFIER}={redHeader.id}" });
        }

        private void ConfigureHttpResponseHeaders()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{ Defines.PATH}/{{id?}}", new { controller = "HttpResponseHeaders" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", respHeader => new { href = HttpResponseHeadersHelper.GetLocation(respHeader.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetHttpResponseHeadersId(null, null);
                return new { href = HttpResponseHeadersHelper.GetLocation(id.Uuid) };
            });
            
            // Sites
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetHttpResponseHeadersId(s, "/");
                return new { href = HttpResponseHeadersHelper.GetLocation(id.Uuid) };
            });

            // Applications
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetHttpResponseHeadersId(s, appId.Path);
                return new { href = HttpResponseHeadersHelper.GetLocation(id.Uuid) };
            });
        }

        private HttpResponseHeadersId GetHttpResponseHeadersId(Site s, string path)
        {
            return new HttpResponseHeadersId(s?.Id, path, HttpResponseHeadersHelper.IsSectionLocal(s, path));
        }
    }
}
