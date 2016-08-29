// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;
    using System.Collections.Generic;
    using Web.Administration;

    public sealed class FailureDefinitions : ConfigurationElement {

        private const string TimeTakenAttribute = "timeTaken";
        private const string StatusCodesAttribute = "statusCodes";
        private const string VerbosityAttribute = "verbosity";

        public string StatusCodes {
            get
            {
                return (string)base[StatusCodesAttribute];
            }
            set
            {
                base[StatusCodesAttribute] = value;
            }
        }

        public TimeSpan TimeTaken {
            get {
                return (TimeSpan)base[TimeTakenAttribute];
            }
            set {
                base[TimeTakenAttribute] = value;
            }
        }

        public FailureDefinitionVerbosity Verbosity {
            get {
                return (FailureDefinitionVerbosity)base[VerbosityAttribute];
            }
            set {
                base[VerbosityAttribute] = value;
            }
        }
    }
}
