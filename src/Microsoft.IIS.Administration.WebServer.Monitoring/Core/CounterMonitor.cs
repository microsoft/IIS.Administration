// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class CounterMonitor : IDisposable
    {
        private readonly TimeSpan RefreshRate = TimeSpan.FromSeconds(1);
        Dictionary<IPerfCounter, PdhCounterHandle> _counters = new Dictionary<IPerfCounter, PdhCounterHandle>();
        private PdhQueryHandle _query;
        private CounterFinder _counterFinder = new CounterFinder();
        private DateTime _lastCalculatedTime;

        public CounterMonitor(IEnumerable<IPerfCounter> counters)
        {
            foreach (var counter in counters) {
                _counters.Add(counter, null);
            }
        }

        public IEnumerable<IPerfCounter> Counters {
            get {
                return _counters.Keys;
            }
        }

        public void Dispose()
        {
            foreach (var key in _counters.Keys.ToList()) {
                if (_counters[key] != null) {
                    _counters[key].Dispose();
                    _counters[key] = null;
                }
            }

            if (_query != null) {
                _query.Dispose();
                _query = null;
            }
        }

        public async Task Refresh()
        {
            if (_counters.Count == 0 || DateTime.UtcNow - _lastCalculatedTime < RefreshRate) {
                return;
            }

            EnsureInit();

            PDH_FMT_COUNTERVALUE value = default(PDH_FMT_COUNTERVALUE);

            uint result = Pdh.PdhCollectQueryData(_query);
            if (result == Pdh.PDH_NO_DATA) {
                throw new CounterNotFoundException(_counters.First().Key);
            }
            if (result != 0) {
                throw new Win32Exception((int)result);
            }

            bool multiSample = false;
            foreach (KeyValuePair<IPerfCounter, PdhCounterHandle> counterInstance in _counters) {

                IPerfCounter counter = counterInstance.Key;
                PdhCounterHandle handle = counterInstance.Value;

                result = Pdh.PdhGetFormattedCounterValue(handle, PdhFormat.PDH_FMT_LARGE, IntPtr.Zero, out value);

                if (result == Pdh.PDH_INVALID_DATA && value.CStatus == Pdh.PDH_CSTATUS_INVALID_DATA && !multiSample) {

                    multiSample = true;

                    result = Pdh.PdhCollectQueryData(_query);
                    if (result != 0) {
                        throw new Win32Exception((int)result);
                    }

                    result = Pdh.PdhGetFormattedCounterValue(handle, PdhFormat.PDH_FMT_LARGE, IntPtr.Zero, out value);
                }

                if ((result != 0 && value.CStatus == Pdh.PDH_CSTATUS_NO_INSTANCE) ||
                        result == Pdh.PDH_INVALID_HANDLE ||
                        result == Pdh.PDH_CALC_NEGATIVE_VALUE ||
                        (result == Pdh.PDH_INVALID_DATA && value.CStatus == Pdh.PDH_INVALID_DATA)) {

                    throw new CounterNotFoundException(counter);
                }
                else if (result != 0) {

                    if (!_counterFinder.CounterExists(counter)) {
                        throw new CounterNotFoundException(counter);
                    }

                    throw new Win32Exception((int)result);
                }
                else if (value.CStatus != 0) {
                    throw new Win32Exception((int)value.CStatus);
                }

                counter.Value = value.longLongValue;
            }

            _lastCalculatedTime = DateTime.UtcNow;
        }

        private void EnsureInit()
        {
            if (_query != null) {
                return;
            }

            PdhQueryHandle query;

            uint result = Pdh.PdhOpenQueryW(null, IntPtr.Zero, out query);
            if (result != 0) {
                throw new Win32Exception((int)result);
            }

            try {
                foreach (var counter in _counters.Keys.ToList()) {
                    PdhCounterHandle hCounter;

                    result = Pdh.PdhAddCounterW(query, counter.Path, IntPtr.Zero, out hCounter);
                    if (result == Pdh.PDH_CSTATUS_NO_OBJECT) {
                        throw new CounterNotFoundException(counter);
                    }
                    if (result != 0) {
                        throw new Win32Exception((int)result);
                    }

                    _counters[counter] = hCounter;
                }
            }
            catch (Win32Exception) {
                query.Dispose();
                throw;
            }

            _query = query;
        }
    }
}
