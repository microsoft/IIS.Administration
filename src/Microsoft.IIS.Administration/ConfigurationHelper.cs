// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using Microsoft.Extensions.Configuration;
    using Microsoft.IIS.Administration.Core.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.IO;


    sealed class ConfigurationHelper {
        private string _basePath;
        private string[] _args;

        public ConfigurationHelper(string[] args) {
            _args = args;

            RootPath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null ?
                       Directory.GetCurrentDirectory() : Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            _basePath = Path.Combine(RootPath, "config");

            if (!Directory.Exists(_basePath)) {
                throw new FileNotFoundException($"Configuration path \"{_basePath}\" doesn't exist. Make sure the working directory is correct.", _basePath);
            }
        }

        public string RootPath { get; private set; }

        public IConfiguration Build() {
            //
            // Run transformation before building the configuration system
            TransformAppSettings();

            //
            // Configure builder
            return new ConfigurationBuilder()
                            .SetBasePath(_basePath)
                            .AddJsonFile("appsettings.json")
                            .AddEnvironmentVariables()
                            .AddCommandLine(_args)
                            .Build();
        }

        private void TransformAppSettings() {
            string filePath = Path.Combine(_basePath, "appsettings.json");

            try {
                dynamic configObject = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
                string id = DynamicHelper.Value(configObject.host_id);

                if (string.IsNullOrEmpty(id)) {
                    id = configObject.host_id = Guid.NewGuid().ToString();
                }

                File.WriteAllText(filePath, JsonConvert.SerializeObject(configObject, Formatting.Indented));
            }
            catch (Exception e) {
                Log.Fatal(e, "");
                throw;
            }
        }


    }
}
