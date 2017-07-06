// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Sites;
    using System;
    using System.Linq;
    using System.Net;
    using Web.Administration;


    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class PreConditionsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.PreConditionsName)]
        public object Get()
        {
            string outboundRulesId = Context.Request.Query[Defines.OUTBOUND_RULES_SECTION_IDENTIFIER];

            if (string.IsNullOrEmpty(outboundRulesId)) {
                outboundRulesId = Context.Request.Query[Defines.IDENTIFIER];
            }

            if (string.IsNullOrEmpty(outboundRulesId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(outboundRulesId);

            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            PreConditionCollection preconditions = OutboundRulesHelper.GetSection(site, sectionId.Path).PreConditions;

            this.Context.Response.SetItemsCount(preconditions.Count());

            Fields fields = Context.Request.GetFields();

            return new
            {
                entries = preconditions.Select(precondition => OutboundRulesHelper.PreConditionToJsonModelRef(precondition, site, sectionId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.PreConditionName)]
        public object Get(string id)
        {
            var preConditionId = new PreConditionId(id);

            Site site = preConditionId.SiteId == null ? null : SiteHelper.GetSite(preConditionId.SiteId.Value);

            if (preConditionId.SiteId != null && site == null) {
                return NotFound();
            }

            PreCondition precondition = OutboundRulesHelper.GetSection(site, preConditionId.Path).PreConditions.FirstOrDefault(pc => pc.Name.Equals(preConditionId.Name, StringComparison.OrdinalIgnoreCase));

            if (precondition == null) {
                return NotFound();
            }

            return OutboundRulesHelper.PreConditionToJsonModel(precondition, site, preConditionId.Path);
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.PreConditionName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var preConditionId = new PreConditionId(id);

            Site site = preConditionId.SiteId == null ? null : SiteHelper.GetSite(preConditionId.SiteId.Value);

            if (preConditionId.SiteId != null && site == null) {
                return NotFound();
            }

            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, preConditionId.Path);
            PreCondition precondition = (PreCondition)section.PreConditions.FirstOrDefault(pc => pc.Name.Equals(preConditionId.Name, StringComparison.OrdinalIgnoreCase));

            if (precondition == null) {
                return NotFound();
            }

            OutboundRulesHelper.UpdatePreCondition(model, precondition, section);

            ManagementUnit.Current.Commit();

            dynamic updatedPreCondition = OutboundRulesHelper.PreConditionToJsonModel(precondition, site, preConditionId.Path, null, true);

            if (updatedPreCondition.id != id) {
                return LocationChanged(OutboundRulesHelper.GetPreConditionLocation(updatedPreCondition.id), updatedPreCondition);
            }

            return updatedPreCondition;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.PreConditionName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model) ?? OutboundRulesHelper.GetSectionIdFromBody(model);

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, parentId.Path, configPath);

            PreCondition precondition = OutboundRulesHelper.CreatePreCondition(model, section);

            OutboundRulesHelper.AddPreCondition(precondition, section);

            ManagementUnit.Current.Commit();

            //
            dynamic pc = OutboundRulesHelper.PreConditionToJsonModel(precondition, site, parentId.Path, null, true);
            return Created(OutboundRulesHelper.GetRuleLocation(pc.id), pc);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            PreCondition preCondition = null;
            var preConditionId = new PreConditionId(id);

            Site site = preConditionId.SiteId == null ? null : SiteHelper.GetSite(preConditionId.SiteId.Value);

            if (preConditionId.SiteId == null || site != null) {
                preCondition = OutboundRulesHelper.GetSection(site, preConditionId.Path).PreConditions.FirstOrDefault(pc => pc.Name.Equals(preConditionId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (preCondition != null) {
                var section = OutboundRulesHelper.GetSection(site, preConditionId.Path, ManagementUnit.ResolveConfigScope());

                OutboundRulesHelper.DeletePreCondition(preCondition, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
