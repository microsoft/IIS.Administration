// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
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
            ConfigureAnonymousAuthentication();
            ConfigureBasicAuthentication();
            ConfigureDigestAuthentication();
            ConfigureWindowsAuthentication();
            ConfigureAuthentication();
        }

        private void ConfigureAuthentication()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;
            
            //
            // Route
            router.MapWebApiRoute(Defines.AuthenticationResource.Guid, $"{Defines.AUTHENTICATION_PATH}/{{id}}", new { controller = "authentication" });
            
            //
            // Hal
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.AuthenticationResource.Name, _ => {
                var id = new AuthenticationId(null, null);
                return new { href = $"/{Defines.AUTHENTICATION_PATH}/{id.Uuid}" };
            });

            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.AuthenticationResource.Name, site => {
                var siteId = new SiteId((string)site.id);
                var id = new AuthenticationId(siteId.Id, "/");
                return new { href = $"/{Defines.AUTHENTICATION_PATH}/{id.Uuid}" }; 
            });

            hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.AuthenticationResource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                var id = new AuthenticationId(appId.SiteId, appId.Path);
                return new { href = $"/{Defines.AUTHENTICATION_PATH}/{id.Uuid}" }; 
            });
        }


        private void ConfigureAnonymousAuthentication()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.AnonAuthResource.Guid, $"{Defines.ANON_AUTH_PATH}/{{id?}}", new { controller = "anonauth" });

            hal.ProvideLink(Defines.AnonAuthResource.Guid, "self", anonAuth => new { href = $"/{Defines.ANON_AUTH_PATH}/{anonAuth.id}" });

            hal.ProvideLink(Defines.AuthenticationResource.Guid, Defines.AnonAuthResource.Name, auth => {
                var authId = new AuthenticationId((string)auth.id);
                Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);
                var anonAuthId = new AnonAuthId(authId.SiteId, authId.Path, AnonymousAuthenticationHelper.IsSectionLocal(site, authId.Path));
                return new { href = $"/{Defines.ANON_AUTH_PATH}/{anonAuthId.Uuid}" };
            });
        }

        private void ConfigureBasicAuthentication()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.BasicAuthResource.Guid, $"{Defines.BASIC_AUTH_PATH}/{{id?}}", new { controller = "BasicAuth" });

            hal.ProvideLink(Defines.BasicAuthResource.Guid, "self", basicAuth => new { href = $"/{Defines.BASIC_AUTH_PATH}/{basicAuth.id}" });

            hal.ProvideLink(Defines.AuthenticationResource.Guid, Defines.BasicAuthResource.Name, auth => {
                var authId = new AuthenticationId((string)auth.id);
                Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);
                var basicAuthId = new BasicAuthId(authId.SiteId, authId.Path, BasicAuthenticationHelper.IsSectionLocal(site, authId.Path));
                return new { href = $"/{Defines.BASIC_AUTH_PATH}/{basicAuthId.Uuid}" };
            });
        }

        private void ConfigureDigestAuthentication()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.DigestAuthResource.Guid, $"{Defines.DIGEST_AUTH_PATH}/{{id?}}", new { controller = "DigestAuth" });

            hal.ProvideLink(Defines.DigestAuthResource.Guid, "self", digestAuth => new { href = $"/{Defines.DIGEST_AUTH_PATH}/{digestAuth.id}" });

            hal.ProvideLink(Defines.AuthenticationResource.Guid, Defines.DigestAuthResource.Name, auth => {
                var authId = new AuthenticationId((string)auth.id);
                Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);
                var digestAuthId = new DigestAuthId(authId.SiteId, authId.Path, DigestAuthenticationHelper.IsSectionLocal(site, authId.Path));
                return new { href = $"/{Defines.DIGEST_AUTH_PATH}/{digestAuthId.Uuid}" };
            });
        }

        private void ConfigureWindowsAuthentication()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.WinAuthResource.Guid, $"{Defines.WIN_AUTH_PATH}/{{id?}}", new { controller = "winauth" });

            hal.ProvideLink(Defines.WinAuthResource.Guid, "self", winAuth => new { href = $"/{Defines.WIN_AUTH_PATH}/{winAuth.id}" });

            hal.ProvideLink(Defines.AuthenticationResource.Guid, Defines.WinAuthResource.Name, auth => {
                var authId = new AuthenticationId((string)auth.id);
                Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);
                var winAuthId = new WinAuthId(authId.SiteId, authId.Path, WindowsAuthenticationHelper.IsSectionLocal(site, authId.Path));
                return new { href = $"/{Defines.WIN_AUTH_PATH}/{winAuthId.Uuid}" };
            });
        }
    }
}
