// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.WebServer.AppPools;
    using Microsoft.Web.Administration;

    class AppPoolHelper
    {
        public static object ToJsonModel(IAppPoolSnapshot snapshot, ApplicationPool pool)
        {
            var obj = new {
                id = AppPoolId.CreateFromName(pool.Name).Uuid,
                requests = new {
                    active = snapshot.ActiveRequests,
                    per_sec = snapshot.RequestsSec,
                    total = snapshot.TotalRequests,
                    percent_500_sec = snapshot.Percent500
                },
                memory = new {
                    private_working_set = snapshot.PrivateWorkingSet,
                    working_set = snapshot.WorkingSet,
                    private_bytes = snapshot.PrivateBytes
                },
                cpu = new {
                    percent_usage = snapshot.PercentCpuTime,
                    threads = snapshot.ThreadCount,
                    processes = snapshot.ProcessCount
                },
                disk = new {
                    io_write_operations_sec = snapshot.IOWriteSec,
                    io_read_operations_sec = snapshot.IOReadSec,
                    page_faults_sec = snapshot.PageFaultsSec
                },
                cache = new {
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
                },
                application_pool = AppPools.AppPoolHelper.ToJsonModelRef(pool)
            };

            return Core.Environment.Hal.Apply(Defines.AppPoolMonitoringResource.Guid, obj);
        }
    }
}
