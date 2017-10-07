// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Info
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.ServiceProcess;

    class InfoHelper
    {
        private const string SERVICE_NAME = "W3SVC";
        private static readonly string VERSION_TEST_DLL = Environment.ExpandEnvironmentVariables(@"%windir%\System32\inetsrv\w3dt.dll");

        public static Status GetStatus()
        {
            ServiceControllerStatus status;

            using (var service = GetService()) {
                if (service != null) {
                    status = service.Status;
                }
                else {
                    return Status.Unknown;
                }
            }

            return FromServiceControllerStatus(status);
        }

        public static string GetVersion()
        {
            string version = null;

            if (File.Exists(VERSION_TEST_DLL)) {
                var info = FileVersionInfo.GetVersionInfo(VERSION_TEST_DLL);
                version = info.ProductVersion;
            }
            else {
                version = "Unknown";
            }

            return version;
        }

        public static object ToJsonModel(IWebServerVersion versionProvider)
        {
            Version version = versionProvider.Version;

            var obj = new {
                name = "Microsoft Internet Information Services",
                id = WebServerId.Create().Uuid,
                supports_sni = version != null && version >= new Version(8, 0),
                status = Enum.GetName(typeof(Status), GetStatus()).ToLower(),
                version = GetVersion()
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }



        private static Status FromServiceControllerStatus(ServiceControllerStatus svcStatus)
        {
            switch (svcStatus) {
                case ServiceControllerStatus.StopPending:
                case ServiceControllerStatus.PausePending:
                    return Status.Stopping;
                case ServiceControllerStatus.Stopped:
                case ServiceControllerStatus.Paused:
                    return Status.Stopped;
                case ServiceControllerStatus.StartPending:
                case ServiceControllerStatus.ContinuePending:
                    return Status.Starting;
                case ServiceControllerStatus.Running:
                    return Status.Started;
                default:
                    return Status.Unknown;
            }
        }

        private static ServiceController GetService()
        {
            var services = ServiceController.GetServices();
            ServiceController target = null;

            foreach (var service in services) {
                if (!service.ServiceName.Equals(SERVICE_NAME)) {
                    service.Dispose();
                }
                else {
                    target = service;
                }
            }
            return target;
        }
    }
}
