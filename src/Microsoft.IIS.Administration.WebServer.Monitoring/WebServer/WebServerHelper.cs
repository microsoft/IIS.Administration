// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    class WebServerHelper
    {
        public static object ToJsonModel(IWebServerSnapshot snapshot)
        {
            var obj = new {
                id = WebServerId.CreateFromPath(ManagementUnit.Current.ApplicationHostConfigPath).Uuid,
                network = new {
                    bytes_sent_sec = snapshot.BytesSentSec,
                    bytes_recv_sec = snapshot.BytesRecvSec,
                    connection_attempts_sec = snapshot.ConnectionAttemptsSec,
                    total_connection_attempts = snapshot.TotalConnectionAttempts
                },
                requests = new {
                    active = snapshot.ActiveRequests,
                    per_sec = snapshot.RequestsSec,
                    total = snapshot.TotalRequests,
                    percent_500_sec = snapshot.Percent500
                },
                memory = new {
                    private_working_set = snapshot.PrivateWorkingSet,
                    working_set = snapshot.WorkingSet,
                    private_bytes = snapshot.PrivateBytes,
                    available = snapshot.AvailableBytes
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
                    output_cache_memory_usage = snapshot.OutputCacheMemoryUsage,
                    output_cache_hits = snapshot.OutputCacheTotalHits,
                    output_cache_misses = snapshot.OutputCacheTotalMisses,
                    uri_cache_count = snapshot.CurrentUrisCached,
                    uri_cache_hits = snapshot.UriCacheHits,
                    uri_cache_misses = snapshot.UriCacheMisses,
                    total_uris_cached = snapshot.TotalUrisCached
                }
            };

            return Core.Environment.Hal.Apply(Defines.WebServerMonitoringResource.Guid, obj);
        }
    }
}
