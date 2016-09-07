// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor {
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using WorkerProcesses;
    using Core.Utils;
    using Web.Administration;
    


    public static class RequestHelper {
        private static readonly Fields RefFields = new Fields("url", "id", "time_elapsed");
        private const int DEFAULT_TIME_ELASPSED = 1000; //ms

        private static readonly RequestComparer _RequestComparer = new RequestComparer();

        public static Request GetRequest(WorkerProcess wp, string requestId) {
            return GetRequests(wp).Where(r => r.RequestId == requestId).FirstOrDefault();
        }

        public static IEnumerable<Request> GetRequests(Filter filter = null) {
            var result = new List<Request>();

            foreach (var wp in ManagementUnit.ServerManager.WorkerProcesses) {
                result.AddRange(GetRequests(wp, filter));
            }

            return result;
        }

        public static IEnumerable<Request> GetRequests(WorkerProcess wp, Filter filter = null) {
            if (wp == null) {
                throw new ArgumentNullException(nameof(wp));
            }

            int timeElapsed = (int?)filter?.Get<uint>("time_elapsed") ?? DEFAULT_TIME_ELASPSED;

            return wp.GetRequests(timeElapsed).Distinct(_RequestComparer);
        }

        public static IEnumerable<Request> GetRequests(Site site, Filter filter = null) {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            var result = new List<Request>();

            foreach (var wp in WorkerProcessHelper.GetWorkerProcesses(site)) {
                foreach (var r in GetRequests(wp, filter)) {
                    if (r.SiteId == site.Id) {
                        result.Add(r);
                    }
                }
            }

            return result;
        }

        public static object ToJsonModelRef(Request request, Fields fields = null) {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(request, RefFields, false);
            }
            else {
                return ToJsonModel(request, fields, false);
            }
        }

        internal static object ToJsonModel(Request request, Fields fields = null, bool full = true) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // url
            if (fields.Exists("url")) {
                obj.url = request.Url;
            }

            //
            // id
            obj.id = new RequestId(request.ProcessId, request.RequestId).Uuid;

            //
            // method
            if (fields.Exists("method")) {
                obj.method = request.Verb;
            }

            //
            // host_name
            if (fields.Exists("host_name")) {
                obj.host_name = request.HostName;
            }

            //
            // client_ip
            if (fields.Exists("client_ip_address")) {
                obj.client_ip_address = request.ClientIPAddr;
            }

            //
            // local_ip_address
            if (fields.Exists("local_ip_address")) {
                obj.local_ip_address = request.LocalIPAddress;
            }

            //
            // local_port
            if (fields.Exists("local_port")) {
                obj.local_port = request.LocalPort;
            }

            //
            // request_id
            if (fields.Exists("request_id")) {
                obj.request_id = request.RequestId;
            }

            //
            // connection_id
            if (fields.Exists("connection_id")) {
                obj.connection_id = request.ConnectionId;
            }

            //
            // pipeline_state
            if (fields.Exists("pipeline_state")) {
                obj.pipeline_state = Enum.GetName(typeof(PipelineState), request.PipelineState);
            }

            //
            // current_module
            if (fields.Exists("current_module")) {
                obj.current_module = request.CurrentModule;
            }

            //
            // time_elapsed
            if (fields.Exists("time_elapsed")) {
                obj.time_elapsed = request.TimeElapsed;
            }

            //
            // time_in_module
            if (fields.Exists("time_in_module")) {
                obj.time_in_module = request.TimeInModule;
            }

            //
            // time_in_state
            if (fields.Exists("time_in_state")) {
                obj.time_in_state = request.TimeInState;
            }

            //
            // worker_process
            if (fields.Exists("worker_process")) {
                var wp = WorkerProcessHelper.GetWorkerProcess(request.ProcessId);
                if (wp != null) {
                    obj.worker_process = WorkerProcessHelper.ToJsonModelRef(wp);
                }
            }

            //
            // website
            if (fields.Exists("website")) {
                var site = Sites.SiteHelper.GetSite(request.SiteId);
                if (site != null) {
                    obj.website = Sites.SiteHelper.ToJsonModelRef(site);
                }
            }

            return Core.Environment.Hal.Apply(Defines.RequestsResource.Guid, obj, full);
        }


        private class RequestComparer : IEqualityComparer<Request>
        {
            public bool Equals(Request x, Request y)
            {
                return object.ReferenceEquals(x, y) || x.RequestId.Equals(y.RequestId);
            }

            public int GetHashCode(Request obj)
            {
                return obj.RequestId.GetHashCode();
            }
        }
    }
}
