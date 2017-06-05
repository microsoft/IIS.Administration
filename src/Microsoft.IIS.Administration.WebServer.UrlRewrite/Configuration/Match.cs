namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    public class Match : ConfigurationElement
    {
        public string Url {
            get {
                return ((string)(base["url"]));
            }
            set {
                base["url"] = value;
            }
        }
    }
}
