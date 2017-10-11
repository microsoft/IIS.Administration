// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using System.Threading.Tasks;


    [ResourceInfo(Name=Defines.ResourceName)]
    public class WebServerController : ApiBaseController
    {
        [HttpGet]
        [RequireWebServer]
        public object Get() {
            return WebServerHelper.WebServerJsonModel();
        }

        [HttpGet]
        [RequireWebServer]
        public object Get(string id) {
            if(id != WebServerId.Create().Uuid) {
                return NotFound();
            }

            return WebServerHelper.WebServerJsonModel();
        }

        /*
        [HttpPost]
        [Audit]
        [RequireWebServer(false)]
        public async Task<object> Post() {
            //
            // Install
            await WebServerHelper.Install();

            dynamic srv = WebServerHelper.WebServerJsonModel();
            return Created(WebServerHelper.GetLocation(srv.id), srv);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id) {
            if (id == WebServerId.CreateFromPath(ManagementUnit.Current.ApplicationHostConfigPath).Uuid) {
                await WebServerHelper.Uninstall();
            }
        }
        */
    }
}
