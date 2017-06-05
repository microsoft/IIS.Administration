namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    static class FeatureHelper
    {
        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }
    }
}
