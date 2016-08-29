// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Scm
{
    public class ScmId
    {
        private const string PURPOSE = "WebServer.Scm";

        public string ServiceName { get; private set; }

        public string Uuid { get; private set; }

        private ScmId() { }

        public static ScmId CreateFromServiceName(string serviceName)
        {
            string uuid = Core.Utils.Uuid.Encode(serviceName ?? string.Empty, PURPOSE);
            return new ScmId()
            {
                ServiceName = serviceName,
                Uuid = uuid
            };
        }

        public static ScmId CreateFromUuid(string uuid)
        {
            string serviceName = Core.Utils.Uuid.Decode(uuid, PURPOSE);
            return new ScmId()
            {
                Uuid = uuid,
                ServiceName = string.IsNullOrEmpty(serviceName) ? null : serviceName
            };
        }
    }
}
