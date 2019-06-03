// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests {
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;

    public class Configuration {
        private static Configuration _instance;

        public static Configuration Instance()
        {
            return _instance ?? (_instance = new Configuration());
        }

        private JObject _config;

        public Configuration()
        {
            var content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test.config.json"));
            _config = JObject.Parse(content);
        }

        public string TEST_SERVER {
            get {
                return _config.Value<string>("test_server");
            }
        }

        public string TEST_PORT {
            get {
                return _config.Value<string>("test_port");
            }
        }

        public string TEST_SERVER_URL {
            get {
                return $"{TEST_SERVER}:{TEST_PORT}";
            }
        }

        public string TEST_ROOT_PATH {
            get
            {
                var val = _config.Value<string>("test_root_path");

                if (string.IsNullOrEmpty(val))
                {
                    val = AppContext.BaseDirectory;
                }

                return Environment.ExpandEnvironmentVariables(val);
            }
        }

        public string PROJECT_PATH
        {
            get
            {
                var val = _config.Value<string>("project_path");

                if (string.IsNullOrEmpty(val))
                {
                    val = AppContext.BaseDirectory;
                }

                return Environment.ExpandEnvironmentVariables(val);
            }
        }

        public string CCSUser
        {
            get
            {
                return _config.Value<string>("ccs_user") ?? "IisAdminCcsTestR";
            }
        }
    }
}
