// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Core.Utils;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System.Dynamic;

    class SiteHelper
    {
        public static object ToJsonModel(IWebSiteSnapshot snapshot, Site site, Fields fields = null, bool full = true)
        {
            if (snapshot == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // id
            obj.id = new SiteId(site.Id).Uuid;

            //
            // uptime
            if (fields.Exists("uptime")) {
                obj.uptime = snapshot.Uptime;
            }

            //
            // network
            if (fields.Exists("network")) {
                obj.network = new {
                    bytes_sent_sec = snapshot.BytesSentSec,
                    bytes_recv_sec = snapshot.BytesRecvSec,
                    connection_attempts_sec = snapshot.ConnectionAttemptsSec,
                    total_connection_attempts = snapshot.TotalConnectionAttempts,
                    current_connections = snapshot.CurrentConnections,
                };
            }

            //
            // requests
            if (fields.Exists("requests")) {
                obj.requests = new {
                    per_sec = snapshot.TotalRequestsSec,
                    total = snapshot.TotalRequests,
                };
            }

            //
            // requests
            if (fields.Exists("requests")) {
                obj.requests = new {
                    active = snapshot.ActiveRequests,
                    per_sec = snapshot.RequestsSec,
                    total = snapshot.TotalRequests,
                    percent_500_sec = snapshot.Percent500
                };
            }

            //
            // memory
            if (fields.Exists("memory")) {
                obj.memory = new {
                    private_working_set = snapshot.PrivateWorkingSet,
                    working_set = snapshot.WorkingSet,
                    private_bytes = snapshot.PrivateBytes
                };
            }

            //
            // cpu
            if (fields.Exists("cpu")) {
                obj.cpu = new {
                    percent_usage = snapshot.PercentCpuTime,
                    threads = snapshot.ThreadCount
                };
            }

            //
            // disk
            if (fields.Exists("disk")) {
                obj.disk = new {
                    io_write_operations_sec = snapshot.IOWriteSec,
                    io_read_operations_sec = snapshot.IOReadSec,
                    page_faults_sec = snapshot.PageFaultsSec
                };
            }

            //
            // cache
            if (fields.Exists("cache")) {
                obj.cache = new {
                    file_cache_count = snapshot.CurrentFilesCached,
                    file_cache_memory_usage = snapshot.FileCacheMemoryUsage,
                    file_cache_hits = snapshot.FileCacheHits,
                    file_cache_misses = snapshot.FileCacheMisses,
                    total_files_cached = snapshot.TotalFilesCached,
                    output_cache_count = snapshot.OutputCacheCurrentItems,
                    output_cache_memory_usage = snapshot.OutputCacheCurrentMemoryUsage,
                    output_cache_hits = snapshot.OutputCacheTotalHits,
                    output_cache_misses = snapshot.OutputCacheTotalMisses,
                    uri_cache_count = snapshot.CurrentUrisCached,
                    uri_cache_hits = snapshot.UriCacheHits,
                    uri_cache_misses = snapshot.UriCacheMisses,
                    total_uris_cached = snapshot.TotalUrisCached
                };
            }


            //
            // website
            if (fields.Exists("website")) {
                obj.website = Sites.SiteHelper.ToJsonModelRef(site);
            }

            return Core.Environment.Hal.Apply(Defines.WebSiteMonitoringResource.Guid, obj);
        }
    }
}
