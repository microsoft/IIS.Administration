namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "url-rewrite";

        // Feature resource
        public const string UrlRewriteName = "Microsoft.WebServer.UrlRewrite";
        public static ResDef Resource = new ResDef("url_rewrite", new Guid("BBB27846-37A0-4651-94DC-04A01B53C6B5"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "url_rewrite.id";
    }
}
