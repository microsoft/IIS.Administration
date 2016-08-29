// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using System;
    using System.Collections.Generic;
    using AppPools;
    using Web.Administration;


    public static class SitesHelper {

        public static IEnumerable<Site> GetSites(WorkerProcess wp) {
            if (wp == null) {
                throw new ArgumentNullException(nameof(wp));
            }

            List<Site> result = new List<Site>();

            //
            // Get AppPool
            var appPool = AppPoolHelper.GetAppPool(wp.AppPoolName);

            if (appPool == null) {
                return result;
            }

            //
            // Find all sites in the app pool
            var sites = Sites.SiteHelper.GetSites(appPool);

            //
            // Match Site Id
            foreach (var s in sites) {
                foreach (var ad in wp.ApplicationDomains) {

                    if (ad.Id.Contains($"/{s.Id}/")) {
                        result.Add(s);
                        break;
                    }
                }
            }

            return result;
        }
    }
}
