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
        private Dictionary<string, IEnumerable<string>> _counters = new Dictionary<string, IEnumerable<string>>();
        private CounterFinder _finder = new CounterFinder();

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

        public string TranslateCounterName(string category, string counterName)
        {
            string translated = null;

            if (_map == null) {
                BuildLookup();
            }

            //
            // If not in map no translation available
            if (!_map.TryGetValue(counterName, out List<int> indexes)) {

                return counterName;

            }

            //
            // Simple case is no ambiguity
            if (indexes.Count == 1) {
                translated = _lookup[indexes[0]];
            }

            if (translated == null) {

                //
                // Handle ambiguity by performing match to translated counter names for the category

                string translatedCategory = TranslateCategory(category);

                if (!_counters.TryGetValue(translatedCategory, out IEnumerable<string> counterNames)) {
                    counterNames = _counters[translatedCategory] = _finder.GetCounterNames(translatedCategory);
                }

                foreach (string name in counterNames) {

                    foreach (int index in indexes) {

                        string indexedName = _lookup[index];

                        //
                        // The lookup points to a counter that is in the category, this is the target counter name
                        if (indexedName.Equals(name)) {

                            translated = indexedName;
                        }
                    }
                }

            }

            if (string.IsNullOrEmpty(translated)) {

                //
                // Counter not translatable
                Log.Information($"Could not translate counter: {category}\\{counterName}");

                return counterName;
            }

            return translated;
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
