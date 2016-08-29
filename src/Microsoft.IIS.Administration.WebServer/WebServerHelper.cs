// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    public class WebServerHelper
    {
        public static object WebServerJsonModel()
        {
            var obj = new {
                id = WebServerId.CreateFromPath(ManagementUnit.Current.ApplicationHostConfigPath).Uuid
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

    }
}
