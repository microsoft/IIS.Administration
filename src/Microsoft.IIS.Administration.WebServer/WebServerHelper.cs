// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Threading.Tasks;

namespace Microsoft.IIS.Administration.WebServer
{
    static class WebServerHelper {
        private const string FEATURE_ROLE = "IIS-WebServerRole";
        private const string FEATURE = "IIS-WebServer";

        public static object WebServerJsonModel() {
            var obj = new {
                id = WebServerId.Create().Uuid
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static async Task Install() {
            await SetFeatureEnabled(true);
        }

        public static async Task Uninstall() {
            await SetFeatureEnabled(false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.PATH}/{id}";
        }


        private static async Task SetFeatureEnabled(bool enabled) {
            IWebServerFeatureManager featureManager = WebServerFeatureManagerAccessor.Instance;

            if (featureManager == null) {
                throw new ArgumentNullException("IWebServerFeatureManager");
            }

            if (enabled) {
                await featureManager.Enable(FEATURE_ROLE, FEATURE);
            }
            else {
                await featureManager.Disable(FEATURE_ROLE);
            }
        }
    }
}
