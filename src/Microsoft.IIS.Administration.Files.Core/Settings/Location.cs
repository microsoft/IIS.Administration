// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using System.Collections.Generic;

    class Location : ILocation
    {
        public string Alias { get; set; }
        public string Path { get; set; }
        public IList<string> Claims { get; set; } = new List<string>();

        public static Location FromSection(IConfigurationSection section)
        {
            string alias = section.GetValue("alias", string.Empty);
            string path = section.GetValue<string>("path");
            IList<string> claims = new List<string>();
            ConfigurationBinder.Bind(section.GetSection("claims"), claims);

            Location location = null;

            if (!string.IsNullOrEmpty(path)) {
                location = new Location() {
                    Alias = alias,
                    Path = path,
                    Claims = claims
                };
            }

            return location;
        }
    }
}
