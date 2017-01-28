// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using AppPools;
    using Core.Utils;
    using Web.Administration;



    public static class WorkerProcessHelper {
        private static readonly Fields RefFields = new Fields("name", "id", "process_id");

        public static WorkerProcess GetWorkerProcess(int processId) {
            return ManagementUnit.ServerManager.WorkerProcesses.Where(wp => wp.ProcessId == processId).FirstOrDefault();
        }

        public static IEnumerable<WorkerProcess> GetWorkerProcesses(ApplicationPool pool) {
            if (pool == null) {
                throw new ArgumentNullException(nameof(pool));
            }

            return ManagementUnit.ServerManager.WorkerProcesses.Where(wp => wp.AppPoolName.Equals(pool.Name, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<WorkerProcess> GetWorkerProcesses() {
            return ManagementUnit.ServerManager.WorkerProcesses;
        }

        public static void Kill(WorkerProcess wp) {
            if (wp == null) {
                throw new ArgumentNullException(nameof(wp));
            }

            Process.GetProcessById(wp.ProcessId).Kill();
        }

        public static object ToJsonModelRef(WorkerProcess wp, Fields fields = null) {
            if (fields == null || !fields.HasFields) {
                return WpToJsonModel(wp, RefFields, false);
            }
            else {
                return WpToJsonModel(wp, fields, false);
            }
        }

        internal static object WpToJsonModel(WorkerProcess wp, Fields fields = null, bool full = true) {
            if (wp == null) {
                throw new ArgumentNullException(nameof(wp));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            Process p = Process.GetProcessById(wp.ProcessId);
            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = p.ProcessName;
            }

            //
            // id
            obj.id = new WorkerProcessId(p.Id, Guid.Parse(wp.ProcessGuid)).Uuid;

            //
            // status
            if (fields.Exists("status")) {
                obj.status = Enum.GetName(typeof(WorkerProcessState), wp.State).ToLower();
            }

            //
            // process_id
            if (fields.Exists("process_id")) {
                obj.process_id = p.Id;
            }

            //
            // process_guid
            if (fields.Exists("process_guid")) {
                obj.process_guid = wp.ProcessGuid;
            }

            //
            // start_time
            if (fields.Exists("start_time")) {
                obj.start_time = p.StartTime;
            }

            //
            // working_set
            if (fields.Exists("working_set")) {
                obj.working_set = p.WorkingSet64;
            }

            //
            // peak_working_set
            if (fields.Exists("peak_working_set")) {
                obj.peak_working_set = p.PeakWorkingSet64;
            }

            //
            // private_memory_size
            if (fields.Exists("private_memory_size")) {
                obj.private_memory_size = p.PrivateMemorySize64;
            }

            //
            // virtual_memory_size
            if (fields.Exists("virtual_memory_size")) {
                obj.virtual_memory_size = p.VirtualMemorySize64;
            }

            //
            // peak_virtual_memory_size
            if (fields.Exists("peak_virtual_memory_size")) {
                obj.peak_virtual_memory_size = p.PeakVirtualMemorySize64;
            }

            //
            // total_processor_time
            if (fields.Exists("total_processor_time")) {
                obj.total_processor_time = p.TotalProcessorTime;
            }

            //
            // application_pool
            if (fields.Exists("application_pool")) {
                ApplicationPool pool = AppPoolHelper.GetAppPool(wp.AppPoolName);
                obj.application_pool = AppPoolHelper.ToJsonModelRef(pool);
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }
    }
}
