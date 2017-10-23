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

    sealed class CounterProvider : ICounterProvider, IDisposable
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ScanFrequency = TimeSpan.FromSeconds(5);
        private MemoryCache _cache;
        private Timer _cacheEvicter;
        private ConcurrentCacheHelper _concurrentCacheHelper;
        private CounterFinder _counterFinder;

        public CounterProvider(CounterFinder finder)
        {
            //
            // Cache strategy
            // key: performance counter category, value: Counter Monitor
            // key: performance counter category + performance counter instance, value: list of performance counters
            // Cache stores one counter monitor per performance counter category
            // When performance counters are requested they are added to the monitor for their category and then cached under category + instance
            //     for quick retrieval and lifetime management
            _cache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = CacheExpiration,
                CompactOnMemoryPressure = false
            });

            _counterFinder = finder;

            _concurrentCacheHelper = new ConcurrentCacheHelper(_cache);

            _cacheEvicter = new Timer(TimerCallback, null, ScanFrequency, ScanFrequency);
        }

        public Task<IEnumerable<string>> GetInstances(string category)
        {
            return Task.FromResult(_counterFinder.GetInstances(category));
        }

        public async Task<IEnumerable<IPerfCounter>> GetCounters(string category, string instance, IEnumerable<string> counterNames)
        {
            string key = category + instance;

            CounterMonitor monitor = _concurrentCacheHelper.GetOrCreate<CounterMonitor>(category,
                () => new CounterMonitor(_counterFinder, Enumerable.Empty<IPerfCounter>()),
                new MemoryCacheEntryOptions() {
                    SlidingExpiration = CacheExpiration
                }.RegisterPostEvictionCallback(PostEvictionCallback)
            );
            
            IEnumerable<IPerfCounter> counters = _cache.Get<IEnumerable<IPerfCounter>>(key);

            if (counters == null || counters.Count() == 0) {

                counters = _counterFinder.GetCounters(category, instance, counterNames);

                if (counters.Count() > 0) {

                    bool didCreate = _concurrentCacheHelper.GetOrCreate(
                        key,
                        () => counters,
                        new MemoryCacheEntryOptions() {
                            SlidingExpiration = CacheExpiration
                        }.RegisterPostEvictionCallback(PostEvictionCallback),
                        out IEnumerable<IPerfCounter> entry
                    );

                    if (didCreate) {
                        monitor.AddCounters(entry);
                    }
                }
            }

            try {
                await monitor.Refresh();
            }
            catch (MissingCountersException e) {
                
                foreach (string counterKey in e.Counters.Select(c => c.CategoryName + c.InstanceName).Distinct()) {
                    //
                    // Cache eviction will remove counters from the counter monitor
                    _cache.Remove(counterKey);
                }

                if (e.Counters.Any(counter => 
                        counter.CategoryName.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                        counter.InstanceName.Equals(instance, StringComparison.OrdinalIgnoreCase))
                ) {
                    // If requested counter category+instance was reported missing we must throw
                    throw;
                }

                //
                // Some counters instances that were in the monitor for the category are gone now, but the requested instance is fine.
                // Repeat the request for counters now that the nonexistent counters have been cleaned away
                counters = await GetCounters(category, instance, counterNames);
            }

            return counters;
        }

        public async Task<IEnumerable<IPerfCounter>> GetSingletonCounters(string category, IEnumerable<string> counterNames)
        {
            //
            // Singleton counter categories do not have instances
            // caching them only requires a counter monitor

            CounterMonitor monitor = _cache.Get<CounterMonitor>(category);

            if (monitor == null) {

                IEnumerable<IPerfCounter> counters = _counterFinder.GetSingletonCounters(category, counterNames);

                monitor = _concurrentCacheHelper.GetOrCreate(
                    category,
                    () => new CounterMonitor(_counterFinder, counters),
                    new MemoryCacheEntryOptions() {
                        SlidingExpiration = CacheExpiration
                    }.RegisterPostEvictionCallback(PostEvictionCallback)
                );
            }

            try {
                await monitor.Refresh();
            }
            catch (MissingCountersException) {
                _cache.Remove(category);
                throw;
            }

            return monitor.Counters;
        }

        private void TimerCallback(object state)
        {
            //
            // Ping cache to evict any expired resources

            _cache.Get(string.Empty);
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            //
            // Handling of disposables is done through eviction from cache

            CounterMonitor monitor = value as CounterMonitor;

            if (monitor != null) {

                try {
                    foreach (var countersKey in monitor.Counters.ToList().Select(counter => counter.CategoryName + counter.InstanceName).Distinct()) {
                        _cache.Remove(countersKey);
                    }
                }
                finally {
                    monitor.Dispose();
                }

                return;
            }

            IEnumerable<IPerfCounter> counters = value as IEnumerable<IPerfCounter>;

            if (counters != null && counters.Count() > 0) {

                monitor = _cache.Get<CounterMonitor>(counters.First().CategoryName);

                if (monitor != null) {
                    monitor.RemoveCounters(counters);
                }

                return;
            }
        }

        public void Dispose()
        {
            //
            // Evict the entire cache to trigger handling of disposable resources
            _cache.Compact(100);

            if (_cacheEvicter != null) {
                _cacheEvicter.Dispose();
                _cacheEvicter = null;
            }

            if (_cache != null) {
                _cache.Dispose();
                _cache = null;
            }

            if (_concurrentCacheHelper != null) {
                _concurrentCacheHelper.Dispose();
                _concurrentCacheHelper = null;
            }
        }
    }
}
