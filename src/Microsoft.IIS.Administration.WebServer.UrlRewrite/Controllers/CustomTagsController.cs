// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Http;
    using Microsoft.IIS.Administration.Core.Utils;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System;
    using System.Linq;
    using System.Net;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class CustomTagsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.CustomTagsName)]
        public object Get()
        {
            string outboundRulesId = Context.Request.Query[Defines.OUTBOUND_RULES_SECTION_IDENTIFIER];

            if (string.IsNullOrEmpty(outboundRulesId))
            {
                outboundRulesId = Context.Request.Query[Defines.IDENTIFIER];
            }

            if (string.IsNullOrEmpty(outboundRulesId))
            {
                return NotFound();
            }

            var sectionId = new RewriteId(outboundRulesId);

            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            TagsCollection tags = OutboundRulesHelper.GetSection(site, sectionId.Path).Tags;

            this.Context.Response.SetItemsCount(tags.Count());

            return new
            {
                entries = tags.Select(tag => OutboundRulesHelper.TagsToJsonModelRef(tag, site, sectionId.Path, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.CustomTagName)]
        public object Get(string id)
        {
            var customTagsId = new CustomTagsId(id);

            Site site = customTagsId.SiteId == null ? null : SiteHelper.GetSite(customTagsId.SiteId.Value);

            if (customTagsId.SiteId != null && site == null)
            {
                return NotFound();
            }

            TagsElement tag = OutboundRulesHelper.GetSection(site, customTagsId.Path).Tags.FirstOrDefault(tags => tags.Name.Equals(customTagsId.Name, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
            {
                return NotFound();
            }

            return OutboundRulesHelper.TagsToJsonModel(tag, site, customTagsId.Path, Context.Request.GetFields());
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.CustomTagName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var customTagsId = new CustomTagsId(id);

            Site site = customTagsId.SiteId == null ? null : SiteHelper.GetSite(customTagsId.SiteId.Value);

            if (customTagsId.SiteId != null && site == null)
            {
                return NotFound();
            }

            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, customTagsId.Path);
            TagsElement tags = section.Tags.FirstOrDefault(t => t.Name.Equals(customTagsId.Name, StringComparison.OrdinalIgnoreCase));

            if (tags == null)
            {
                return NotFound();
            }

            OutboundRulesHelper.UpdateCustomTags(model, tags, section);

            ManagementUnit.Current.Commit();

            dynamic updatedCustomTags = OutboundRulesHelper.TagsToJsonModel(tags, site, customTagsId.Path, Context.Request.GetFields(), true);

            if (updatedCustomTags.id != id)
            {
                return LocationChanged(OutboundRulesHelper.GetCustomTagsLocation(updatedCustomTags.id), updatedCustomTags);
            }

            return updatedCustomTags;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.CustomTagName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null)
            {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model) ?? OutboundRulesHelper.GetSectionIdFromBody(model);

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            OutboundRulesSection section = OutboundRulesHelper.GetSection(site, parentId.Path, configPath);

            TagsElement tags = OutboundRulesHelper.CreateCustomTags(model, section);

            OutboundRulesHelper.AddCustomTags(tags, section);

            ManagementUnit.Current.Commit();

            dynamic pc = OutboundRulesHelper.TagsToJsonModel(tags, site, parentId.Path, Context.Request.GetFields(), true);
            return Created(OutboundRulesHelper.GetCustomTagsLocation(pc.id), pc);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            TagsElement tags = null;
            var tagsId = new CustomTagsId(id);

            Site site = tagsId.SiteId == null ? null : SiteHelper.GetSite(tagsId.SiteId.Value);

            if (tagsId.SiteId == null || site != null)
            {
                tags = OutboundRulesHelper.GetSection(site, tagsId.Path).Tags.FirstOrDefault(t => t.Name.Equals(tagsId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (tags != null)
            {
                var section = OutboundRulesHelper.GetSection(site, tagsId.Path, ManagementUnit.ResolveConfigScope());

                OutboundRulesHelper.DeleteCustomTags(tags, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
