// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using System;
    using System.Runtime.InteropServices;

    static class BitnessUtility {

        public static void AppendBitnessPreCondition(ref string preCondition, string filePath) {
            if (Is32BitMachine()) {
                return;
            }

            if (Is32Bit(filePath)) {
                if (!preCondition.ToLower().Contains("bitness32")) {
                    if (preCondition.ToLower().Contains("bitness64")) {
                        preCondition = preCondition.Replace("bitness64", "bitness32");
                    }
                    else {
                        preCondition = preCondition + ",bitness32";
                    }
                }
            }
            else {
                if (!preCondition.ToLower().Contains("bitness64")) {
                    if (preCondition.ToLower().Contains("bitness32")) {
                        preCondition = preCondition.Replace("bitness32", "bitness64");
                    }
                    else {
                        preCondition = preCondition + ",bitness64";
                    }
                }
            }

            preCondition = preCondition.Trim(',');
        }

        private static bool Is32BitMachine() {
            return RuntimeInformation.OSArchitecture == Architecture.X86;
        }

        private static bool Is32Bit(string filePath)
        {
            var peInfo = new PeInfo(Environment.ExpandEnvironmentVariables(filePath));
            return peInfo.Machine == ImageFileMachine.I386;
        }
    }
}
