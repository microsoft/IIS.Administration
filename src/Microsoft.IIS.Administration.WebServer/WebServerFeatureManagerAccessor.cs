// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;

    public static class WebServerFeatureManagerAccessor
    {
        private static IWebServerFeatureManager _instance;

        internal static IServiceProvider Services { get; set; }

        public static IWebServerFeatureManager Instance {
            get {
                if (_instance != null) {
                    return _instance;
                }

                _instance = (IWebServerFeatureManager)Services.GetService(typeof(IWebServerFeatureManager));
                return _instance;
            }
        }
    }
}
