// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;

    public class FileRedirectService : IFileRedirectService
    {
        private Dictionary<string, IRedirect> _redirects = new Dictionary<string, IRedirect>();

        public void AddRedirect(string fileName, Func<string> redirectBuilder, bool permanent)
        {
            _redirects.Add(fileName.ToLower(), new Redirect {
                Name = fileName.ToLower(),
                To = redirectBuilder,
                Permanent = permanent
            });
        }

        public IRedirect GetRedirect(string fileName)
        {
            var key = fileName.ToLower();
            return _redirects.ContainsKey(key) ? _redirects[key] : null;
        }

        private class Redirect : IRedirect
        {
            public string Name { get; set; }
            public Func<string> To { get; set; }
            public bool Permanent { get; set; }
        }
    }
}
