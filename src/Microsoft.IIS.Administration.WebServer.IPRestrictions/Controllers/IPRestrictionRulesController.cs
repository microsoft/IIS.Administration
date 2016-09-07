// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using AspNetCore.Mvc;
    using Web.Administration;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Sites;
    using System.Net;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Core.Http;


    [RequireGlobalModule("IpRestrictionModule", "IP and Domain Restrictions")]
    public class IPRestrictionRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.EntriesName)]
        public object Get()
        {
            string ipRestrictionUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(ipRestrictionUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            IPRestrictionId ipId = new IPRestrictionId(ipRestrictionUuid);

            // Get site rule is for if applicable
            Site site = ipId.SiteId == null ? null : SiteHelper.GetSite(ipId.SiteId.Value);

            List<Rule> rules = IPRestrictionsHelper.GetRules(site, ipId.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(rules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                entries = rules.Select(rule => IPRestrictionsHelper.RuleToJsonModelRef(rule, site, ipId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Get(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                // The rule id specified a site but we couldn't find it, therefore we can't get the rule
                return NotFound();
            }

            Rule rule = IPRestrictionsHelper.GetRules(site, ruleId.Path).Where(r => r.IpAddress.ToString().Equals(ruleId.IpAddress)).FirstOrDefault();

            if (rule == null) {
                return NotFound();
            }

            return IPRestrictionsHelper.RuleToJsonModel(rule, site, ruleId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Post([FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Rule must be created for a specific ip restriction section
            if (model.ip_restriction == null) {
                throw new ApiArgumentException("ip_restriction");
            }
            if (!(model.ip_restriction is JObject)) {
                throw new ApiArgumentException("ip_restriction");
            }
            string ipUuid = DynamicHelper.Value(model.ip_restriction.id);
            if(ipUuid == null) {
                throw new ApiArgumentException("ip_restriction.id");
            }

            // Get the ip restriction feature id
            IPRestrictionId ipId = new IPRestrictionId(ipUuid);

            // Get site
            Site site = ipId.SiteId == null ? null : SiteHelper.GetSite(ipId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            IPSecuritySection section = IPRestrictionsHelper.GetSection(site, ipId.Path, configPath);

            // Create ip restriction rule
            Rule rule = IPRestrictionsHelper.CreateRule(model, section);

            // Add it
            IPRestrictionsHelper.AddRule(rule, section);

            // Save
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic r = IPRestrictionsHelper.RuleToJsonModel(rule, site, ipId.Path);
            return Created(IPRestrictionsHelper.GetRuleLocation(r.id), r);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                // The rule id specified a site but we couldn't find it, therefore we can't get the rule
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            var section = IPRestrictionsHelper.GetSection(site, ruleId.Path, configPath);
            Rule rule = section.IpAddressFilters.Where(r => r.IpAddress.ToString().Equals(ruleId.IpAddress)).FirstOrDefault();

            if (rule == null) {
                return NotFound();
            }
            
            rule = IPRestrictionsHelper.SetRule(rule, model, section);

            ManagementUnit.Current.Commit();

            dynamic rle = IPRestrictionsHelper.RuleToJsonModel(rule, site, ruleId.Path);

            if (rle.id != id) {
                return LocationChanged(IPRestrictionsHelper.GetRuleLocation(rle.id), rle);
            }

            return rle;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                // The rule id specified a site but we couldn't find it, therefore we can't get the rule
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            Rule rule = IPRestrictionsHelper.GetRules(site, ruleId.Path).Where(r => r.IpAddress.ToString().Equals(ruleId.IpAddress)).FirstOrDefault();

            if (rule !=  null) {

                var section = IPRestrictionsHelper.GetSection(site, ruleId.Path, ManagementUnit.ResolveConfigScope());

                IPRestrictionsHelper.DeleteRule(rule, section);

                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
