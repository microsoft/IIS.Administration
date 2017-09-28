// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class CounterProvider : ICounterProvider, IDisposable
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);
        private MemoryCache _cache;
        private Timer _timer;
        private CounterFinder _counterFinder = new CounterFinder();

        public CounterProvider()
        {
            _cache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = CacheExpiration
            });

            _timer = new Timer(TimerCallback, null, 120 * 1000, 120 * 1000);
        }

        public async Task<IEnumerable<IPerfCounter>> GetCounters(string category, string instance)
        {
            if (!_counterFinder.GetInstances(category).Contains(instance)) {
                return Enumerable.Empty<IPerfCounter>();
            }

            CounterMonitor monitor = _cache.Get<CounterMonitor>(category + instance);

            if (monitor == null) {

                IEnumerable<IPerfCounter> counters = _counterFinder.GetCounters(category, instance);

                monitor = new CounterMonitor(counters);

                AddMonitor(category + instance, monitor);
            }

            try {
                await monitor.Refresh();
            }
            catch (CounterNotFoundException) {
                _cache.Remove(category + instance);
                monitor.Dispose();
                throw;
            }

            return monitor.Counters;
        }

        public async Task<IEnumerable<IPerfCounter>> GetSingletonCounters(string category)
        {
            CounterMonitor monitor = _cache.Get<CounterMonitor>(category);

            if (monitor == null) {

                IEnumerable<IPerfCounter> counters = _counterFinder.GetSingletonCounters(category);

                monitor = new CounterMonitor(counters);

                AddMonitor(category, monitor);
            }

            try {
                await monitor.Refresh();
            }
            catch (CounterNotFoundException) {
                _cache.Remove(category);
                monitor.Dispose();
                throw;
            }

            return monitor.Counters;
        }

        private void TimerCallback(object state)
        {
            _cache.Get(string.Empty);
        }

        private void AddMonitor(string key, CounterMonitor monitor)
        {
            _cache.Set(key, monitor, new MemoryCacheEntryOptions() {
                SlidingExpiration = CacheExpiration
            }.RegisterPostEvictionCallback(PostEvictionCallback));
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            CounterMonitor monitor = value as CounterMonitor;

            if (value != null) {
                monitor.Dispose();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
