// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Concurrent collection of performance counters that can be refreshed to populate performance counter values
    /// </summary>

    sealed class CounterMonitor : IDisposable
    {
        private readonly TimeSpan RefreshRate = TimeSpan.FromMilliseconds(1000);
        private Dictionary<IPerfCounter, PdhCounterHandle> _counters = new Dictionary<IPerfCounter, PdhCounterHandle>();
        private PdhQueryHandle _query;
        private DateTime _lastCalculatedTime;
        private CounterFinder _counterFinder;
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public CounterMonitor(CounterFinder finder, IEnumerable<IPerfCounter> counters)
        {
            _counterFinder = finder;
            AddCounters(counters);
        }

        public IEnumerable<IPerfCounter> Counters {
            get {
                _lock.EnterReadLock();

                try {
                    return _counters.Keys;
                }
                finally {
                    _lock.ExitReadLock();
                }
            }
        }

        private bool NeedsRefresh {
            get {
                return _counters.Count > 0 && DateTime.UtcNow - _lastCalculatedTime >= RefreshRate;
            }
        }

        public void Dispose()
        {
            _lock.EnterWriteLock();

            try {
                DoDispose();
            }
            finally {
                _lock.ExitWriteLock();
            }

            if (_lock != null) {
                _lock.Dispose();
                _lock = null;
            }
        }

        public async Task Refresh()
        {
            if (!NeedsRefresh) {
                return;
            }

            //
            // Immediately return if can't obtain lock
            if (_lock.TryEnterWriteLock(0)) {
                try {
                    await DoRefresh();
                }
                finally {
                    _lock.ExitWriteLock();
                }
            }
        }

        public void AddCounters(IEnumerable<IPerfCounter> counters)
        {
            _lock.EnterWriteLock();

            try {

                DoAddCounters(counters);
            }
            finally {
                _lock.ExitWriteLock();
            }
        }

        public void RemoveCounters(IEnumerable<IPerfCounter> counters)
        {
            IEnumerable<PdhCounterHandle> removedCounters = null;

            _lock.EnterWriteLock();

            try {
                removedCounters = DoRemoveCounters(counters);
            }
            finally {
                _lock.ExitWriteLock();
            }

            //
            // References not available to other threads, no need to guard
            
            foreach (var counterHandle in removedCounters) {
                counterHandle.Dispose();
            }
        }

        public async Task DoRefresh()
        {
            if (!NeedsRefresh) {
                return;
            }

            PDH_FMT_COUNTERVALUE value = default(PDH_FMT_COUNTERVALUE);

            uint result = Pdh.PdhCollectQueryData(_query);
            if (result == Pdh.PDH_NO_DATA) {
                throw new MissingCountersException(_counters.Keys);
            }
            if (result != 0) {
                throw new Win32Exception((int)result);
            }

            bool multiSample = false;
            List<IPerfCounter> missingCounters = new List<IPerfCounter>();
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
                        result == Pdh.PDH_CALC_NEGATIVE_DENOMINATOR ||
                        (result == Pdh.PDH_INVALID_DATA && value.CStatus == Pdh.PDH_INVALID_DATA)) {

                    missingCounters.Add(counter);
                    continue;
                }
                else if (result != 0) {

                    if (!_counterFinder.CounterExists(counter)) {
                        missingCounters.Add(counter);
                        continue;
                    }

                    throw new Win32Exception((int)result);
                }
                else if (value.CStatus != 0) {
                    throw new Win32Exception((int)value.CStatus);
                }

                counter.Value = value.longLongValue;
            }

            if (missingCounters.Count > 0) {
                throw new MissingCountersException(missingCounters);
            }

            TimeSpan calculationDelta = DateTime.UtcNow - _lastCalculatedTime;
            _lastCalculatedTime = DateTime.UtcNow;
        }

        private void DoDispose()
        {
            foreach (var key in _counters.Keys.ToList()) {
                if (_counters[key] != null) {
                    _counters[key].Dispose();
                    _counters.Remove(key);
                }
            }

            if (_query != null) {
                _query.Dispose();
                _query = null;
            }
        }

        private void DoAddCounters(IEnumerable<IPerfCounter> counters)
        {
            uint result;

            if (_query == null) {

                PdhQueryHandle query;

                result = Pdh.PdhOpenQueryW(null, IntPtr.Zero, out query);
                if (result != 0) {
                    throw new Win32Exception((int)result);
                }

                _query = query;
            }

            List<IPerfCounter> missingCounters = new List<IPerfCounter>();
            foreach (var counter in counters) {
                PdhCounterHandle hCounter;

                result = Pdh.PdhAddEnglishCounterW(_query, counter.Path, IntPtr.Zero, out hCounter);
                if (result == Pdh.PDH_CSTATUS_NO_OBJECT ||
                    result == Pdh.PDH_CSTATUS_NO_COUNTER) {
                    missingCounters.Add(counter);
                    continue;
                }
                if (result != 0) {
                    throw new Win32Exception((int)result);
                }

                _counters[counter] = hCounter;
            }

            if (missingCounters.Count > 0) {
                throw new MissingCountersException(missingCounters);
            }
        }

        private IEnumerable<PdhCounterHandle> DoRemoveCounters(IEnumerable<IPerfCounter> counters)
        {
            PdhCounterHandle hCounter = null;
            List<PdhCounterHandle> removedCounters = new List<PdhCounterHandle>();

            foreach (var counter in counters) {

                if (_counters.TryGetValue(counter, out hCounter)) {

                    _counters.Remove(counter);

                    removedCounters.Add(hCounter);
                }
            }

            return removedCounters;
        }
    }
}
