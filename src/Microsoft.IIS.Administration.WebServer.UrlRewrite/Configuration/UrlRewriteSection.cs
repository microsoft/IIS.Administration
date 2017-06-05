namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    public class UrlRewriteSection : ConfigurationSection
    {
        private RuleCollection _rules;


        public RuleCollection Rules {
            get {
                if ((this._rules == null)) {
                    this._rules = ((RuleCollection)(base.GetCollection("rules", typeof(RuleCollection))));
                }
                return this._rules;
            }
        }
    }
}
