// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Microsoft.AspNetCore.Authorization;
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    [Authorize(Policy = "System")]
    public class LocationsController : ApiBaseController
    {
        LocationsHelper _helper;

        public LocationsController(IFileOptions options, IConfigurationWriter configurationWriter)
        {
            _helper = new LocationsHelper(options, configurationWriter);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.LocationsName)]
        public object Get()
        {
            return new {
                locations = _helper.Options.Locations.Select(l => _helper.ToJsonModel(l))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.LocationsName)]
        public object Get(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            ILocation location = _helper.Options.Locations.FirstOrDefault(l => l.Path.Equals(fileId.PhysicalPath, StringComparison.OrdinalIgnoreCase));

            if (location == null) {
                return NotFound();
            }

            return _helper.ToJsonModel(location);
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.LocationsName)]
        [Audit(AuditAttribute.ALL)]
        public async Task<object> Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = FileId.FromUuid(id);

            ILocation location = _helper.Options.Locations.FirstOrDefault(l => l.Path.Equals(fileId.PhysicalPath, StringComparison.OrdinalIgnoreCase));

            if (location == null) {
                return NotFound();
            }

            location = await _helper.UpdateLocation(model, location);

            dynamic locModel = _helper.ToJsonModel(location);

            if (locModel.id != id) {
                return LocationChanged(_helper.GetLocationPath(locModel.id), locModel);
            }

            return locModel;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.LocationsName)]
        [Audit(AuditAttribute.ALL)]
        public async Task<object> Post([FromBody] dynamic model)
        {
            ILocation location = _helper.CreateLocation(model);

            //
            // Refresh location to newly added version
            location = await _helper.AddLocation(location);

            dynamic locModel = _helper.ToJsonModel(location);
            return Created(_helper.GetLocationPath(locModel.id), locModel);
        }

        [HttpDelete]
        [Audit(AuditAttribute.ALL)]
        public async Task Delete(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            ILocation location = _helper.Options.Locations.FirstOrDefault(l => l.Path.Equals(fileId.PhysicalPath, StringComparison.OrdinalIgnoreCase));

            if (location != null) {
                await _helper.RemoveLocation(location);
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
