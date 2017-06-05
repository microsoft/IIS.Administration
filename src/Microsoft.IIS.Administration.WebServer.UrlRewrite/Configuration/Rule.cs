namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    public class Rule : ConfigurationElement
    {

        private AppliesToCollection _appliesTo;

        private DenyStringCollection _denyStrings;

        private ScanHeaderCollection _scanHeaders;

        public Rule()
        {
        }

        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

        public bool ScanAllRaw {
            get {
                return ((bool)(base["scanAllRaw"]));
            }
            set {
                base["scanAllRaw"] = value;
            }
        }

        public bool ScanQueryString {
            get {
                return ((bool)(base["scanQueryString"]));
            }
            set {
                base["scanQueryString"] = value;
            }
        }

        public bool ScanUrl {
            get {
                return ((bool)(base["scanUrl"]));
            }
            set {
                base["scanUrl"] = value;
            }
        }

        public AppliesToCollection AppliesTo {
            get {
                if ((this._appliesTo == null)) {
                    this._appliesTo = ((AppliesToCollection)(base.GetCollection("appliesTo", typeof(AppliesToCollection))));
                }
                return this._appliesTo;
            }
        }

        public DenyStringCollection DenyStrings {
            get {
                if ((this._denyStrings == null)) {
                    this._denyStrings = ((DenyStringCollection)(base.GetCollection("denyStrings", typeof(DenyStringCollection))));
                }
                return this._denyStrings;
            }
        }

        public ScanHeaderCollection ScanHeaders {
            get {
                if ((this._scanHeaders == null)) {
                    this._scanHeaders = ((ScanHeaderCollection)(base.GetCollection("scanHeaders", typeof(ScanHeaderCollection))));
                }
                return this._scanHeaders;
            }
        }
    }
}
