// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Delegation
{
    using Applications;
    using AspNetCore.Mvc;
    using Sites;
    using System.Linq;
    using Web.Administration;
    using Core.Http;
    using Core;


    [RequireWebServer]
    public class DelegationController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.DelegationsName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();
            string configScope = ManagementUnit.ResolveConfigScope();

            return new {
                sections = DelegationHelper.GetWebServerSections(site, path, configScope)
                                           .Select(section => DelegationHelper.SectionToJsonModelRef(section, site, path, configScope))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.DelegationName)]
        public object Get(string id)
        {
            SectionId sectionId = new SectionId(id);
            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            Configuration configuration = ManagementUnit.GetConfiguration(site?.Id, sectionId.Path);

            ConfigurationSection section = configuration.GetSection($"{DelegationHelper.XPATH}/{sectionId.SectionPath}");

            if(section == null) {
                return NotFound();
            }

            return DelegationHelper.SectionToJsonModel(section, site, sectionId.Path, sectionId.ConfigScope);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.DelegationName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            SectionId sectionId = new SectionId(id);
            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            string configScope = ManagementUnit.ResolveConfigScope(model);

            Configuration configuration = ManagementUnit.GetConfiguration(site?.Id, sectionId.Path, configScope);
            string location = ManagementUnit.GetLocationTag(site?.Id, sectionId.Path, configScope);

            string fullSectionPath = $"{DelegationHelper.XPATH}/{sectionId.SectionPath}";
            ConfigurationSection section = location == null ? configuration.GetSection(fullSectionPath) : configuration.GetSection(fullSectionPath, location);

            if (section == null) {
                return NotFound();
            }

            DelegationHelper.Update(section, model);

            ManagementUnit.Current.Commit();

            // Refresh the section
            configuration = ManagementUnit.GetConfiguration(site?.Id, sectionId.Path);
            section = configuration.GetSection(fullSectionPath);

            return DelegationHelper.SectionToJsonModel(section, site, sectionId.Path, configScope);
        }
    }
}
