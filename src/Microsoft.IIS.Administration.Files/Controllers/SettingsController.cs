// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Microsoft.AspNetCore.Authorization;
    using System.Threading.Tasks;

    [Authorize(Policy = "System")]
    public class SettingsController : ApiBaseController
    {
        IConfigurationWriter _configWriter;
        IFileOptions _options;

        public SettingsController(IFileOptions options, IConfigurationWriter configurationWriter)
        {
            _options = options;
            _configWriter = configurationWriter;
        }

        [HttpGet]
        [ResourceInfo(Name = "Microsoft.IIS.Administration.Files.Settings")]
        public object Get()
        {
            return SettingsHelper.ToJsonModel(_options);
        }

        [HttpGet]
        [ResourceInfo(Name = "Microsoft.IIS.Administration.Files.Settings")]
        public object Get(string id)
        {
            return SettingsHelper.ToJsonModel(_options);
        }

        [HttpPatch]
        [ResourceInfo(Name = "Microsoft.IIS.Administration.Files.Settings")]
        public async Task<object> Patch([FromBody] dynamic model)
        {
            await SettingsHelper.UpdateSettings(model, _configWriter, _options);

            return SettingsHelper.ToJsonModel(_options);
        }
    }
}
