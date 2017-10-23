// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Used for finding what counters are available on the system.
    /// Does not provide values for performance counters.
    /// </summary>

    sealed class CounterFinder
    {
        private ICounterTranslator _translator;

        public CounterFinder(ICounterTranslator translator)
        {
            _translator = translator;
        }

        public IEnumerable<IPerfCounter> GetSingletonCounters(string category, IEnumerable<string> counterNames)
        {
            List<IPerfCounter> counters = new List<IPerfCounter>();

            foreach (string name in counterNames) {
                counters.Add(new SingletonPerfCounter(name, category));
            }

            return counters;
        }

        public IEnumerable<IPerfCounter> GetCounters(string category, string instance, IEnumerable<string> counterNames)
        {
            if (!GetInstances(category).Contains(instance)) {
                return Enumerable.Empty<IPerfCounter>();
            }

            List<IPerfCounter> counters = new List<IPerfCounter>();

            foreach (string name in counterNames) {
                counters.Add(new PerfCounter(name, instance, category));
            }

            return counters;
        }

        public IEnumerable<IPerfCounter> GetCountersByName(string category, string counterName)
        {
            List<IPerfCounter> counters = new List<IPerfCounter>();

            foreach (string instance in GetInstances(category)) {
                counters.Add(new PerfCounter(counterName, instance, category));
            }

            return counters;
        }

        public IEnumerable<string> GetInstances(string category)
        {
            List<string> strings = ExpandCounterPath(@"\" + _translator.TranslateCategory(category) + @"(*)\*", PdhExpansionFlags.PDH_NOEXPANDCOUNTERS);

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

        public bool CounterExists(IPerfCounter counter)
        {
            return GetCounters(counter.CategoryName, counter.InstanceName).Any(c => c.Name.Equals(counter.Name));
        }

        private IEnumerable<IPerfCounter> GetCounters(string category, string instance)
        {
            if (!GetInstances(category).Contains(instance)) {
                return Enumerable.Empty<IPerfCounter>();
            }

            List<IPerfCounter> counters = new List<IPerfCounter>();
            List<string> strings = ExpandCounterPath(@"\" + _translator.TranslateCategory(category) + @"(" + instance + @")\*", PdhExpansionFlags.NONE);

            for (int i = 0; i < strings.Count; i++) {
                string s = strings[i];
                s = s.Substring(s.IndexOf('\\'));
                s = s.Substring(2);
                s = s.Substring(s.IndexOf('\\') + 1);
                s = s.Substring(s.IndexOf(')') + 2);
                strings[i] = s;

                counters.Add(new PerfCounter(s, instance, category));
            }

            return counters;
        }

        private List<string> ExpandCounterPath(string searchPattern, PdhExpansionFlags flags)
        {
            long cchPathListLength = 0;
            uint result = 0;
            byte[] buffer = null;

            //
            // Do - while, the buffer can grow after being told the necessary size
            do {

                IntPtr mszExpandedPathList = IntPtr.Zero;

                if (cchPathListLength > 0) {
                    //
                    // If we received a buffer size allocate one
                    // Unicode size is 2 bytes
                    mszExpandedPathList = Marshal.AllocHGlobal(2 * (int)cchPathListLength);
                }

                try {
                    result = Pdh.PdhExpandWildCardPathW(null, searchPattern, mszExpandedPathList, ref cchPathListLength, flags);

                    if (result == Pdh.PDH_MORE_DATA) {
                        continue;
                    }

                    if (result == Pdh.PDH_CSTATUS_NO_OBJECT || result == Pdh.PDH_CSTATUS_NO_INSTANCE) {
                        return new List<string>();
                    }

                    if (result != 0 && result != Pdh.PDH_MORE_DATA) {
                        throw new Win32Exception((int)result);
                    }

                    buffer = new byte[cchPathListLength * 2];
                    Marshal.Copy(mszExpandedPathList, buffer, 0, buffer.Length);

                }
                finally {

                    //
                    // Always clean up allocated buffer
                    if (mszExpandedPathList != IntPtr.Zero) {
                        Marshal.FreeHGlobal(mszExpandedPathList);
                    }

                }

            } while (result == Pdh.PDH_MORE_DATA);

            if (buffer == null) {
                throw new Win32Exception((int)result);
            }

            //
            // Parse paths from filled buffer

            List<string> strings = new List<string>();
            int start = 0;
            int end = 0;

            var chars = Encoding.Unicode.GetChars(buffer);

            do {

                do {
                    end++;
                }
                while (end < chars.Length && chars[end] != 0);

                strings.Add(new string(chars, start, end - start));
                start = end;

            } while (start < chars.Length - 1 && chars[start + 1] != 0);

            return strings;
        }
    }
}
