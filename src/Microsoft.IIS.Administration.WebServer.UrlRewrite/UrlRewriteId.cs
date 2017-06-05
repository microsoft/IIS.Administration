namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;

    public class UrlRewriteId
    {
        private const string PURPOSE = "WebServer.UrlRewrite";
        private const char DELIMITER = '\n';

        private const uint PATH_INDEX = 0;
        private const uint SITE_ID_INDEX = 1;
        private const uint IS_LOCAL_INDEX = 2;

        public bool IsLocal { get; private set; }
        public string Path { get; private set; }
        public long? SiteId { get; private set; }
        public string Uuid { get; private set; }

        public UrlRewriteId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            string[] info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);
            string path = info[PATH_INDEX];
            string siteId = info[SITE_ID_INDEX];
            string isLocal = info[IS_LOCAL_INDEX];

            if (!string.IsNullOrEmpty(path)) {

                if (string.IsNullOrEmpty(siteId)) {
                    throw new ArgumentNullException("siteId");
                }

                this.Path = path;
                this.SiteId = long.Parse(siteId);
            }
            else if (!string.IsNullOrEmpty(siteId)) {

                this.SiteId = long.Parse(siteId);
            }

            this.IsLocal = int.Parse(isLocal) == 1;
            this.Uuid = uuid;
        }

        public UrlRewriteId(long? siteId, string path, bool isLocal)
        {

            if (!string.IsNullOrEmpty(path) && siteId == null) {
                throw new ArgumentNullException("siteId");
            }

            this.Path = path;
            this.SiteId = siteId;
            this.IsLocal = isLocal;

            string encodableSiteId = this.SiteId == null ? "" : this.SiteId.Value.ToString();
            string encodableIsLocal = (this.IsLocal ? 1 : 0).ToString();

            this.Uuid = Core.Utils.Uuid.Encode($"{this.Path}{DELIMITER}{encodableSiteId}{DELIMITER}{encodableIsLocal}", PURPOSE);
        }
    }
}
