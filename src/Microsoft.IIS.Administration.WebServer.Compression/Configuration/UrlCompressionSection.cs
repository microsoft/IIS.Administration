// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Web.Administration;

    public sealed class UrlCompressionSection : ConfigurationSection {

        private const string DoDynamicCompressionAttribute = "doDynamicCompression";
        private const string DoStaticCompressionAttribute = "doStaticCompression";

        public UrlCompressionSection() {
        }

        public bool DoDynamicCompression {
            get {
                return (bool)base[DoDynamicCompressionAttribute];
            }
            set {
                base[DoDynamicCompressionAttribute] = value;
            }
        }

        public bool DoStaticCompression {
            get {
                return (bool)base[DoStaticCompressionAttribute];
            }
            set {
                base[DoStaticCompressionAttribute] = value;
            }
        }
    }
}
