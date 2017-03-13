// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    class WebServerFeatureManager : IWebServerFeatureManager
    {
        public async Task Enable(string feature)
        {
            await SetFeatureEnabled(feature, true);
        }

        public async Task Disable(string feature)
        {
            await SetFeatureEnabled(feature, false);
        }


        private Task SetFeatureEnabled(string feature, bool enabled)
        {
            string arguments = $"/Online {(enabled ? "/Enable-Feature" : "/Disable-Feature")} /FeatureName:{feature}";
            ProcessStartInfo info = new ProcessStartInfo("dism.exe", arguments);

            Process p = new Process() {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>();

            p.Exited += (sender, args) => {
                if (p.ExitCode != 0) {
                    tcs.SetException(new DismException(p.ExitCode, feature));
                }
                else {
                    tcs.SetResult(p.ExitCode);
                }
                p.Dispose();
            };

            p.Start();
            return tcs.Task;
        }
    }
}
