// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using Extensions.Configuration;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.IO;
    using Utils;

    public static class ConfigurationHelper
    {
        public static IConfiguration Configuration { get; set; }
        public static Guid HostId { get; private set; }

        public static void Initialize(string filePath)
        {
            try {
                dynamic configObject = JsonConvert.DeserializeObject(File.ReadAllText(filePath));
                string id = DynamicHelper.Value(configObject.host_id);

                if (string.IsNullOrEmpty(id)) {
                    id = configObject.host_id = Guid.NewGuid().ToString();
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(configObject, Formatting.Indented));
                }

                HostId = Guid.Parse(id);
            }
            catch (Exception e) {
                Log.Fatal(e, "");
                throw;
            }
        }
    }
}
