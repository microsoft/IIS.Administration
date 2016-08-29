// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests {
    using Newtonsoft.Json.Linq;
    using System.IO;

    public static class Configuration {
        private static JObject _config;

        static Configuration() {
            Initialize();
        }

        public static string TEST_SERVER {
            get {
                return _config.Value<string>("test_server");
            }
        }
        public static string TEST_PORT {
            get {
                return _config.Value<string>("test_port");
            }
        }
        public static string TEST_SERVER_URL {
            get {
                return $"{TEST_SERVER}:{TEST_PORT}";
            }
        }
        public static string TEST_ROOT_PATH {
            get {
                return _config.Value<string>("test_root_path");
            }
        }

        private static void Initialize() {
            var content = File.ReadAllText("test.config.json");
            _config = JObject.Parse(content);
        }
    }
}
