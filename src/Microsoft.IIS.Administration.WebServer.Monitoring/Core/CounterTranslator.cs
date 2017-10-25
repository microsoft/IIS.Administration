// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using Microsoft.Win32;
    using Serilog;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    sealed class CounterTranslator : ICounterTranslator
    {
        //
        // 009 - English Language Id
        private const string CounterNamesRegKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009";
        private const string CounterNamesRegSubKey = "counter";

        //
        // Performance counter entries can be duplicated so that indexes for an english performance counter name are ambiguous
        // A lookup must be created
        // https://support.microsoft.com/en-us/help/287159/using-pdh-apis-correctly-in-a-localized-language

        private Dictionary<string, List<int>> _map = null;
        private Dictionary<int, string> _lookup = null;

        public string TranslateCategory(string counterName)
        {
            if (_map == null) {
                BuildLookup();
            }

            string translated = null;

            if (_map.TryGetValue(counterName, out List<int> indexes)) {

                if (indexes.Count > 1) {
                    Log.Error("Ambiguous translation for performance counter category");
                }

                translated = LookupName((uint)indexes[0]);
            }

            return !string.IsNullOrEmpty(translated) ? translated : counterName;
        }

        private void BuildLookup()
        {
            var map = GetCounterIdMap();
            var lookup = new Dictionary<int, string>();

            foreach (var kvp in map) {

                foreach (var index in kvp.Value) {
                    lookup[index] = LookupName((uint)index);
                }
            }

            _map = map;
            _lookup = lookup;
        }

        private Dictionary<string, List<int>> GetCounterIdMap()
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

        private string LookupName(uint index)
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
