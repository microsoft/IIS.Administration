// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "static-content";
        private const string MIME_MAPS_ENDPOINT = "mime-maps";

        public const string StaticContentName = "Microsoft.WebServer.StaticContent";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("static_content", new Guid("EE556F7B-6A10-4166-97BF-DC3AEFED2BE8"), ENDPOINT);
        internal const string IDENTIFIER = "static_content.id";

        public const string MimeMapsName = "Microsoft.WebServer.StaticContent.MimeMaps";
        public const string MimeMapName = "Microsoft.WebServer.StaticContent.MimeMap";
        public static readonly string MIME_MAPS_PATH = $"{PATH}/{MIME_MAPS_ENDPOINT}";
        public static readonly ResDef MimeMapsResource = new ResDef("mime_maps", new Guid("C7C2AA9E-D64C-4318-B8A1-D309367EC6E3"), MIME_MAPS_ENDPOINT);
    }
}
