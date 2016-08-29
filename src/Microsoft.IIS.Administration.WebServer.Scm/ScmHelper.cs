// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Scm
{
    using System;
    using System.ServiceProcess;

    public class ScmHelper
    {
        public const string SERVICE_NAME = "W3SVC";

        public static bool IsInstalled()
        {
            using (var svc = GetService()) {
                return svc != null;
            }
        }

        public static Status GetStatus()
        {
            ServiceControllerStatus status;

            if (IsInstalled())
            {
                using (ServiceController svcccontroller = new ServiceController(SERVICE_NAME))
                {
                    status = svcccontroller.Status;
                }
            }
            else
            {
                return Status.Unknown;
            }

            return FromServiceControllerStatus(status);
        }

        public static Status Start()
        {

            using (ServiceController svcController = new ServiceController(SERVICE_NAME))
            {
                if (svcController.Status != ServiceControllerStatus.Running)
                {
                    svcController.Start();
                }
            }
            return GetStatus();
        }

        public static Status Stop()
        {
            using (ServiceController svcController = new ServiceController(SERVICE_NAME))
            {
                if (svcController.Status != ServiceControllerStatus.Stopped)
                {
                    svcController.Stop();
                }
            }
            return GetStatus();
        }

        public static object ToJsonModel()
        {
            if(!IsInstalled())
            {
                throw new Exception("IIS not installed");
            }

            using (ServiceController svcController = new ServiceController(SERVICE_NAME))
            {
                var status = FromServiceControllerStatus(svcController.Status);

                var obj = new
                {
                    name = svcController.DisplayName,
                    id = ScmId.CreateFromServiceName(SERVICE_NAME).Uuid,
                    service_name = svcController.ServiceName,
                    status = Enum.GetName(typeof(Status), status).ToLower()
                };

                return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
            }
        }



        private static Status FromServiceControllerStatus(ServiceControllerStatus svcStatus)
        {
            switch (svcStatus)
            {
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
