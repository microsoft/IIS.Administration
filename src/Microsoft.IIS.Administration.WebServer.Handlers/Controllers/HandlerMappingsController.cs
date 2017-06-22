// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
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


    [RequireWebServer]
    public class HandlerMappingsController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.EntriesName)]
        public object Get()
        {
            string HandlersUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(HandlersUuid)) {
                return NotFound();
            }

            HandlersId id = new HandlersId(HandlersUuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<Mapping> mappings = MappingsHelper.GetMappings(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(mappings.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                entries = mappings.Select(mapping => MappingsHelper.ToJsonModelRef(mapping, site, id.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Get(string id)
        {
            MappingId mappingId = new MappingId(id);

            Site site = mappingId.SiteId == null ? null : SiteHelper.GetSite(mappingId.SiteId.Value);

            if (mappingId.SiteId != null && site == null) {
                return NotFound();
            }

            Mapping mapping = MappingsHelper.GetMappings(site, mappingId.Path).FirstOrDefault(u => u.Name.Equals(mappingId.Name));

            if (mapping == null) {
                return NotFound();
            }

            return MappingsHelper.ToJsonModel(mapping, site, mappingId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Post([FromBody] dynamic model)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.handler == null || !(model.handler is JObject)) {
                throw new ApiArgumentException("handler");
            }
            string handlersUuid = DynamicHelper.Value(model.handler.id);
            if (handlersUuid == null) {
                throw new ApiArgumentException("handler.id");
            }

            // Get the feature id
            HandlersId handlersId = new HandlersId(handlersUuid);

            Site site = handlersId.SiteId == null ? null : SiteHelper.GetSite(handlersId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            HandlersSection section = HandlersHelper.GetHandlersSection(site, handlersId.Path, configPath);

            Mapping mapping = MappingsHelper.CreateMapping(model, section);

            MappingsHelper.AddMapping(mapping, section);

            ManagementUnit.Current.Commit();

            dynamic m = MappingsHelper.ToJsonModel(mapping, site, handlersId.Path);

            return Created(MappingsHelper.GetLocation(m.id), m);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.EntryName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            MappingId mappingId = new MappingId(id);

            Site site = mappingId.SiteId == null ? null : SiteHelper.GetSite(mappingId.SiteId.Value);

            if (mappingId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = ManagementUnit.ResolveConfigScope(model);
            HandlersSection section = HandlersHelper.GetHandlersSection(site, mappingId.Path, configPath);
            Mapping mapping = section.Mappings.FirstOrDefault(u => u.Name.Equals(mappingId.Name));

            if (mapping == null) {
                return NotFound();
            }

            MappingsHelper.UpdateMapping(model, mapping, section);

            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic m = MappingsHelper.ToJsonModel(mapping, site, mappingId.Path);

            if (m.id != id) {
                return LocationChanged(MappingsHelper.GetLocation(m.id), m);
            }

            return m;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            MappingId mappingId = new MappingId(id);

            Site site = mappingId.SiteId == null ? null : SiteHelper.GetSite(mappingId.SiteId.Value);

            if (mappingId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }
            
            Mapping mapping = MappingsHelper.GetMappings(site, mappingId.Path).FirstOrDefault(u => u.Name.Equals(mappingId.Name));

            if (mapping != null) {
                var section = HandlersHelper.GetHandlersSection(site, mappingId.Path, ManagementUnit.ResolveConfigScope());

                MappingsHelper.DeleteMapping(mapping, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
