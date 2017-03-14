// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class HiddenSegmentsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.HiddenSegmentsName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return NotFound();
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            List<HiddenSegment> segments = HiddenSegmentsHelper.getSegments(site, reqId.Path);

            return new {
                hidden_segments = segments.Select(s => HiddenSegmentsHelper.ToJsonModelRef(s, site, reqId.Path))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HiddenSegmentName)]
        public object Get(string id)
        {
            SegmentId segId = new SegmentId(id);

            Site site = segId.SiteId == null ? null : SiteHelper.GetSite(segId.SiteId.Value);

            if (segId.SiteId != null && site == null) {
                return NotFound();
            }

            HiddenSegment segment = HiddenSegmentsHelper.getSegments(site, segId.Path).Where(s => s.Segment.Equals(segId.Segment)).FirstOrDefault();

            if (segment == null) {
                return NotFound();
            }

            return HiddenSegmentsHelper.ToJsonModel(segment, site, segId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.HiddenSegmentName)]
        public object Post([FromBody] dynamic model)
        {
            HiddenSegment segment = null;
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

            // Get the feature id
            reqId = new RequestFilteringId(reqUuid);

            site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            segment = HiddenSegmentsHelper.CreateSegment(model, section);

            HiddenSegmentsHelper.AddSegment(segment, section);

            ManagementUnit.Current.Commit();

            dynamic hidden_segment = HiddenSegmentsHelper.ToJsonModel(segment, site, reqId.Path);
            return Created(HiddenSegmentsHelper.GetLocation(hidden_segment.id), hidden_segment);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            SegmentId segId = new SegmentId(id);

            Site site = segId.SiteId == null ? null : SiteHelper.GetSite(segId.SiteId.Value);

            if (segId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            HiddenSegment segment = HiddenSegmentsHelper.getSegments(site, segId.Path).Where(s => s.Segment.ToString().Equals(segId.Segment)).FirstOrDefault();

            if (segment != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, segId.Path, ManagementUnit.ResolveConfigScope());

                HiddenSegmentsHelper.DeleteSegment(segment, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }
    }
}
