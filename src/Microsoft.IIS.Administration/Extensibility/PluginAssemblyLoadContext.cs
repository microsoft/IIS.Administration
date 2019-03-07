// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Extensibility
{
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    // Implemented for https://github.com/dotnet/coreclr/issues/5837
    // Can move back to resolver callback when fixed
    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private string _pluginDir;

        public PluginAssemblyLoadContext(string pluginDirectory)
        {
            this._pluginDir = pluginDirectory;
        }

        private Assembly LoadFromCurrentDomain(AssemblyName target)
        {
            foreach (var existing in AppDomain.CurrentDomain.GetAssemblies())
            {
                var existingName = existing.GetName();
                if (existingName.Name == target.Name)
                {
                    // assume higher version always compatable
                    if (existingName.Version >= target.Version)
                    {
                        return existing;
                    }
                }
            }
            return null;
        }

        private Assembly LoadFromPluginDir(AssemblyName assemblyName)
        {
            var rootPaths = new List<string>();
            rootPaths.Add(Path.Combine(_pluginDir, $"{assemblyName.Name}.{assemblyName.Version}"));
            rootPaths.Add(_pluginDir);

            foreach (var path in rootPaths)
            {
                string asmPath = Path.Combine(path, $"{assemblyName.Name}.dll");
                Log.Logger.Debug($"Resolving plugin dependency {assemblyName} using location {asmPath}");
                if (File.Exists(asmPath))
                {
                    // If LoadFromAssemblyPath's argument does not point to a valid assembly a fatal error will occur that will not throw an exception
                    // The process will terminate ungracefully
                    return LoadFromAssemblyPath(asmPath);
                }
            }
            return null;
        }

        private Assembly LoadFromRuntimeDir(AssemblyName assemblyName)
        {
            string winRuntime = null;
            string runtimes = Path.Combine(this._pluginDir, "runtimes");

            if (Directory.Exists(runtimes))
            {
                winRuntime = Directory.GetDirectories(runtimes).FirstOrDefault(d => d.ToLower().Contains("win"));
            }

            if (winRuntime != null)
            {
                foreach (var file in Directory.GetFiles(winRuntime, "*.dll", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) == assemblyName.Name + ".dll")
                    {
                        return LoadFromAssemblyPath(file);
                    }
                }
            }
            return null;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            Assembly asm = LoadFromCurrentDomain(assemblyName);
            if (asm != null)
            {
                return asm;
            }
            asm = LoadFromPluginDir(assemblyName);
            if (asm != null)
            {
                return asm;
            }
            return LoadFromCurrentDomain(assemblyName);
        }
    }
}
