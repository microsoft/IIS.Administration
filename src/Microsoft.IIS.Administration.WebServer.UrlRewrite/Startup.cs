namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
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
            ConfigureUrlRewrite();
        }

        private void ConfigureUrlRewrite()
        {
            // MVC routing
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "UrlRewrite" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", rf => new { href = FeatureHelper.GetLocation(rf.id) });


            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetRequestFilteringId(null, null);
                return new { href = FeatureHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetRequestFilteringId(s, "/");
                return new { href = FeatureHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetRequestFilteringId(s, appId.Path);
                return new { href = FeatureHelper.GetLocation(id.Uuid) };
            });
        }

        private UrlRewriteId GetRequestFilteringId(Site s, string path)
        {
            return new UrlRewriteId(s?.Id, path, FeatureHelper.IsSectionLocal(s, path));
        }
    }
}
