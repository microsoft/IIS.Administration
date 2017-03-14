// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Sites;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Core;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class HeaderLimitsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.HeaderLimitsName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return NotFound();
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            List<HeaderLimit> headers = HeaderLimitsHelper.GetHeaderLimits(site, reqId.Path);

            return new {
                header_limits = headers.Select(h => HeaderLimitsHelper.ToJsonModelRef(h, site, reqId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HeaderLimitName)]
        public object Get(string id)
        {
            HeaderLimitId headerId = new HeaderLimitId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                return NotFound();
            }

            HeaderLimit headerLimit = HeaderLimitsHelper.GetHeaderLimits(site, headerId.Path).FirstOrDefault(h => h.Header.Equals(headerId.Header));

            if (headerLimit == null) {
                return NotFound();
            }

            return HeaderLimitsHelper.ToJsonModel(headerLimit, site, headerId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.HeaderLimitName)]
        public object Post([FromBody] dynamic model)
        {
            HeaderLimit headerLimit = null;
            Site site = null;
            RequestFilteringId reqId = null;

            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.request_filtering == null) {
                throw new ApiArgumentException("request_filtering");
            }
            if (!(model.request_filtering is JObject)) {
                throw new ApiArgumentException(String.Empty, "request_filtering");
            }
            string reqUuid = DynamicHelper.Value(model.request_filtering.id);
            if (reqUuid == null) {
                throw new ApiArgumentException("request_filtering.id");
            }

            reqId = new RequestFilteringId(reqUuid);

            site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            headerLimit = HeaderLimitsHelper.CreateHeaderLimit(model, section);

            HeaderLimitsHelper.AddHeaderLimit(headerLimit, section);

            ManagementUnit.Current.Commit();

            dynamic header_limit = HeaderLimitsHelper.ToJsonModel(headerLimit, site, reqId.Path);
            return Created(HeaderLimitsHelper.GetLocation(header_limit.id), header_limit);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.HeaderLimitName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            HeaderLimitId headerLimitId = new HeaderLimitId(id);

            Site site = headerLimitId.SiteId == null ? null : SiteHelper.GetSite(headerLimitId.SiteId.Value);

            if (headerLimitId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            HeaderLimit headerLimit = HeaderLimitsHelper.GetHeaderLimits(site, headerLimitId.Path, configPath)
                .FirstOrDefault(h => h.Header.ToString().Equals(headerLimitId.Header));

            if (headerLimit == null) {
                return NotFound();
            }
            
            headerLimit = HeaderLimitsHelper.UpdateHeaderLimit(headerLimit, model);

            ManagementUnit.Current.Commit();

            dynamic head = HeaderLimitsHelper.ToJsonModel(headerLimit, site, headerLimitId.Path);

            if(head.id != id) {
                return LocationChanged(HeaderLimitsHelper.GetLocation(head.id), head);
            }

            return head;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            HeaderLimitId headerId = new HeaderLimitId(id);

            Site site = headerId.SiteId == null ? null : SiteHelper.GetSite(headerId.SiteId.Value);

            if (headerId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            HeaderLimit headerLimit = HeaderLimitsHelper.GetHeaderLimits(site, headerId.Path).Where(h => h.Header.ToString().Equals(headerId.Header)).FirstOrDefault();

            if (headerLimit != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, headerId.Path, ManagementUnit.ResolveConfigScope());

                HeaderLimitsHelper.DeleteHeaderLimit(headerLimit, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
