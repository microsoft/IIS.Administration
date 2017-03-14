// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(AuthorizationHelper.MODULE, "Authorization")]
    public class RulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.RulesName)]
        public object Get()
        {
            string authUuid = Context.Request.Query[Defines.AUTHORIZATION_IDENTIFIER];

            if (string.IsNullOrEmpty(authUuid)) {
                return NotFound();
            }

            AuthorizationId id = new AuthorizationId(authUuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<Rule> rules = AuthorizationHelper.GetRules(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(rules.Count);

            Fields fields = Context.Request.GetFields();

            return new {                
                rules = rules.Select(rule => AuthorizationHelper.RuleToJsonModelRef(rule, site, id.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Get(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            Rule rule = AuthorizationHelper.GetRule(site, ruleId.Path, ruleId.Users, ruleId.Roles, ruleId.Verbs);

            if (rule == null) {
                return NotFound();
            }

            return AuthorizationHelper.RuleToJsonModel(rule, site, ruleId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Post([FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.authorization == null || !(model.authorization is JObject)) {
                throw new ApiArgumentException("authorization");
            }

            string authorizationUuid = DynamicHelper.Value(model.authorization.id);
            if (authorizationUuid == null) {
                throw new ApiArgumentException("authorization.id");
            }

            // Get the feature id
            AuthorizationId authId = new AuthorizationId(authorizationUuid);
            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            if (authId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = ManagementUnit.ResolveConfigScope(model);
            var section = AuthorizationHelper.GetSection(site, authId.Path, configPath);

            Rule rule = AuthorizationHelper.CreateRule(model, section);

            if(AuthorizationHelper.GetRule(site, authId.Path, rule.Users, rule.Roles, rule.Verbs) != null) {
                throw new AlreadyExistsException("rule");
            }

            section.Rules.Add(rule.AccessType, rule.Users, rule.Roles, rule.Verbs);

            ManagementUnit.Current.Commit();

            dynamic r = AuthorizationHelper.RuleToJsonModel(rule, site, authId.Path);

            return Created(AuthorizationHelper.GetRuleLocation(r.id), r);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.RuleName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            Rule rule = AuthorizationHelper.GetRule(site, ruleId.Path, ruleId.Users, ruleId.Roles, ruleId.Verbs);

            if (rule == null) {
                return NotFound();
            }

            rule = AuthorizationHelper.UpdateRule(rule, model);

            ManagementUnit.Current.Commit();

            dynamic r = AuthorizationHelper.RuleToJsonModel(rule, site, ruleId.Path);

            if (r.id != id) {
                return LocationChanged(AuthorizationHelper.GetRuleLocation(r.id), r);
            };

            return r;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            Rule rule = AuthorizationHelper.GetRule(site, ruleId.Path, ruleId.Users, ruleId.Roles, ruleId.Verbs);

            if (rule != null) {

                var section = AuthorizationHelper.GetSection(site, ruleId.Path, ManagementUnit.ResolveConfigScope());

                AuthorizationHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return;
        }
    }
}
