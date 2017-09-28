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

    public class AsyncCounterProvider
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(10);
        private MemoryCache _cache;
        private Timer _timer;
        private CounterProvider _counterProvider = new CounterProvider();

        public AsyncCounterProvider()
        {
            _cache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = CacheExpiration
            });

            _timer = new Timer(TimerCallback, null, 5 * 1000, 5 * 1000);
        }

        public async Task<IEnumerable<IPerfCounter>> GetCountersAsync(string category, string instance)
        {
            if (!_counterProvider.GetInstances(category).Contains(instance)) {
                return Enumerable.Empty<IPerfCounter>();
            }

            CounterMonitor monitor = _cache.Get<CounterMonitor>(category + instance);

            if (monitor == null) {

                IEnumerable<IPerfCounter> counters = _counterProvider.GetCounters(category, instance);

                monitor = new CounterMonitor(counters);

                AddMonitor(category + instance, monitor);
            }

            try {
                await monitor.Refresh();
            }
            catch (CounterNotFoundException) {
                monitor.Dispose();
                _cache.Remove(category + instance);
                throw;
            }

            return monitor.Counters;
        }

        public async Task<IEnumerable<IPerfCounter>> GetSingletonCounters(string category)
        {
            CounterMonitor monitor = _cache.Get<CounterMonitor>(category);

            if (monitor == null) {

                IEnumerable<IPerfCounter> counters = _counterProvider.GetSingletonCounters(category);

                monitor = new CounterMonitor(counters);

                AddMonitor(category, monitor);
            }

            try {
                await monitor.Refresh();
            }
            catch (CounterNotFoundException) {
                monitor.Dispose();
                _cache.Remove(category);
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
    }
}
