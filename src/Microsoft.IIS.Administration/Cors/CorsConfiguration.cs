// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Cors
{
    using System.Collections.Generic;

    class CorsConfiguration : ICorsConfiguration
    {
        public IEnumerable<Rule> Rules { get; set; }
    }
}
