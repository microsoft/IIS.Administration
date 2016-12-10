// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public static class MappingsHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "path");

        public static List<Mapping> GetMappings(Site site, string path, string configPath = null)
        {
            HandlersSection section = HandlersHelper.GetHandlersSection(site, path, configPath);

            return section.Mappings.ToList();
        }

        public static Mapping CreateMapping(dynamic model, HandlersSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            string name = DynamicHelper.Value(model.name);
            if (string.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }
            if (string.IsNullOrEmpty(DynamicHelper.Value(model.path))) {
                throw new ApiArgumentException("path");
            }

            Mapping mapping = section.Mappings.CreateElement();
            
            mapping.Name = name;
            SetMapping(model, mapping);

            return mapping;
        }

        public static void AddMapping(Mapping mapping, HandlersSection section)
        {
            if (section == null) {
                throw new ArgumentNullException("section");
            }
            if (mapping == null) {
                throw new ArgumentNullException("mapping");
            }
            if (string.IsNullOrEmpty(mapping.Name)) {
                throw new ArgumentNullException("mapping.Name");
            }
            if (string.IsNullOrEmpty(mapping.Path)) {
                throw new ArgumentNullException("mapping.Path");
            }

            if (section.Mappings.Any(m => m.Name.Equals(mapping.Name, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("mapping");
            }

            try {
                section.Mappings.Add(mapping);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void UpdateMapping(dynamic model, Mapping mapping, HandlersSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            try {

                string name = DynamicHelper.Value(model.name);
                if (!string.IsNullOrEmpty(name)) {

                    // Check if trying to change to a different name, if so make sure it doesn't already exist
                    if (section.Mappings.Any(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        && !mapping.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) {
                        throw new AlreadyExistsException("mapping.name");
                    }

                    mapping.Name = name;
                }

                SetMapping(model, mapping);

            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        private static void SetMapping(dynamic model, Mapping mapping)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (mapping == null) {
                throw new ArgumentNullException("mapping");
            }
            
            mapping.Path = DynamicHelper.Value(model.path) ?? mapping.Path;
            mapping.Verb = DynamicHelper.Value(model.verbs) ?? mapping.Verb;
            mapping.Type = DynamicHelper.Value(model.type) ?? mapping.Type;
            mapping.Modules = DynamicHelper.Value(model.modules) ?? mapping.Modules;
            mapping.ScriptProcessor = DynamicHelper.Value(model.script_processor) ?? mapping.ScriptProcessor;
            mapping.ResourceType = DynamicHelper.To<ResourceType>(model.resource_type) ?? mapping.ResourceType;
            mapping.RequireAccess = DynamicHelper.To<HandlerRequiredAccess>(model.require_access) ?? mapping.RequireAccess;
            mapping.AllowPathInfo = DynamicHelper.To<bool>(model.allow_path_info) ?? mapping.AllowPathInfo;
            mapping.PreCondition = DynamicHelper.Value(model.precondition) ?? mapping.PreCondition;
            mapping.ResponseBufferLimit = DynamicHelper.To(model.response_buffer_limit, 0, uint.MaxValue) ?? mapping.ResponseBufferLimit;
        }

        public static void DeleteMapping(Mapping mapping, HandlersSection section)
        {
            if (mapping == null) {
                return;
            }

            // To remove it we must first make sure we pulled it from this exact section
            mapping = section.Mappings.FirstOrDefault(m => m.Name.Equals(mapping.Name, StringComparison.OrdinalIgnoreCase));

            if (mapping != null) {

                try {
                    section.Mappings.Remove(mapping);

                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }

            }
        }

        internal static object ToJsonModel(Mapping mapping, Site site, string path, Fields fields = null, bool full = true)
        {
            if (mapping == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = mapping.Name;
            }

            //
            // id
            obj.id = new MappingId(site?.Id, path, mapping.Name).Uuid;

            //
            // path
            if (fields.Exists("path")) {
                obj.path = mapping.Path;
            }

            //
            // verbs
            if (fields.Exists("verbs")) {
                obj.verbs = mapping.Verb;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = mapping.Type;
            }

            //
            // modules
            if (fields.Exists("modules")) {
                obj.modules = mapping.Modules;
            }

            //
            // script_processor
            if (fields.Exists("script_processor")) {
                obj.script_processor = mapping.ScriptProcessor;
            }

            //
            // resource_type
            if (fields.Exists("resource_type")) {
                obj.resource_type = Enum.GetName(typeof(ResourceType), mapping.ResourceType).ToLower();
            }

            //
            // require_access
            if (fields.Exists("require_access")) {
                obj.require_access = Enum.GetName(typeof(HandlerRequiredAccess), mapping.RequireAccess).ToLower();
            }

            //
            // allow_path_info
            if (fields.Exists("allow_path_info")) {
                obj.allow_path_info = mapping.AllowPathInfo;
            }

            //
            // precondition
            if (fields.Exists("precondition")) {
                obj.precondition = mapping.PreCondition;
            }

            //
            // response_buffer_limit
            if (fields.Exists("response_buffer_limit")) {
                obj.response_buffer_limit = mapping.ResponseBufferLimit;
            }

            //
            // handler
            if (fields.Exists("handler")) {
                obj.handler = HandlersHelper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.MappingsResource.Guid, obj, full);
        }

        public static object ToJsonModelRef(Mapping mapping, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(mapping, site, path, RefFields, false);
            }
            else {
                return ToJsonModel(mapping, site, path, fields, false);
            }
        }

        public static string GetLocation(string id) {
            return $"/{Defines.MAPPINGS_PATH}/{id}";
        }
    }
}
