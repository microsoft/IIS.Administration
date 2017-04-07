// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;

    public class CentralCertsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        public object Get()
        {
            return CentralCertHelper.ToJsonModel();
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        public object Get(string id)
        {
            if (!id.Equals(new CentralCertId().Uuid)) {
                return NotFound();
            }

            return CentralCertHelper.ToJsonModel();
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (!id.Equals(new CentralCertId().Uuid)) {
                return NotFound();
            }

            CentralCertHelper.Update(model);

            return CentralCertHelper.ToJsonModel();
        }
    }
}
