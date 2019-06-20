// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;

    class WebServerFeatureManager : IWebServerFeatureManager
    {
        public async Task Enable(params string[] features)
        {
            await SetFeatureEnabled(true, features);
        }

        public async Task Disable(params string[] features)
        {
            await SetFeatureEnabled(false, features);
        }


        private Task SetFeatureEnabled(bool enabled, params string[] features)
        {
            string arguments = $"/Quiet /NoRestart /Online {(enabled ? "/Enable-Feature" : "/Disable-Feature")}";

            foreach (string feature in features) {
                arguments += $" /FeatureName:{feature}";
            }

            ProcessStartInfo info = new ProcessStartInfo("dism.exe", arguments);

            Process p = new Process() {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>();
            var errorStream = new StringBuilder();
            var outputStream = new StringBuilder();
            p.ErrorDataReceived += (sender, e) =>
            {
                errorStream.AppendLine(e.Data);
            };
            p.OutputDataReceived += (sender, e) =>
            {
                outputStream.AppendLine(e.Data);
            };
            p.Exited += (sender, args) => {
                if (p.ExitCode != 0) {
                    // 3010 status code: https://github.com/microsoft/IIS.Administration/issues/236
                    tcs.SetException(new DismException(
                        p.ExitCode,
                        string.Join(", ", features),
                        errorStream.ToString(),
                        outputStream.ToString()));
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
