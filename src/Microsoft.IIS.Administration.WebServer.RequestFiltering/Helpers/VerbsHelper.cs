// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public class VerbsHelper
    {
        public static List<VerbElement> GetVerbs(Site site, string path, string configPath = null)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            var collection = requestFilteringSection.Verbs;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<VerbElement>();
        }

        public static VerbElement CreateVerb(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string verbString = DynamicHelper.Value(model.verb);
            if (string.IsNullOrEmpty(verbString)) {
                throw new ApiArgumentException("verb");
            }

            VerbElement verb = section.Verbs.CreateElement();

            verb.Verb = verbString;

            UpdateVerb(verb, model);

            return verb;
        }

        public static VerbElement UpdateVerb(VerbElement verb, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (verb == null) {
                throw new ArgumentNullException("verb");
            }

            try {
                verb.Allowed = DynamicHelper.To<bool>(model.allowed) ?? verb.Allowed;
            }
            catch (FileLoadException e) {
                throw new LockedException(RequestFilteringGlobals.RequestFilteringSectionName, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return verb;

        }

        public static void AddVerb(VerbElement verb, RequestFilteringSection section)
        {
            if (verb == null) {
                throw new ArgumentNullException("verb");
            }
            if (verb.Verb == null) {
                throw new ArgumentNullException("verb.Verb");
            }

            VerbCollection collection = section.Verbs;

            if (collection.Any(v => v.Verb.Equals(verb.Verb))) {
                throw new AlreadyExistsException("verb");
            }

            try {
                collection.Add(verb);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteVerb(VerbElement verb, RequestFilteringSection section)
        {
            if (verb == null) {
                return;
            }

            VerbCollection collection = section.Verbs;

            // To utilize the remove functionality we must pull the element directly from the collection
            verb = collection.FirstOrDefault(v => v.Verb.Equals(verb.Verb));

            if (verb != null) {
                try {
                    collection.Remove(verb);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }
    }
}
