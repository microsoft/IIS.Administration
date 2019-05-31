// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using WebServer;
    using Web.Administration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Core.Utils;

    public class ApplicationPools
    {
        public const string TEST_APP_POOL_NAME = "test_app_pool";
        public static readonly string TEST_APP_POOL = $"{{\"name\": \"{TEST_APP_POOL_NAME}\"}}";

        public static readonly string APP_POOLS_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/application-pools";

        [Fact]
        public void CreateAndCleanup()
        {
            using (HttpClient client = ApiHttpClient.Create())
            {
                EnsureNoPool(client, TEST_APP_POOL_NAME);

                string id;
                Assert.True(CreateAppPool(client, TEST_APP_POOL, out id));

                string testAppPoolUri = $"{APP_POOLS_URL}/{id}";

                Assert.True(AppPoolExists(client, testAppPoolUri));

                Assert.True(DeleteAppPool(client, testAppPoolUri));
            }
        }

        [Fact]
        public void GetPools()
        {
            using (HttpClient client = ApiHttpClient.Create())
            {
                string res = null;
                int runs = 10;
                for (int i = 0; i < runs; i++)
                {
                    Assert.True(client.Get(APP_POOLS_URL, out res));
                }
            }
        }

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create())
            {

                EnsureNoPool(client, TEST_APP_POOL_NAME);

                string id;
                Assert.True(CreateAppPool(client, TEST_APP_POOL, out id));

                JObject pool = GetAppPool(client, TEST_APP_POOL_NAME);
                JObject cachedPool = new JObject(pool);

                WaitForStatus(client, pool);

                pool["auto_start"] = !pool.Value<bool>("auto_start");
                pool["enable_32bit_win64"] = !pool.Value<bool>("enable_32bit_win64");
                pool["queue_length"] = pool.Value<long>("queue_length") + 1;
                pool["managed_runtime_version"] = "v2.0";
                pool["status"] = Enum.GetName(typeof(Status),
                                             DynamicHelper.To<Status>(pool["status"]) ==
                                             Status.Stopped ? Status.Started :
                                             Status.Stopped);
                pool["pipeline_mode"] = Enum.GetName(typeof(ManagedPipelineMode),
                                                             DynamicHelper.To<ManagedPipelineMode>(pool["pipeline_mode"]) ==
                                                             ManagedPipelineMode.Integrated ? ManagedPipelineMode.Classic :
                                                             ManagedPipelineMode.Integrated);

                JObject cpu = pool.Value<JObject>("cpu");
                cpu["limit"] = cpu.Value<long>("limit") + 1;
                cpu["limit_interval"] = cpu.Value<long>("limit_interval") + 1;
                cpu["action"] = Enum.GetName(typeof(ProcessorAction),
                                                             DynamicHelper.To<ProcessorAction>(cpu["action"]) ==
                                                             ProcessorAction.NoAction ? ProcessorAction.KillW3wp :
                                                             ProcessorAction.NoAction);

                JObject pModel = pool.Value<JObject>("process_model");
                pModel["idle_timeout"] = pModel.Value<long>("idle_timeout") + 1;
                pModel["max_processes"] = pModel.Value<long>("max_processes") + 1;
                pModel["ping_interval"] = pModel.Value<long>("ping_interval") + 1;
                pModel["ping_response_time"] = pModel.Value<long>("ping_response_time") + 1;
                pModel["shutdown_time_limit"] = pModel.Value<long>("shutdown_time_limit") + 1;
                pModel["startup_time_limit"] = pModel.Value<long>("startup_time_limit") + 1;
                pModel["pinging_enabled"] = !pModel.Value<bool>("pinging_enabled");
                pModel["idle_timeout_action"] = Enum.GetName(typeof(IdleTimeoutAction),
                                                             DynamicHelper.To<IdleTimeoutAction>(pModel["idle_timeout_action"]) ==
                                                             IdleTimeoutAction.Terminate ? IdleTimeoutAction.Suspend :
                                                             IdleTimeoutAction.Terminate);

                JObject recycling = (JObject)pool["recycling"];
                recycling["disable_overlapped_recycle"] = !recycling.Value<bool>("disable_overlapped_recycle");
                recycling["disable_recycle_on_config_change"] = !recycling.Value<bool>("disable_recycle_on_config_change");

                JObject logEvents = (JObject)pool["recycling"]["log_events"];
                logEvents["time"] = !logEvents.Value<bool>("time");
                logEvents["requests"] = !logEvents.Value<bool>("requests");
                logEvents["schedule"] = !logEvents.Value<bool>("schedule");
                logEvents["memory"] = !logEvents.Value<bool>("memory");
                logEvents["isapi_unhealthy"] = !logEvents.Value<bool>("isapi_unhealthy");
                logEvents["on_demand"] = !logEvents.Value<bool>("on_demand");
                logEvents["config_change"] = !logEvents.Value<bool>("config_change");
                logEvents["private_memory"] = !logEvents.Value<bool>("private_memory");

                JObject pRestart = (JObject)pool["recycling"]["periodic_restart"];
                pRestart["time_interval"] = pRestart.Value<long>("time_interval") + 1;
                pRestart["private_memory"] = pRestart.Value<long>("private_memory") + 1;
                pRestart["request_limit"] = pRestart.Value<long>("request_limit") + 1;
                pRestart["virtual_memory"] = pRestart.Value<long>("virtual_memory") + 1;

                JArray schedule = (JArray)pRestart["schedule"];
                schedule.Add(new JValue(TimeSpan.FromHours(20).ToString(@"hh\:mm")));

                JObject rfp = pool.Value<JObject>("rapid_fail_protection");
                rfp["interval"] = rfp.Value<long>("interval") + 1;
                rfp["max_crashes"] = rfp.Value<long>("max_crashes") + 1;
                rfp["enabled"] = !rfp.Value<bool>("enabled");
                rfp["auto_shutdown_exe"] = "test.exe";
                rfp["auto_shutdown_params"] = "testparams";
                rfp["load_balancer_capabilities"] = Enum.GetName(typeof(LoadBalancerCapabilities),
                                                             DynamicHelper.To<LoadBalancerCapabilities>(rfp["idle_timeout_action"]) ==
                                                             LoadBalancerCapabilities.HttpLevel ? LoadBalancerCapabilities.TcpLevel :
                                                             LoadBalancerCapabilities.HttpLevel);

                JObject processOrphaning = pool.Value<JObject>("process_orphaning");
                processOrphaning["enabled"] = !processOrphaning.Value<bool>("enabled");
                processOrphaning["orphan_action_exe"] = "test.exe";
                processOrphaning["orphan_action_params"] = "testparams";

                string result;
                string body = JsonConvert.SerializeObject(pool);

                Assert.True(client.Patch(Utils.Self(pool), body, out result));

                JObject newPool = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(pool, newPool, "auto_start"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "enable_32bit_win64"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "queue_length"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "pipeline_mode", StringComparison.OrdinalIgnoreCase));
                Assert.True(Utils.JEquals<string>(pool, newPool, "managed_runtime_version"));

                Assert.True(Utils.JEquals<long>(pool, newPool, "cpu.limit"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "cpu.limit_interval"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "cpu.processor_affinity_enabled"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "cpu.action", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.idle_timeout"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.max_processes"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.ping_interval"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.ping_response_time"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.shutdown_time_limit"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "process_model.startup_time_limit"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "process_model.pinging_enabled"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "process_model.idle_timeout_action", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.disable_overlapped_recycle"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.disable_recycle_on_config_change"));

                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.time"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.requests"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.schedule"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.memory"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.isapi_unhealthy"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.on_demand"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.config_change"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "recycling.log_events.private_memory"));

                Assert.True(Utils.JEquals<long>(pool, newPool, "recycling.periodic_restart.time_interval"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "recycling.periodic_restart.private_memory"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "recycling.periodic_restart.request_limit"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "recycling.periodic_restart.virtual_memory"));

                JArray scheduleOld = pool["recycling"]["periodic_restart"].Value<JArray>("schedule");
                JArray scheduleNew = newPool["recycling"]["periodic_restart"].Value<JArray>("schedule");

                for (int i = 0; i < scheduleOld.Count; i++)
                {
                    Assert.True(scheduleOld[i].ToObject<string>().Equals(scheduleNew[i].ToObject<string>()));
                }

                Assert.True(Utils.JEquals<long>(pool, newPool, "rapid_fail_protection.interval"));
                Assert.True(Utils.JEquals<long>(pool, newPool, "rapid_fail_protection.max_crashes"));
                Assert.True(Utils.JEquals<bool>(pool, newPool, "rapid_fail_protection.enabled"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "rapid_fail_protection.auto_shutdown_exe"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "rapid_fail_protection.auto_shutdown_params"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "rapid_fail_protection.load_balancer_capabilities", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<bool>(pool, newPool, "process_orphaning.enabled"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "process_orphaning.orphan_action_exe"));
                Assert.True(Utils.JEquals<string>(pool, newPool, "process_orphaning.orphan_action_params"));

                EnsureNoPool(client, TEST_APP_POOL_NAME);
            }
        }

        public static JObject CreateAppPool(HttpClient client, string name)
        {
            var pool = new {
                name = name
            };

            return client.Post(APP_POOLS_URL, pool);
        }

        public static JObject GetAppPool(HttpClient client, string name)
        {
            var pool =  client.Get(APP_POOLS_URL)["app_pools"]
                        .ToObject<IEnumerable<JObject>>()
                        .FirstOrDefault(p => p.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase));

            return pool == null ? null : Utils.FollowLink(client, pool, "self");
        }

        public static void EnsureNoPool(HttpClient client, string name)
        {
            JObject pool = GetAppPool(client, name);

            if (pool == null) {
                return;
            }

            client.Delete(Utils.Self(pool));
        }

        private static bool CreateAppPool(HttpClient client, string testPool, out string id)
        {
            id = null;

            HttpContent content = new StringContent(TEST_APP_POOL, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(APP_POOLS_URL, content).Result;

            if (!Globals.Success(response)) {
                return false;
            }

            dynamic appPool = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

            id = DynamicHelper.Value(appPool.id);

            return true;
        }

        public static bool DeleteAppPool(HttpClient client, string poolUri)
        {
            if (!AppPoolExists(client, poolUri)) { throw new Exception("Can't delete test site because it doesn't exist."); }
            HttpResponseMessage response = client.DeleteAsync(poolUri).Result;
            return Globals.Success(response);
        }

        private static bool AppPoolExists(HttpClient client, string poolUri)
        {
            HttpResponseMessage responseMessage = client.GetAsync(poolUri).Result;
            return Globals.Success(responseMessage);
        }

        public static bool GetAppPools(HttpClient client, out List<JObject> pools)
        {
            string response = null;
            pools = null;

            if (!client.Get(APP_POOLS_URL, out response)) {
                return false;
            }

            JObject jObj = JsonConvert.DeserializeObject<JObject>(response);

            JArray poolsArr = jObj["app_pools"] as JArray;
            pools = new List<JObject>();

            foreach (JObject pool in poolsArr) {
                pools.Add(pool);
            }

            return true;
        }

        private void WaitForStatus(HttpClient client, JObject pool)
        {
            string res;
            int refreshCount = 0;
            while (pool.Value<string>("status") == "unknown") {
                refreshCount++;
                if (refreshCount > 100) {
                    throw new Exception();
                }

                client.Get(Utils.Self(pool), out res);
                pool = JsonConvert.DeserializeObject<JObject>(res);
            }
        }
    }
}
