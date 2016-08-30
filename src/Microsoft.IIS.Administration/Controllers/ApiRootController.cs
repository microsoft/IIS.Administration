// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using Core;
    using AspNetCore.Mvc;
    using Core.Http;
    using System;
    using Extensions.Configuration;

    public class ApiRootController : ApiBaseController
    {
        private const string ApiRootName = "Microsoft.WebServer.Api";
        private IConfiguration _config;

        public ApiRootController(IConfiguration configuration)
        {
            if(configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _config = configuration;
        }

        [HttpGet]
        [ResourceInfo(Name = ApiRootName)]
        public object Get()
        {
            string host = _config.GetValue<string>("host_name", System.Environment.GetEnvironmentVariable("COMPUTERNAME"));

            // Initialize an empty expandable object for adding properties for the json model
            var obj = new {
                host_name = host
            };

            // Apply HAL to the json model
            return Core.Environment.Hal.Apply(Globals.ApiResource.Guid, obj);
        }
    }
}
