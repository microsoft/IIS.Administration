// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Sites;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Newtonsoft.Json.Linq;
    using Core.Utils;
    using Core;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class QueryStringsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.QueryStringsName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return NotFound();
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            return new {
                query_strings = QueryStringsHelper.GetQueryStrings(site, reqId.Path).Select(s => QueryStringsHelper.ToJsonModelRef(s, site, reqId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.QueryStringName)]
        public object Get(string id)
        {
            QueryStringId queryStringId = new QueryStringId(id);

            Site site = queryStringId.SiteId == null ? null : SiteHelper.GetSite(queryStringId.SiteId.Value);

            if (queryStringId.SiteId != null && site == null) {
                return NotFound();
            }

            QueryStringRule queryString = QueryStringsHelper.GetQueryStrings(site, queryStringId.Path).FirstOrDefault(s => s.QueryString.ToString().Equals(queryStringId.QueryString));

            if (queryString == null) {
                return NotFound();
            }

            return QueryStringsHelper.ToJsonModel(queryString, site, queryStringId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.QueryStringName)]
        public object Post([FromBody] dynamic model)
        {
            QueryStringRule queryString = null;
            Site site = null;
            RequestFilteringId reqId = null;
            
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.request_filtering == null) {
                throw new ApiArgumentException("request_filtering");
            }
            if (!(model.request_filtering is JObject)) {
                throw new ApiArgumentException("request_filtering");
            }
            string reqUuid = DynamicHelper.Value(model.request_filtering.id);
            if (reqUuid == null) {
                throw new ApiArgumentException("request_filtering.id");
            }

            reqId = new RequestFilteringId(reqUuid);

            site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            queryString = QueryStringsHelper.CreateQueryString(model);

            QueryStringsHelper.AddQueryString(queryString, section);

            ManagementUnit.Current.Commit();
            
            //
            // Create response
            dynamic qs = QueryStringsHelper.ToJsonModel(queryString, site, reqId.Path);

            return Created(QueryStringsHelper.GetLocation(qs.id), qs);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.QueryStringName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            QueryStringId queryStringId = new QueryStringId(id);

            Site site = queryStringId.SiteId == null ? null : SiteHelper.GetSite(queryStringId.SiteId.Value);

            if (queryStringId.SiteId != null && site == null) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            QueryStringRule queryString = QueryStringsHelper.GetQueryStrings(site, queryStringId.Path).FirstOrDefault(s => s.QueryString.ToString().Equals(queryStringId.QueryString));

            if (queryString == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            QueryStringsHelper.UpdateQueryString(queryString, model, site, queryStringId.Path, configPath);

            ManagementUnit.Current.Commit();

            dynamic qs = QueryStringsHelper.ToJsonModel(queryString, site, queryStringId.Path);

            if(qs.id != id) {
                return LocationChanged(QueryStringsHelper.GetLocation(qs.id), qs);
            }

            return qs;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            QueryStringId queryStringId = new QueryStringId(id);

            Site site = queryStringId.SiteId == null ? null : SiteHelper.GetSite(queryStringId.SiteId.Value);

            if (queryStringId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            QueryStringRule queryString = QueryStringsHelper.GetQueryStrings(site, queryStringId.Path).FirstOrDefault(r => r.QueryString.ToString().Equals(queryStringId.QueryString));

            if (queryString != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, queryStringId.Path, ManagementUnit.ResolveConfigScope());

                QueryStringsHelper.DeleteQueryString(queryString, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }
    }
}
