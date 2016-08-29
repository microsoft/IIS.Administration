// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
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
            ConfigureFileExtensions();
            ConfigureHeaderLimits();
            ConfigureHiddenSegments();
            ConfigureQueryStrings();
            ConfigureRules();
            ConfigureUrls();
            ConfigureRequestFiltering();
        }

        private void ConfigureRequestFiltering()
        {
            // MVC routing
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "requestfiltering" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", rf => new { href = RequestFilteringHelper.GetLocation(rf.id) });


            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetRequestFilteringId(null, null);
                return new { href = RequestFilteringHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetRequestFilteringId(s, "/");
                return new { href = RequestFilteringHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetRequestFilteringId(s, appId.Path);
                return new { href = RequestFilteringHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureFileExtensions()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.FileExtensionsResource.Guid, $"{ Defines.FILE_NAME_EXTENSIONS_PATH}/{{id?}}", new { controller = "filenameextensions" });

            Environment.Hal.ProvideLink(Defines.FileExtensionsResource.Guid, "self", fn => new { href = ExtensionsHelper.GetLocation(fn.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.FileExtensionsResource.Name, rf => new { href = $"/{Defines.FILE_NAME_EXTENSIONS_PATH}?{Defines.IDENTIFIER}={rf.id}" });
        }

        private void ConfigureHeaderLimits()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.HeaderLimitsResource.Guid, $"{ Defines.HEADER_LIMITS_PATH}/{{id?}}", new { controller = "headerlimits" });

            Environment.Hal.ProvideLink(Defines.HeaderLimitsResource.Guid, "self", hl => new { href = HeaderLimitsHelper.GetLocation(hl.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.HeaderLimitsResource.Name, rf => new { href = $"/{Defines.HEADER_LIMITS_PATH}?{Defines.IDENTIFIER}={rf.id}" });
        }

        private void ConfigureHiddenSegments()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.HiddenSegmentsResource.Guid, $"{ Defines.HIDDEN_SEGMENTS_PATH}/{{id?}}", new { controller = "hiddensegments" });

            Environment.Hal.ProvideLink(Defines.HiddenSegmentsResource.Guid, "self", hs => new { href = HiddenSegmentsHelper.GetLocation(hs.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.HiddenSegmentsResource.Name, rf => new { href = $"/{Defines.HIDDEN_SEGMENTS_PATH}?{Defines.IDENTIFIER}={rf.id}" });            
        }

        private void ConfigureQueryStrings()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.QueryStringResource.Guid, $"{ Defines.QUERY_STRING_PATH}/{{id?}}", new { controller = "querystrings" });

            Environment.Hal.ProvideLink(Defines.QueryStringResource.Guid, "self", qs => new { href = QueryStringsHelper.GetLocation(qs.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.QueryStringResource.Name, rf => new { href = $"/{Defines.QUERY_STRING_PATH}?{Defines.IDENTIFIER}={rf.id}" });
        }

        private void ConfigureRules()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RulesResource.Guid, $"{ Defines.RULES_PATH}/{{id?}}", new { controller = "filteringrules" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.RulesResource.Name, rf => new { href = $"/{Defines.RULES_PATH}?{Defines.IDENTIFIER}={rf.id}" });

            Environment.Hal.ProvideLink(Defines.RulesResource.Guid, "self", rule => new { href = RulesHelper.GetLocation(rule.id) });
        }

        private void ConfigureUrls()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.UrlsResource.Guid, $"{ Defines.URLS_PATH}/{{id?}}", new { controller = "urls" });

            Environment.Hal.ProvideLink(Defines.UrlsResource.Guid, "self", url => new { href = UrlsHelper.GetLocation(url.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.UrlsResource.Name, rf => new { href = $"/{Defines.URLS_PATH}?{Defines.IDENTIFIER}={rf.id}" });
        }

        private RequestFilteringId GetRequestFilteringId(Site s, string path)
        {
            return new RequestFilteringId(s?.Id, path, RequestFilteringHelper.IsSectionLocal(s, path));
        }
    }
}
