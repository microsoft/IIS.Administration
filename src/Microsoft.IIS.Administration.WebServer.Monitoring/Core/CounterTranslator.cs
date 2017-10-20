// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using Microsoft.IIS.Administration.WebServer.Monitoring;
    using Serilog;
    using System.Collections.Generic;

    class CounterTranslator : ICounterTranslator
    {
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

                translated = CounterUtil.LookupName((uint)indexes[0]);
            }

            return !string.IsNullOrEmpty(translated) ? translated : counterName;
        }

        private void BuildLookup()
        {
            var map = CounterUtil.GetCounterIdMap();
            var lookup = new Dictionary<int, string>();

            foreach (var kvp in map) {

                foreach (var index in kvp.Value) {
                    lookup[index] = CounterUtil.LookupName((uint)index);
                }
            }

            _map = map;
            _lookup = lookup;
        }
    }
}
