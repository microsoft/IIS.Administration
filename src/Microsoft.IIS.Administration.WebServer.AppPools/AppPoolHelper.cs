// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.AppPools
{
    using Core;
    using Core.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Web.Administration;

    public static class AppPoolHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "status");
        private const string IdleTimeoutActionAttribute = "idleTimeoutAction";

        public static ApplicationPool CreateAppPool(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (String.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }

            if (GetAppPool(name) != null) {
                throw new AlreadyExistsException("name");
            }

            var sm = ManagementUnit.ServerManager;

            ApplicationPool appPool = sm.ApplicationPools.CreateElement();

            SetToDefaults(appPool, sm.ApplicationPoolDefaults);
            SetAppPool(appPool, model);

            return appPool;
        }

        public static ApplicationPool GetAppPool(string name)
        {
            ApplicationPool pool = ManagementUnit.ServerManager.ApplicationPools.Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return pool;
        }

        public static ApplicationPool UpdateAppPool(string name, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            else if (String.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }

            ApplicationPool appPool = GetAppPool(name);

            if (appPool != null) {
                SetAppPool(appPool, model);
            }

            return appPool;
        }

        public static void DeleteAppPool(ApplicationPool pool)
        {
            ManagementUnit.ServerManager.ApplicationPools.Remove(pool);
        }

        internal static object ToJsonModel(ApplicationPool pool, Fields fields = null, bool full = true)
        {
            if (pool == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = pool.Name;
            }

            //
            // id
            obj.id = AppPoolId.CreateFromName(pool.Name).Uuid;

            //
            // status
            if (fields.Exists("status")) {

                // Prepare state
                Status state = Status.Unknown;
                try {
                    state = StatusExtensions.FromObjectState(pool.State);
                }
                catch (COMException) {
                    // Problem getting state of app pool. Possible reasons:
                    // 1. App pool's application pool was deleted.
                    // 2. App pool was just created and the status is not accessible yet.
                }
                obj.status = Enum.GetName(typeof(Status), state).ToLower();
            }

            //
            // auto_start
            if (fields.Exists("auto_start")) {
                obj.auto_start = pool.AutoStart;
            }

            //
            // pipeline_mode
            if (fields.Exists("pipeline_mode")) {
                obj.pipeline_mode = Enum.GetName(typeof(ManagedPipelineMode), pool.ManagedPipelineMode).ToLower();
            }

            //
            // managed_runtime_version
            if (fields.Exists("managed_runtime_version")) {
                obj.managed_runtime_version = pool.ManagedRuntimeVersion;
            }

            //
            // enable_32bit_win64
            if (fields.Exists("enable_32bit_win64")) {
                obj.enable_32bit_win64 = pool.Enable32BitAppOnWin64;
            }

            //
            // queue_length
            if (fields.Exists("queue_length")) {
                obj.queue_length = pool.QueueLength;
            }

            //
            // cpu
            if (fields.Exists("cpu")) {
                obj.cpu = new {
                    limit = pool.Cpu.Limit,
                    limit_interval = pool.Cpu.ResetInterval.TotalMinutes,
                    action = Enum.GetName(typeof(ProcessorAction), pool.Cpu.Action),
                    processor_affinity_enabled = pool.Cpu.SmpAffinitized,
                    processor_affinity_mask32 = "0x" + pool.Cpu.SmpProcessorAffinityMask.ToString("X"),
                    processor_affinity_mask64 = "0x" + pool.Cpu.SmpProcessorAffinityMask2.ToString("X")
                };
            }

            
            //
            // process_model
            if (fields.Exists("process_model")) {
                dynamic processModel = new ExpandoObject();

                processModel.idle_timeout = pool.ProcessModel.IdleTimeout.TotalMinutes;
                processModel.max_processes = pool.ProcessModel.MaxProcesses;
                processModel.pinging_enabled = pool.ProcessModel.PingingEnabled;
                processModel.ping_interval = pool.ProcessModel.PingInterval.TotalSeconds;
                processModel.ping_response_time = pool.ProcessModel.PingResponseTime.TotalSeconds;
                processModel.shutdown_time_limit = pool.ProcessModel.ShutdownTimeLimit.TotalSeconds;
                processModel.startup_time_limit = pool.ProcessModel.StartupTimeLimit.TotalSeconds;

                if (pool.ProcessModel.Schema.HasAttribute(IdleTimeoutActionAttribute)) {
                    processModel.idle_timeout_action = Enum.GetName(typeof(IdleTimeoutAction), pool.ProcessModel.IdleTimeoutAction);
                }

                obj.process_model = processModel;
            }

            //
            // identity
            if (fields.Exists("identity")) {
                obj.identity = new {
                    // Not changing the casing or adding '_' on the identity type enum because they represent identities and therefore spelling and casing are important
                    identity_type = Enum.GetName(typeof(ProcessModelIdentityType), pool.ProcessModel.IdentityType),
                    username = pool.ProcessModel.UserName,
                    load_user_profile = pool.ProcessModel.LoadUserProfile
                };
            }

            //
            // recycling
            if (fields.Exists("recycling")) {
                RecyclingLogEventOnRecycle logEvent = pool.Recycling.LogEventOnRecycle;

                Dictionary<string, bool> logEvents = new Dictionary<string, bool>();
                logEvents.Add("time", logEvent.HasFlag(RecyclingLogEventOnRecycle.Time));
                logEvents.Add("requests", logEvent.HasFlag(RecyclingLogEventOnRecycle.Requests));
                logEvents.Add("schedule", logEvent.HasFlag(RecyclingLogEventOnRecycle.Schedule));
                logEvents.Add("memory", logEvent.HasFlag(RecyclingLogEventOnRecycle.Memory));
                logEvents.Add("isapi_unhealthy", logEvent.HasFlag(RecyclingLogEventOnRecycle.IsapiUnhealthy));
                logEvents.Add("on_demand", logEvent.HasFlag(RecyclingLogEventOnRecycle.OnDemand));
                logEvents.Add("config_change", logEvent.HasFlag(RecyclingLogEventOnRecycle.ConfigChange));
                logEvents.Add("private_memory", logEvent.HasFlag(RecyclingLogEventOnRecycle.PrivateMemory));

                obj.recycling = new {
                    disable_overlapped_recycle = pool.Recycling.DisallowOverlappingRotation,
                    disable_recycle_on_config_change = pool.Recycling.DisallowRotationOnConfigChange,
                    log_events = logEvents,
                    periodic_restart = new {
                        time_interval = pool.Recycling.PeriodicRestart.Time.TotalMinutes,
                        private_memory = pool.Recycling.PeriodicRestart.PrivateMemory,
                        request_limit = pool.Recycling.PeriodicRestart.Requests,
                        virtual_memory = pool.Recycling.PeriodicRestart.Memory,
                        schedule = pool.Recycling.PeriodicRestart.Schedule.Select(s => s.Time.ToString(@"hh\:mm"))
                    }
                };
            }

            //
            // rapid_fail_protection
            if (fields.Exists("rapid_fail_protection")) {
                obj.rapid_fail_protection = new {
                    enabled = pool.Failure.RapidFailProtection,
                    load_balancer_capabilities = Enum.GetName(typeof(LoadBalancerCapabilities), pool.Failure.LoadBalancerCapabilities),
                    interval = pool.Failure.RapidFailProtectionInterval.TotalMinutes,
                    max_crashes = pool.Failure.RapidFailProtectionMaxCrashes,
                    auto_shutdown_exe = pool.Failure.AutoShutdownExe,
                    auto_shutdown_params = pool.Failure.AutoShutdownParams
                };
            }

            //
            // process_orphaning
            if (fields.Exists("process_orphaning")) {
                obj.process_orphaning = new {
                    enabled = pool.Failure.OrphanWorkerProcess,
                    orphan_action_exe = pool.Failure.OrphanActionExe,
                    orphan_action_params = pool.Failure.OrphanActionParams,
                };
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(ApplicationPool pool, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(pool, RefFields, false);
            }
            else {
                return ToJsonModel(pool, fields, false);
            }
        }

        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.PATH}/{id}";
        }


        private static void SetToDefaults(ApplicationPool pool, ApplicationPoolDefaults defaults)
        {
            pool.ManagedPipelineMode = defaults.ManagedPipelineMode;
            pool.ManagedRuntimeVersion = defaults.ManagedRuntimeVersion;
            pool.Enable32BitAppOnWin64 = defaults.Enable32BitAppOnWin64;
            pool.QueueLength = defaults.QueueLength;
            pool.AutoStart = defaults.AutoStart;

            pool.Cpu.Limit = defaults.Cpu.Limit;
            pool.Cpu.ResetInterval = defaults.Cpu.ResetInterval;
            pool.Cpu.Action = defaults.Cpu.Action;
            pool.Cpu.SmpAffinitized = defaults.Cpu.SmpAffinitized;
            pool.Cpu.SmpProcessorAffinityMask = defaults.Cpu.SmpProcessorAffinityMask;
            pool.Cpu.SmpProcessorAffinityMask2 = defaults.Cpu.SmpProcessorAffinityMask2;

            if (pool.ProcessModel.Schema.HasAttribute(IdleTimeoutActionAttribute)) {
                pool.ProcessModel.IdleTimeoutAction = defaults.ProcessModel.IdleTimeoutAction;
            }

            pool.ProcessModel.MaxProcesses = defaults.ProcessModel.MaxProcesses;
            pool.ProcessModel.PingingEnabled = defaults.ProcessModel.PingingEnabled;
            pool.ProcessModel.IdleTimeout = defaults.ProcessModel.IdleTimeout;
            pool.ProcessModel.PingInterval = defaults.ProcessModel.PingInterval;
            pool.ProcessModel.PingResponseTime = defaults.ProcessModel.PingResponseTime;
            pool.ProcessModel.ShutdownTimeLimit = defaults.ProcessModel.ShutdownTimeLimit;
            pool.ProcessModel.StartupTimeLimit = defaults.ProcessModel.StartupTimeLimit;

            pool.Recycling.LogEventOnRecycle = defaults.Recycling.LogEventOnRecycle;
            pool.Recycling.DisallowOverlappingRotation = defaults.Recycling.DisallowOverlappingRotation;
            pool.Recycling.DisallowRotationOnConfigChange = defaults.Recycling.DisallowRotationOnConfigChange;
            pool.Recycling.PeriodicRestart.PrivateMemory = defaults.Recycling.PeriodicRestart.PrivateMemory;
            pool.Recycling.PeriodicRestart.Memory = defaults.Recycling.PeriodicRestart.Memory;
            pool.Recycling.PeriodicRestart.Requests = defaults.Recycling.PeriodicRestart.Requests;
            pool.Recycling.PeriodicRestart.Time = defaults.Recycling.PeriodicRestart.Time;
        }
        
        private static void SetAppPool(ApplicationPool appPool, dynamic model)
        {
            Debug.Assert(appPool != null);
            Debug.Assert((bool)(model != null));

            DynamicHelper.If((object)model.name, v => SetName(appPool, v));

            appPool.ManagedPipelineMode = DynamicHelper.To<ManagedPipelineMode>(model.pipeline_mode) ?? appPool.ManagedPipelineMode;
            appPool.ManagedRuntimeVersion = DynamicHelper.Value(model.managed_runtime_version) ?? appPool.ManagedRuntimeVersion; 
            appPool.Enable32BitAppOnWin64 = DynamicHelper.To<bool>(model.enable_32bit_win64) ?? appPool.Enable32BitAppOnWin64;
            appPool.QueueLength = DynamicHelper.To(model.queue_length, 10, 65535) ?? appPool.QueueLength;
            appPool.AutoStart = DynamicHelper.To<bool>(model.auto_start) ?? appPool.AutoStart;

            // CPU
            if (model.cpu != null) {
                dynamic cpu = model.cpu;

                appPool.Cpu.Limit = DynamicHelper.To(cpu.limit, 0, 100000) ?? appPool.Cpu.Limit;
                appPool.Cpu.SmpAffinitized = DynamicHelper.To<bool>(cpu.processor_affinity_enabled) ?? appPool.Cpu.SmpAffinitized;
                appPool.Cpu.SmpProcessorAffinityMask = DynamicHelper.ToLong(cpu.processor_affinity_mask32, 16, 0, 4294967295) ?? appPool.Cpu.SmpProcessorAffinityMask;
                appPool.Cpu.SmpProcessorAffinityMask2 = DynamicHelper.ToLong(cpu.processor_affinity_mask64, 16, 0, 4294967295) ?? appPool.Cpu.SmpProcessorAffinityMask2;

                try {
                    appPool.Cpu.Action = DynamicHelper.To<ProcessorAction>(cpu.action) ?? appPool.Cpu.Action;
                }
                catch (COMException e) {
                    throw new ApiArgumentException("cpu.action", e);
                }

                long? resetInterval = DynamicHelper.To(cpu.limit_interval, 0, 1440);
                appPool.Cpu.ResetInterval = (resetInterval != null) ? TimeSpan.FromMinutes(resetInterval.Value) : appPool.Cpu.ResetInterval;
            }

            // Process Model
            if (model.process_model != null) {
                dynamic processModel = model.process_model;

                if (appPool.ProcessModel.Schema.HasAttribute(IdleTimeoutActionAttribute)) {
                    appPool.ProcessModel.IdleTimeoutAction = DynamicHelper.To<IdleTimeoutAction>(processModel.idle_timeout_action) ?? appPool.ProcessModel.IdleTimeoutAction;
                }
                appPool.ProcessModel.MaxProcesses = DynamicHelper.To(processModel.max_processes, 0, 2147483647) ?? appPool.ProcessModel.MaxProcesses;
                appPool.ProcessModel.PingingEnabled = DynamicHelper.To<bool>(processModel.pinging_enabled) ?? appPool.ProcessModel.PingingEnabled;

                long? idleTimeout = DynamicHelper.To(processModel.idle_timeout, 0, 43200);
                appPool.ProcessModel.IdleTimeout = (idleTimeout != null) ? TimeSpan.FromMinutes(idleTimeout.Value) : appPool.ProcessModel.IdleTimeout;

                long? pingInterval = DynamicHelper.To(processModel.ping_interval, 1, 4294967);
                appPool.ProcessModel.PingInterval = (pingInterval != null) ? TimeSpan.FromSeconds(pingInterval.Value) : appPool.ProcessModel.PingInterval;

                long? pingResponseTime = DynamicHelper.To(processModel.ping_response_time, 1, 4294967);
                appPool.ProcessModel.PingResponseTime = (pingResponseTime != null) ? TimeSpan.FromSeconds(pingResponseTime.Value) : appPool.ProcessModel.PingResponseTime;

                long? shutDownTimeLimit = DynamicHelper.To(processModel.shutdown_time_limit, 1, 4294967);
                appPool.ProcessModel.ShutdownTimeLimit = (shutDownTimeLimit != null) ? TimeSpan.FromSeconds(shutDownTimeLimit.Value) : appPool.ProcessModel.ShutdownTimeLimit;

                long? startupTimeLimit = DynamicHelper.To(processModel.startup_time_limit, 1, 4294967);
                appPool.ProcessModel.StartupTimeLimit = (startupTimeLimit != null) ? TimeSpan.FromSeconds(startupTimeLimit.Value): appPool.ProcessModel.StartupTimeLimit;
            }

            // Identity
            if (model.identity != null) {
                dynamic identity = model.identity;

                appPool.ProcessModel.IdentityType = DynamicHelper.To<ProcessModelIdentityType>(identity.identity_type) ?? appPool.ProcessModel.IdentityType;
                appPool.ProcessModel.LoadUserProfile = DynamicHelper.To<bool>(identity.load_user_profile) ?? appPool.ProcessModel.LoadUserProfile;
                appPool.ProcessModel.UserName = DynamicHelper.Value(identity.username) ?? appPool.ProcessModel.UserName;
                DynamicHelper.If((object)identity.password, v => appPool.ProcessModel.Password = v);
            }

            // Recycling
            if (model.recycling != null) {
                dynamic recycling = model.recycling;

                appPool.Recycling.DisallowOverlappingRotation = DynamicHelper.To<bool>(recycling.disable_overlapped_recycle) ?? appPool.Recycling.DisallowOverlappingRotation;
                appPool.Recycling.DisallowRotationOnConfigChange = DynamicHelper.To<bool>(recycling.disable_recycle_on_config_change) ?? appPool.Recycling.DisallowRotationOnConfigChange;

                // Check if log event collection provided
                if (recycling.log_events != null) {

                    try {
                        // Convert the log_events dynamic into a string and then deserialize it into a Dictionary<string,bool>, from there we turn it into a flags enum
                        Dictionary<string, bool> logEvents = JsonConvert.DeserializeObject<Dictionary<string, bool>>(recycling.log_events.ToString());

                        var flags = appPool.Recycling.LogEventOnRecycle;

                        if (logEvents == null) {
                            throw new ApiArgumentException("recycling.log_events");
                        }

                        Dictionary<string, RecyclingLogEventOnRecycle> flagPairs = new Dictionary<string, RecyclingLogEventOnRecycle>
                        {
                            { "time", RecyclingLogEventOnRecycle.Time },
                            { "requests", RecyclingLogEventOnRecycle.Requests },
                            { "schedule", RecyclingLogEventOnRecycle.Schedule },
                            { "memory", RecyclingLogEventOnRecycle.Memory },
                            { "isapi_unhealthy", RecyclingLogEventOnRecycle.IsapiUnhealthy },
                            { "on_demand", RecyclingLogEventOnRecycle.OnDemand },
                            { "config_change", RecyclingLogEventOnRecycle.ConfigChange },
                            { "private_memory", RecyclingLogEventOnRecycle.PrivateMemory }
                        };

                        foreach (var key in flagPairs.Keys) {
                            if (logEvents.ContainsKey(key)) {
                                if (logEvents[key]) {
                                    flags |= flagPairs[key];
                                }
                                else {
                                    flags &= ~flagPairs[key];
                                }
                            }
                        }

                        appPool.Recycling.LogEventOnRecycle = flags;
                    }
                    catch(JsonSerializationException e) {
                        throw new ApiArgumentException("recycling.log_events", e);
                    }
                }                
                
                // Periodic Restart
                if (recycling.periodic_restart != null) {
                    dynamic periodicRestart = recycling.periodic_restart;

                    appPool.Recycling.PeriodicRestart.PrivateMemory = DynamicHelper.To(periodicRestart.private_memory, 0, 4294967295) ?? appPool.Recycling.PeriodicRestart.PrivateMemory;
                    appPool.Recycling.PeriodicRestart.Requests = DynamicHelper.To(periodicRestart.request_limit, 0, 4294967295) ?? appPool.Recycling.PeriodicRestart.Requests;
                    appPool.Recycling.PeriodicRestart.Memory = DynamicHelper.To(periodicRestart.virtual_memory, 0, 4294967295) ?? appPool.Recycling.PeriodicRestart.Memory;

                    long? timeInterval = DynamicHelper.To(periodicRestart.time_interval, 0, 432000);
                    appPool.Recycling.PeriodicRestart.Time = timeInterval != null ? TimeSpan.FromMinutes(timeInterval.Value) : appPool.Recycling.PeriodicRestart.Time;


                    // Check if schedule provided
                    if (periodicRestart.schedule != null) {

                        if (!(periodicRestart.schedule is JArray)) {
                            throw new ApiArgumentException("recyclying.periodic_restart.schedule", ApiArgumentException.EXPECTED_ARRAY);
                        }

                        // Clear the old time spans in the schedule
                        appPool.Recycling.PeriodicRestart.Schedule.Clear();
                        IEnumerable<dynamic> schedule = periodicRestart.schedule;

                        // Add the time spans
                        foreach (var d in schedule) {
                            var value = DynamicHelper.Value(d);

                            DateTime dt = default(DateTime);
                            if (value == null || !DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) {
                                throw new ApiArgumentException("recyclying.periodic_restart.schedule.item", "Expected hh:mm");
                            }

                            appPool.Recycling.PeriodicRestart.Schedule.Add(dt.TimeOfDay);
                        }
                    }
                }
            }

            // Rapid Fail Protection
            if (model.rapid_fail_protection != null) {
                var protection = model.rapid_fail_protection;

                appPool.Failure.RapidFailProtection = DynamicHelper.To<bool>(protection.enabled) ?? appPool.Failure.RapidFailProtection;
                appPool.Failure.LoadBalancerCapabilities = DynamicHelper.To<LoadBalancerCapabilities>(protection.load_balancer_capabilities) ?? appPool.Failure.LoadBalancerCapabilities;
                appPool.Failure.RapidFailProtectionMaxCrashes = DynamicHelper.To(protection.max_crashes, 1, 2147483647) ?? appPool.Failure.RapidFailProtectionMaxCrashes;
                appPool.Failure.AutoShutdownExe = DynamicHelper.Value(protection.auto_shutdown_exe) ?? appPool.Failure.AutoShutdownExe;
                appPool.Failure.AutoShutdownParams = DynamicHelper.Value(protection.auto_shutdown_params) ?? appPool.Failure.AutoShutdownParams;

                long? protectionInterval = DynamicHelper.To(protection.interval, 1, 144400);
                appPool.Failure.RapidFailProtectionInterval = (protectionInterval != null) ? TimeSpan.FromMinutes(protectionInterval.Value) : appPool.Failure.RapidFailProtectionInterval;
            }

            // Process Orphaning
            if (model.process_orphaning != null) {
                var orphaning = model.process_orphaning;

                appPool.Failure.OrphanWorkerProcess = DynamicHelper.To<bool>(orphaning.enabled) ?? appPool.Failure.OrphanWorkerProcess;
                appPool.Failure.OrphanActionExe = DynamicHelper.Value(orphaning.orphan_action_exe) ?? appPool.Failure.OrphanActionExe;
                appPool.Failure.OrphanActionParams = DynamicHelper.Value(orphaning.orphan_action_params) ?? appPool.Failure.OrphanActionParams;
            }

        }

        private static void SetName(ApplicationPool pool, string name)
        {
            const string isPresentTag = "isPresent";

            if (ManagementUnit.ServerManager.ApplicationPools.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !p.Name.Equals(pool.Name, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("name");
            }

            if (pool.Name != null) {
                var applications = ManagementUnit.ServerManager.Sites.SelectMany(s => s.Applications);
                bool isDefault = pool.Name.Equals(ManagementUnit.ServerManager.ApplicationDefaults.ApplicationPoolName, StringComparison.OrdinalIgnoreCase);

                if (isDefault) {
                    ManagementUnit.ServerManager.ApplicationDefaults.ApplicationPoolName = name;
                }

                foreach (var app in applications) {
                    if (app.ApplicationPoolName.Equals(pool.Name, StringComparison.OrdinalIgnoreCase) && (!isDefault || (bool)app.GetAttribute("applicationPool").GetMetadata(isPresentTag))) {
                        app.ApplicationPoolName = name;
                    }
                }
            }

            pool.Name = name;
        }
    }
}