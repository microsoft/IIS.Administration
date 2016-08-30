// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Cors
{
    using Extensions.Configuration;
    using System.Collections.Generic;

    class CorsConfiguration
    {
        public IEnumerable<Rule> Rules { get; set; }

        public CorsConfiguration(IConfiguration configuration)
        {
            Rules = new List<Rule>();
            ConfigurationBinder.Bind(configuration.GetSection("cors:rules"), Rules);
        }
    }
}