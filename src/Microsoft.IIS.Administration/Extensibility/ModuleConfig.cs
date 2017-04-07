// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using Newtonsoft.Json;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Parses data from a configuration file formatted using JSON. The Parser is passed the path to the configuration file and it immediately loads it.
    /// The data in the configuration file is available from the parser through the ModuleConfig object which acts as the root object of the config file
    /// the configuration file.
    /// </summary>
    class ModuleConfig
    {
        private ModulesConfigData _configJsonObject;
        private string _configFilePath;
        private string[] _modules;


        public ModuleConfig(string configFilePath)
        {
            this._configFilePath = configFilePath;
            LoadConfigFile();
        }

        private void LoadConfigFile()
        {
            string fullPath = _configFilePath;

            if (Path.IsPathRooted(fullPath)) {

                fullPath = Path.GetFullPath(fullPath);
            }

            string fileContents;
            if (!File.Exists(fullPath)) {

                throw new FileNotFoundException(fullPath);
            }

            StreamReader sr;
            using (sr = new StreamReader(new FileStream(_configFilePath, FileMode.Open, FileAccess.Read))) {

                fileContents = sr.ReadToEnd();
            }

            _configJsonObject = JsonConvert.DeserializeObject<ModulesConfigData>(fileContents);
        }

        public string[] Modules
        {
            get
            {
                if (this._modules == null) {
                    this._modules = this._configJsonObject.modules.Where(m => m.Enabled).Select(m => m.Name).ToArray();
                }

                return this._modules;
            }
        }

        public ModulesConfigData GetConfigDataRepresentation()
        {
            return _configJsonObject;
        }
    }

    /// <summary>
    /// Provides a strongly typed data representation of the JSON config file that the IIS Management Service reads for loading modules.
    /// </summary>
    class ModulesConfigData
    {
        public ModuleConfigData[] modules = null;
        public ModulesConfigData() { }
    }

    class ModuleConfigData
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
    }
}
