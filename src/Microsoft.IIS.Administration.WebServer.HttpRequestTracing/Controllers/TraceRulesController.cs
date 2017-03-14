// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(Helper.TRACING_MODULE, Helper.DISPLAY_NAME)]
    [RequireGlobalModule(Helper.FAILED_REQUEST_TRACING_MODULE, Helper.DISPLAY_NAME)]
    public class TraceRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.RulesName)]
        public object Get()
        {
            string hrtUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(hrtUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }
            
            HttpRequestTracingId hrtId = new HttpRequestTracingId(hrtUuid);           

            Site site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            var rules = RulesHelper.GetRules(site, hrtId.Path);

            this.Context.Response.SetItemsCount(rules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                rules = rules.Select(r => RulesHelper.ToJsonModelRef(r, site, hrtId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Get(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                return NotFound();
            }

            TraceRule rule = RulesHelper.GetRules(site, ruleId.AppPath).Where(r => r.Path.Equals(ruleId.Path)).FirstOrDefault();

            if(rule == null) {
                return NotFound();
            }

            return RulesHelper.ToJsonModel(rule, site, ruleId.AppPath);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Post(dynamic model)
        {
            TraceRule rule = null;
            Site site = null;
            HttpRequestTracingId hrtId = null;

            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.request_tracing == null) {
                throw new ApiArgumentException("request_tracing");
            }
            if (!(model.request_tracing is JObject)) {
                throw new ApiArgumentException("request_tracing", ApiArgumentException.EXPECTED_OBJECT);
            }
            string hrtUuid = DynamicHelper.Value(model.request_tracing.id);
            if (hrtUuid == null) {
                throw new ApiArgumentException("request_tracing.id");
            }

            hrtId = new HttpRequestTracingId(hrtUuid);

            site = hrtId.SiteId == null ? null : SiteHelper.GetSite(hrtId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);

            rule = RulesHelper.CreateRule(model, site, hrtId.Path, configPath);

            var section = Helper.GetTraceFailedRequestsSection(site, hrtId.Path, configPath);
            RulesHelper.AddRule(rule, section);

            ManagementUnit.Current.Commit();

            dynamic r = RulesHelper.ToJsonModel(rule, site, hrtId.Path);
            return Created(RulesHelper.GetLocation(r.id), r);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Patch(string id, dynamic model)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                return NotFound();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string configPath = ManagementUnit.ResolveConfigScope(model);
            TraceRule rule = RulesHelper.GetRules(site, ruleId.AppPath, configPath).Where(r => r.Path.ToString().Equals(ruleId.Path)).FirstOrDefault();

            if (rule == null) {
                return NotFound();
            }


            rule = RulesHelper.UpdateRule(rule, model, site, ruleId.AppPath, configPath);

            ManagementUnit.Current.Commit();

            dynamic rle = RulesHelper.ToJsonModel(rule, site, ruleId.AppPath);

            if (rle.id != id) {
                return LocationChanged(RulesHelper.GetLocation(rle.id), rle);
            }

            return rle;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);
            Application app = ApplicationHelper.GetApplication(ruleId.AppPath, site);

            if (ruleId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            TraceRule rule = RulesHelper.GetRules(site, ruleId.AppPath).Where(r => r.Path.ToString().Equals(ruleId.Path)).FirstOrDefault();

            if (rule != null) { 

                var section = Helper.GetTraceFailedRequestsSection(site, ruleId.AppPath, ManagementUnit.ResolveConfigScope());

                RulesHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
