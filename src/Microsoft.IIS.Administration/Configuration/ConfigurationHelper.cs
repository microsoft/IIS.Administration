// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Utils;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.IO;


    sealed class ConfigurationHelper {
        public const string APPSETTINGS_NAME = "appsettings.json";

        private string _basePath;
        private string[] _args;

        public ConfigurationHelper(string[] args) {
            _args = args;

            RootPath = string.Equals(System.Environment.GetEnvironmentVariable("USE_CURRENT_DIRECTORY_AS_ROOT"), "true", StringComparison.OrdinalIgnoreCase) ?
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
                            .AddJsonFile(APPSETTINGS_NAME, false, true)
                            .AddEnvironmentVariables()
                            .AddCommandLine(_args)
                            .Build();
        }

        private void TransformAppSettings() {
            string filePath = Path.Combine(_basePath, APPSETTINGS_NAME);

            try {
                dynamic configObject = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
                if (string.IsNullOrEmpty(DynamicHelper.Value(configObject["host_id"]))) {
                    configObject["host_id"] = Guid.NewGuid().ToString();
                }
                File.WriteAllText(filePath, JsonConvert.SerializeObject(configObject, Formatting.Indented));
            }
            catch (Exception e) {
                Log.Fatal(e, "");
                throw;
            }
        }


    }

    static class ConfigurationExtensions
    {
        public static void AddConfigurationWriter(this IServiceCollection services, IHostingEnvironment hostingEnvironment)
        {
            var writer = new ConfigurationWriter(hostingEnvironment.GetConfigPath(ConfigurationHelper.APPSETTINGS_NAME));
            services.TryAddSingleton<IConfigurationWriter>(writer);
        }
    }
}
