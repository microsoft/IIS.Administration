// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    class CounterUtil
    {
        //
        // 009 - English Language Id
        private const string CounterNamesRegKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009";
        private const string CounterNamesRegSubKey = "counter";

        public static Dictionary<string, List<int>> GetCounterIdMap()
        {
            string[] entries = null;
            var idMap = new Dictionary<string, List<int>>();

            using (var key = Registry.LocalMachine.OpenSubKey(CounterNamesRegKey)) {
                entries = (string[])key?.GetValue(CounterNamesRegSubKey);
            }

            if (entries != null) {

                for (int i = 0; i < entries.Length - 1; i += 2) {

                    string key = entries[i + 1];

                    if (int.TryParse(entries[i], out int val)) {

                        if (!idMap.ContainsKey(key)) {

                            idMap[key] = new List<int>() { val };
                        }
                        else {

                            idMap[key].Add(val);
                        }
                    }
                }
            }

            return idMap;
        }

        public static string LookupName(uint index)
        {
            uint bufSize = 0;

            uint result = Pdh.PdhLookupPerfNameByIndexW(null, index, null, ref bufSize);

            if (result != 0 && result != Pdh.PDH_MORE_DATA) {
                throw new Win32Exception((int)result);
            }

            StringBuilder buffer = new StringBuilder((int)bufSize);

            result = Pdh.PdhLookupPerfNameByIndexW(null, index, buffer, ref bufSize);

            if (result != 0) {
                throw new Win32Exception((int)result);
            }

            return buffer.ToString();
        }
    }
}
