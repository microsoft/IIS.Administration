// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Scm
{
    using AspNetCore.Mvc;
    using Core.Utils;
    using Core.Http;
    using Core;

    [RequireWebServer]
    public class ScmController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ServiceControllerName)]
        public object Get()
        {
            if (!ScmHelper.IsInstalled())
            {
                return NotFound();
            }

            return ScmHelper.ToJsonModel();
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.ServiceControllerName)]
        public object Patch(dynamic model)
        {
            if (!ScmHelper.IsInstalled())
            {
                return NotFound();
            }

            if (model.status != null)
            {
                Status status = DynamicHelper.To<Status>(model.status);
                switch (status)
                {
                    case Status.Started:
                        ScmHelper.Start();
                        break;
                    case Status.Stopped:
                        ScmHelper.Stop();
                        break;
                    default:
                        break;
                }
            }

            return ScmHelper.ToJsonModel();
        }
    }
}
