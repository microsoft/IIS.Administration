// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using Serilog;
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

        protected override Assembly Load(AssemblyName assemblyName)
        {

            Assembly asm = null;

            IList<string> rootPaths = new List<string>();

            rootPaths.Add(Path.Combine(this._pluginDir, $"{assemblyName.Name}.{assemblyName.Version}"));
            rootPaths.Add(this._pluginDir);

            foreach (var path in rootPaths) {

                string asmPath = Path.Combine(path, $"{assemblyName.Name}.dll");

                Log.Logger.Debug($"Resolving plugin dependency {assemblyName} using location {asmPath}");

                if (File.Exists(asmPath)) {
                    // If LoadFromAssemblyPath's argument does not point to a valid assembly a fatal error will occur that will not throw an exception
                    // The process will terminate ungracefully
                    asm = this.LoadFromAssemblyPath(asmPath);
                }
            }

            // Possible runtime assembly
            if (asm == null) {

                string winRuntimeDirectory = null;
                string[] winRunTimeDlls = null;

                string runTimesPath = Path.Combine(this._pluginDir, "runtimes");
                if (Directory.Exists(runTimesPath)) {

                    winRuntimeDirectory = Directory.GetDirectories(runTimesPath).FirstOrDefault(d => {
                        return d.ToLower().Contains("win");
                    });
                }

                if (winRuntimeDirectory != null) {
                    winRunTimeDlls = Directory.GetFiles(winRuntimeDirectory, "*.dll", SearchOption.AllDirectories);
                }

                foreach (var file in winRunTimeDlls) {
                    if (Path.GetFileName(file) == assemblyName.Name + ".dll") {
                        asm = this.LoadFromAssemblyPath(file);
                        break;
                    }
                }
            }

            return asm;
        }
    }
}
