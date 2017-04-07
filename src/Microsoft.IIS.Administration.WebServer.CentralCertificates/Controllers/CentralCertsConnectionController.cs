namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using System;
    using System.Net;

    public class CentralCertsConnectionController : ApiBaseController
    {
        [HttpPost]
        [ResourceInfo(Name = Defines.ConnectionTestName)]
        public object Post()
        {
            Context.Response.StatusCode = (int) HttpStatusCode.Created;

            return new {
                id = Uuid.Encode(Guid.NewGuid().ToString(), "WebServer.CentralCertificates.Connection"),
                success = CentralCertHelper.TestConnection()
            };
        }
    }
}
