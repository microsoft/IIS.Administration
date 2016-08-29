// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Transactions
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            Environment.Host.ApplicationBuilder.UseMiddleware<Middleware>();

            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "transactions" }, skipEdge:true);
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", trans => new { href = TransactionHelper.GetLocation(trans.id) });

            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }
    }
}
