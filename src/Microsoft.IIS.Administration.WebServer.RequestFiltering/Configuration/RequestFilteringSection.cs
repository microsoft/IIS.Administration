// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;
    
    public class RequestFilteringSection : ConfigurationSection {
        
        private AlwaysAllowedQueryStringCollection _alwaysAllowedQueryStrings;
        
        private AlwaysAllowedUrlCollection _alwaysAllowedUrls;
        
        private DenyQueryStringSequenceCollection _denyQueryStringSequences;
        private DenyUrlSequenceCollection _denyUrlSequences;
        
        private FileExtensionCollection _fileExtensions;
        private FilteringRuleCollection _filteringRules;
        
        private HiddenSegmentCollection _hiddenSegments;
        
        private RequestLimitsElement _requestLimits;
        
        private VerbCollection _verbs;
        
        public RequestFilteringSection() {
        }
        
        public bool AllowDoubleEscaping {
            get {
                return ((bool)(base["allowDoubleEscaping"]));
            }
            set {
                base["allowDoubleEscaping"] = value;
            }
        }
        
        public bool AllowHighBitCharacters {
            get {
                return ((bool)(base["allowHighBitCharacters"]));
            }
            set {
                base["allowHighBitCharacters"] = value;
            }
        }
        
        public bool UnescapeQueryString {
            get {
                return ((bool)(base["unescapeQueryString"]));
            }
            set {
                base["unescapeQueryString"] = value;
            }
        }
        
        public AlwaysAllowedQueryStringCollection AlwaysAllowedQueryStrings {
            get {
                if ((this._alwaysAllowedQueryStrings == null)) {
                    this._alwaysAllowedQueryStrings = ((AlwaysAllowedQueryStringCollection)(base.GetCollection("alwaysAllowedQueryStrings", typeof(AlwaysAllowedQueryStringCollection))));
                }
                return this._alwaysAllowedQueryStrings;
            }
        }
        
        public AlwaysAllowedUrlCollection AlwaysAllowedUrls {
            get {
                if ((this._alwaysAllowedUrls == null)) {
                    this._alwaysAllowedUrls = ((AlwaysAllowedUrlCollection)(base.GetCollection("alwaysAllowedUrls", typeof(AlwaysAllowedUrlCollection))));
                }
                return this._alwaysAllowedUrls;
            }
        }
        
        public DenyQueryStringSequenceCollection DenyQueryStringSequences {
            get {
                if ((this._denyQueryStringSequences == null)) {
                    this._denyQueryStringSequences = ((DenyQueryStringSequenceCollection)(base.GetCollection("denyQueryStringSequences", typeof(DenyQueryStringSequenceCollection))));
                }
                return this._denyQueryStringSequences;
            }
        }
        public DenyUrlSequenceCollection DenyUrlSequences {
            get {
                if ((this._denyUrlSequences == null)) {
                    this._denyUrlSequences = ((DenyUrlSequenceCollection)(base.GetCollection("denyUrlSequences", typeof(DenyUrlSequenceCollection))));
                }
                return this._denyUrlSequences;
            }
        }
        
        public FileExtensionCollection FileExtensions {
            get {
                if ((this._fileExtensions == null)) {
                    this._fileExtensions = ((FileExtensionCollection)(base.GetCollection("fileExtensions", typeof(FileExtensionCollection))));
                }
                return this._fileExtensions;
            }
        }
        
        public FilteringRuleCollection FilteringRules {
            get {
                if ((this._filteringRules == null)) {
                    this._filteringRules = ((FilteringRuleCollection)(base.GetCollection("filteringRules", typeof(FilteringRuleCollection))));
                }
                return this._filteringRules;
            }
        }
        
        public HiddenSegmentCollection HiddenSegments {
            get {
                if ((this._hiddenSegments == null)) {
                    this._hiddenSegments = ((HiddenSegmentCollection)(base.GetCollection("hiddenSegments", typeof(HiddenSegmentCollection))));
                }
                return this._hiddenSegments;
            }
        }
        
        public RequestLimitsElement RequestLimits {
            get {
                if ((this._requestLimits == null)) {
                    this._requestLimits = ((RequestLimitsElement)(base.GetChildElement("requestLimits", typeof(RequestLimitsElement))));
                }
                return this._requestLimits;
            }
        }
        
        public VerbCollection Verbs {
            get {
                if ((this._verbs == null)) {
                    this._verbs = ((VerbCollection)(base.GetCollection("verbs", typeof(VerbCollection))));
                }
                return this._verbs;
            }
        }
    }
}
