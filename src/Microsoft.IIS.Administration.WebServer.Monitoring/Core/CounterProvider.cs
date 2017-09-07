// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class CounterProvider
    {
        //private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);
        //private MemoryCache _cache;

        //public CounterProvider()
        //{
        //    _cache = new MemoryCache(new MemoryCacheOptions() {
        //        ExpirationScanFrequency = CacheExpiration
        //    });
        //}


        public IEnumerable<IPerfCounter> GetCounters(string category)
        {
            //
            // Syntax of a counter path:
            // \\Computer\PerfObject(ParentInstance/ObjectInstance#InstanceIndex)\Counter

            List<IPerfCounter> counters = new List<IPerfCounter>();
            List<string> counterPaths = Utils.ExpandCounterPath(@"\" + category + @"(*)\*", PdhExpansionFlags.NONE);

            foreach (string counterPath in counterPaths) {

                string s = counterPath;
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('(') + 1);

                string instance = s.Substring(0, s.IndexOf(')'));
                string name = s.Substring(s.IndexOf(')') + 2);

                counters.Add(new PerfCounter(name, instance, category));
            }

            return counters;
        }

        public IEnumerable<IPerfCounter> GetSingletonCounters(string category)
        {
            List<IPerfCounter> counters = new List<IPerfCounter>();
            List<string> counterPaths = Utils.ExpandCounterPath(@"\" + category + @"\*", PdhExpansionFlags.NONE);

            foreach (string counterPath in counterPaths) {

                string s = counterPath;
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('\\') + 1);
                string name = s.Substring(s.IndexOf('\\') + 1);

                counters.Add(new SingletonPerfCounter(name, category));
            }

            return counters;
        }

        public IEnumerable<IPerfCounter> GetCounters(string category, string instance)
        {
            if (!GetInstances(category).Contains(instance)) {
                return Enumerable.Empty<IPerfCounter>();
            }

            List<string> strings = Utils.ExpandCounterPath(@"\" + category + @"(" + instance + @")\*", PdhExpansionFlags.NONE);

            for (int i = 0; i < strings.Count; i++) {
                string s = strings[i];
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('\\') + 1);
                s = s.Substring(s.IndexOf(')') + 2);
                strings[i] = s;
            }

            return strings.Select(s => new PerfCounter(s, instance, category));
        }

        public IEnumerable<IPerfCounter> GetCountersByName(string category, string counterName)
        {
            List<IPerfCounter> counters = new List<IPerfCounter>();
            List<string> strings = Utils.ExpandCounterPath(@"\" + category + @"(*)\" + counterName, PdhExpansionFlags.NONE);

            for (int i = 0; i < strings.Count; i++) {
                string s = strings[i];
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('(') + 1);
                string instance = s.Substring(0, s.IndexOf(')'));

                counters.Add(new PerfCounter(counterName, instance, category));
            }

            return counters;
        }

        public IEnumerable<string> GetInstances(string category)
        {
            List<string> strings = Utils.ExpandCounterPath(@"\" + category + @"(*)\*", PdhExpansionFlags.PDH_NOEXPANDCOUNTERS);

            for (int i = 0; i < strings.Count; i++) {
                string s = strings[i];
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('\\') + 1);
                s = s.Substring(s.IndexOf('(') + 1);
                s = s.Substring(0, s.IndexOf(')'));
                strings[i] = s;
            }

            return strings;
        }

        public IEnumerable<string> GetCategories()
        {
            List<string> strings = Utils.ExpandCounterPath(@"\*(*)\*", PdhExpansionFlags.PDH_NOEXPANDCOUNTERS | PdhExpansionFlags.PDH_NOEXPANDINSTANCES);

            for (int i = 0; i < strings.Count; i++) {
                string s = strings[i];
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('\\') + 1);
                s = s.Substring(0, s.IndexOf('('));
                strings[i] = s;
            }

            return strings;
        }

        public bool CounterExists(IPerfCounter counter)
        {
            return GetCounters(counter.CategoryName, counter.InstanceName).Any(c => c.Name.Equals(counter.Name));
        }
    }
}
