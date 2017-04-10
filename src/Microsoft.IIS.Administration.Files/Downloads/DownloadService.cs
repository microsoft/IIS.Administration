// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;

    class DownloadService : IDownloadService
    {
        private Dictionary<string, IDownload> _downloads = new Dictionary<string, IDownload>();
        private IMemoryCache _cache;

        public DownloadService(IMemoryCache cache)
        {
            if (cache == null) {
                throw new ArgumentNullException(nameof(cache));
            }

            _cache = cache;
        }

        public IDownload Create(string physicalPath, int? timeout)
        {
            if (physicalPath == null) {
                throw new ArgumentNullException(nameof(physicalPath));
            }

            var dl = new Download()
            {
                PhysicalPath = physicalPath
            };

            _cache.Set(dl.Id, dl, new MemoryCacheEntryOptions() {
                AbsoluteExpiration = timeout != null ? DateTimeOffset.UtcNow.AddMilliseconds((double)timeout) : (DateTimeOffset?)null
            });

            return dl;
        }

        public void Remove(string id)
        {
            _downloads.Remove(id);
        }

        public IDownload Get(string id)
        {
            IDownload dl = null;
            _cache.TryGetValue(id, out dl);
            return dl;
        }
    }
}
