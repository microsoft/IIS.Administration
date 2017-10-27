// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.IIS.Administration
{
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Files;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;

    class ConfigurationWriter : IConfigurationWriter
    {
        private string _path;

        public ConfigurationWriter(string configurationPath)
        {
            _path = configurationPath;
        }

        public void WriteSection(string name, object value)
        {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }

            try {
                WriteSectionInteral(name, value);
            }
            catch (IOException e) {
                if (e.HResult == HResults.FileInUse) {
                    throw new LockedException("appsettings.json");
                }

                throw;
            }
        }

        private void WriteSectionInteral(string name, object value)
        {
            JToken root = JObject.Parse(File.ReadAllText(_path));
            JToken seeker = root;
            JToken parent = null;
            string[] parts = name.Split(':');
            int part = 0;

            //
            // Traverse through object to get the target section's parent
            while (seeker != null && seeker is JObject && part < parts.Length - 1) {
                seeker = seeker[parts[part]];
                part++;
            }

            //
            // Cannot write the section if a node in the section path is not a JObject
            if (seeker != null && !(seeker is JObject)) {
                throw new FormatException("Invalid section");
            }

            //
            // If we traversed all the way we found the value of the target section
            if (part == parts.Length - 1) {
                parent = seeker;
            }

            if (parent == null) {

                //
                // We traverse again, this time creating the parent section
                seeker = root;
                part = 0;
                while (seeker != null && seeker is JObject && part < parts.Length - 1) {

                    if (seeker[parts[part]] == null) {
                        seeker[parts[part]] = JObject.FromObject(new { });
                    }

                    seeker = seeker[parts[part]];
                    part++;
                }

                parent = seeker;
            }

            string sectionName = parts[parts.Length - 1];
            parent[sectionName] = JToken.FromObject(value);

            File.WriteAllText(_path, JsonConvert.SerializeObject(root, Formatting.Indented));
        }
    }
}
