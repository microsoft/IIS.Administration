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

    public class HiddenSegmentsHelper
    {
        public static List<HiddenSegment> getSegments(Site site, string path)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path);

            var collection = requestFilteringSection.HiddenSegments;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<HiddenSegment>();
        }

        public static HiddenSegment CreateSegment(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string segmentName = DynamicHelper.Value(model.segment);
            if (string.IsNullOrEmpty(segmentName)) {
                throw new ApiArgumentException("segment");
            }

            HiddenSegment segment = section.HiddenSegments.CreateElement();

            segment.Segment = segmentName;

            return segment;
        }

        public static void AddSegment(HiddenSegment segment, RequestFilteringSection section)
        {
            if (segment == null) {
                throw new ArgumentNullException("segment");
            }
            if (segment.Segment == null) {
                throw new ArgumentNullException("extension.Segment");
            }

            var collection = section.HiddenSegments;

            if (collection.Any(seg => seg.Segment.Equals(segment.Segment))) {
                throw new AlreadyExistsException("segment");
            }

            try {
                collection.Add(segment);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteSegment(HiddenSegment segment, RequestFilteringSection section)
        {
            if (segment == null) {
                return;
            }

            var collection = section.HiddenSegments;

            segment = collection.FirstOrDefault(s => s.Segment.Equals(segment.Segment));

            if (segment != null) {
                try {
                    collection.Remove(segment);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        internal static object ToJsonModel(HiddenSegment segment, Site site, string path)
        {
            if (segment == null) {
                return null;
            }

            SegmentId segmentId = new SegmentId(site?.Id, path, segment.Segment);

            var obj = new {
                segment = segment.Segment,
                id = segmentId.Uuid,
                request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.HiddenSegmentsResource.Guid, obj);
        }

        public static object ToJsonModelRef(HiddenSegment segment, Site site, string path)
        {
            if (segment == null) {
                return null;
            }

            SegmentId segmentId = new SegmentId(site?.Id, path, segment.Segment);

            var obj = new {
                segment = segment.Segment,
                id = segmentId.Uuid
            };

            return Core.Environment.Hal.Apply(Defines.HiddenSegmentsResource.Guid, obj, false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.HIDDEN_SEGMENTS_PATH}/{id}";
        }
    }
}
