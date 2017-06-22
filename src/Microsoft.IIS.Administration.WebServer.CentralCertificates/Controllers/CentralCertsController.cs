// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Files;
    using System.Net;
    using System.Threading.Tasks;


    [RequireWebServer]
    public class CentralCertsController : ApiBaseController
    {
        private const string HIDDEN_FIELDS = "model.identity.password,model.private_key_password";
        private CentralCertificateStore _ccs;
        private IFileProvider _fileProvider;

        public CentralCertsController(IFileProvider fileProvider)
        {
            _ccs = Startup.CentralCertificateStore;
            _fileProvider = fileProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        public object Get()
        {
            RequireEnabled();

            return LocationChanged(CentralCertHelper.GetLocation(), CentralCertHelper.ToJsonModel());
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        public object Get(string id)
        {
            RequireEnabled();

            if (!id.Equals(new CentralCertConfigId().Uuid)) {
                return NotFound();
            }

            return CentralCertHelper.ToJsonModel();
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            RequireEnabled();

            if (!id.Equals(new CentralCertConfigId().Uuid)) {
                return NotFound();
            }

            CentralCertHelper.Update(model, _fileProvider);

            return CentralCertHelper.ToJsonModel();
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.CentralCertsName)]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        public async Task<object> Post([FromBody] dynamic model)
        {
            await CentralCertHelper.Enable(model, _fileProvider);

            return CentralCertHelper.ToJsonModel();
        }

        [HttpDelete]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        public async Task Delete(string id)
        {
            if (id.Equals(new CentralCertConfigId().Uuid)) {
                await CentralCertHelper.Disable();
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }



        private void RequireEnabled()
        {
            if (!CentralCertHelper.FeatureEnabled || !_ccs.Enabled) {
                throw new FeatureNotFoundException("IIS Central Certificate Store");
            }
        }
    }
}
