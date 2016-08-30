// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using Extensions.Configuration;
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using Utils;

    public static class ConfigurationHelper
    {
        public static Guid HostId{ get; private set;}

        public static IConfiguration Config { get; set; }

        public static void Initialize(string filePath)
        {
            dynamic configObject;

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs)) {
                configObject = JsonConvert.DeserializeObject(sr.ReadToEnd());
            }

            string id = DynamicHelper.Value(configObject.host_id);


            if (string.IsNullOrEmpty(id)) {
                Guid guid = Guid.NewGuid();
                configObject.host_id = guid.ToString();

                using (FileStream fs = new FileStream(filePath, FileMode.Truncate))
                using (StreamWriter sw = new StreamWriter(fs)) {
                    sw.Write(JsonConvert.SerializeObject(configObject, Formatting.Indented));
                    sw.Flush();
                }
                HostId = guid;
            }
            else {
                HostId = Guid.Parse(id);
            }
        }
    }
}
