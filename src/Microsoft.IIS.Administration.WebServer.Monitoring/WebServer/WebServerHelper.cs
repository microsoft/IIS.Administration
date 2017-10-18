// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.IIS.Administration.Core.Utils;
using System.Dynamic;

namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    class WebServerHelper
    {
        public static object ToJsonModel(IWebServerSnapshot snapshot, Fields fields = null, bool full = true)
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
            obj.id = WebServerId.Create().Uuid;

            //
            // network
            if (fields.Exists("network")) {
                obj.network = new {
                    bytes_sent_sec = snapshot.BytesSentSec,
                    bytes_recv_sec = snapshot.BytesRecvSec,
                    connection_attempts_sec = snapshot.ConnectionAttemptsSec,
                    total_bytes_sent = snapshot.TotalBytesSent,
                    total_bytes_recv = snapshot.TotalBytesRecv,
                    total_connection_attempts = snapshot.TotalConnectionAttempts,
                    current_connections = snapshot.CurrentConnections
                };
            }

            //
            // requests
            if (fields.Exists("requests")) {
                obj.requests = new {
                    active = snapshot.ActiveRequests,
                    per_sec = snapshot.RequestsSec,
                    total = snapshot.TotalRequests
                };
            }

            //
            // memory
            if (fields.Exists("memory")) {
                obj.memory = new {
                    handles = snapshot.HandleCount,
                    private_bytes = snapshot.PrivateBytes,
                    private_working_set = snapshot.PrivateWorkingSet,
                    system_in_use = snapshot.SystemMemoryInUse,
                    installed = snapshot.TotalInstalledMemory
                };
            }

            //
            // cpu
            if (fields.Exists("cpu")) {
                obj.cpu = new {
                    threads = snapshot.ThreadCount,
                    processes = snapshot.ProcessCount,
                    percent_usage = snapshot.PercentCpuTime,
                    system_percent_usage = snapshot.SystemPercentCpuTime
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
                    output_cache_memory_usage = snapshot.OutputCacheMemoryUsage,
                    output_cache_hits = snapshot.OutputCacheTotalHits,
                    output_cache_misses = snapshot.OutputCacheTotalMisses,
                    uri_cache_count = snapshot.CurrentUrisCached,
                    uri_cache_hits = snapshot.UriCacheHits,
                    uri_cache_misses = snapshot.UriCacheMisses,
                    total_uris_cached = snapshot.TotalUrisCached
                };
            }

            return Core.Environment.Hal.Apply(Defines.WebServerMonitoringResource.Guid, obj);
        }
    }
}
