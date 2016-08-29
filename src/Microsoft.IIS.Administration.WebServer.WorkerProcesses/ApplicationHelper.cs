// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using System;
    using System.Collections.Generic;
    using AppPools;
    using Core.Utils;
    using Applications;
    using Web.Administration;



    public static class ApplicationHelper {
        private static readonly Fields RefFields = new Fields("name", "id", "process_id");

        public static IEnumerable<ApplicationInfo> GetApplications(WorkerProcess wp) {
            if (wp == null) {
                throw new ArgumentNullException(nameof(wp));
            }

            List<ApplicationInfo> result = new List<ApplicationInfo>();

            //
            // Get AppPool
            var appPool = AppPoolHelper.GetAppPool(wp.AppPoolName);

            if (appPool == null) {
                return result;
            }

            //
            // Find all apps in the app pool
            var apps = Applications.ApplicationHelper.GetApplications(appPool);

            //
            // Match Site Id and App Path
            foreach (var app in apps) {
                foreach (var ad in wp.ApplicationDomains) {
                    if (ad.Id.Contains($"/{app.Site.Id}/")) {
                        string appPath = app.Application.Path;
                        string adPath = ad.VirtualPath;

                        if (appPath.Equals(adPath, StringComparison.OrdinalIgnoreCase) ||
                            appPath.Equals(adPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) {
                            result.Add(app);
                            break;
                        }
                    }
                }
            }

            return result;
        }

    }
}
