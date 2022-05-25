// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Hosting;
    using Core;
    using Microsoft.IIS.Administration.Extensibility;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;

    class ModuleLoader
    {
        private IWebHostEnvironment _env;
        private AssemblyLoadContext _loader;
        private List<Assembly> _loadedAssemblies;
        private AdminHost _moduleHolder;
        private string _moduleLoadBasePath;

        private const string PLUGINS_FOLDER_NAME = "plugins";

        public ModuleLoader(IWebHostEnvironment env)
        {
            this._env = env;
            this._moduleLoadBasePath = Path.Combine(env.ContentRootPath, PLUGINS_FOLDER_NAME);
            this._loadedAssemblies = new List<Assembly>();
            this._moduleHolder = AdminHost.Instance;
            this._loader = new PluginAssemblyLoadContext(_moduleLoadBasePath);
        }

        public Assembly LoadModule(string assemblyName)
        {
            string assemblyPath = Path.Combine(_moduleLoadBasePath, $"{assemblyName}.dll");

            Log.Logger.Debug($"Loading plugin {assemblyName}");

            Assembly assembly = _loader.LoadFromAssemblyPath(assemblyPath);
            _loadedAssemblies.Add(assembly);

            //
            // Every module should expose a type called Startup in a namespace equivalent to the assembly name
            Type type = assembly.GetType(assemblyName + ".Startup");
            IModule module = (IModule) Activator.CreateInstance(type);
            _moduleHolder.Add(module);
            return assembly;
        }

        public List<Assembly> GetAllLoadedAssemblies()
        {
            return _loadedAssemblies;
        }
    }
}
