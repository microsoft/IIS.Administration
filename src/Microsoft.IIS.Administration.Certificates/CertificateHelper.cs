// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core.Utils;
    using System.Dynamic;

    public static class CertificateHelper
    {
        private static readonly Fields RefFields = new Fields("alias", "id", "issued_by", "subject", "valid_to", "thumbprint");
        private static readonly Fields StoreRefFields = new Fields("name", "id");

        public static object ToJsonModelRef(ICertificate cert, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(cert, RefFields, false);
            }
            else {
                return ToJsonModel(cert, fields, false);
            }
        }

        internal static object ToJsonModel(ICertificate certificate, Fields fields = null, bool full = true)
        {
            if (certificate == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // alias
            if (fields.Exists("alias")) {
                obj.alias = certificate.Alias;
            }

            //
            // id
            obj.id = new CertificateId(certificate.Id, certificate.Store.Name).Uuid;

            //
            // issued_by
            if (fields.Exists("issued_by")) {
                obj.issued_by = certificate.Issuer;
            }

            //
            // subject
            if (fields.Exists("subject")) {
                obj.subject = certificate.Subject;
            }

            //
            // thumbprint
            if (fields.Exists("thumbprint")) {
                obj.thumbprint = certificate.Thumbprint;
            }

            //
            // signature_algorithm
            if (fields.Exists("signature_algorithm")) {
                obj.signature_algorithm = certificate.SignatureAlgorithm;
            }

            //
            // valid_from
            if (fields.Exists("valid_from")) {
                obj.valid_from = certificate.ValidFrom.ToUniversalTime();
            }

            //
            // valid_to
            if (fields.Exists("valid_to")) {
                obj.valid_to = certificate.Expires.ToUniversalTime();
            }

            //
            // version
            if (fields.Exists("version")) {
                obj.version = certificate.Version.ToString();
            }

            //
            // intended_purposes
            if (fields.Exists("intended_purposes")) {
                obj.intended_purposes = certificate.Purposes;
            }

            //
            // private_key
            if (fields.Exists("private_key")) {
                obj.private_key = KeyToJsonModel(certificate);
            }

            //
            // subject_alternative_names
            if (fields.Exists("subject_alternative_names")) {
                obj.subject_alternative_names = certificate.SubjectAlternativeNames;
            }

            //
            // store
            if (fields.Exists("store")) {
                obj.store = StoreToJsonModelRef(certificate.Store);
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object StoreToJsonModelRef(ICertificateStore store, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return StoreToJsonModel(store, StoreRefFields, false);
            }
            else {
                return StoreToJsonModel(store, fields, false);
            }
        }

        internal static object StoreToJsonModel(ICertificateStore store, Fields fields = null, bool full = true)
        {
            if (store == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            StoreId id = StoreId.FromName(store.Name);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = store.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = id.Uuid;
            }

            //
            // claims
            if (fields.Exists("claims")) {
                obj.claims = store.Claims;
            }

            return Core.Environment.Hal.Apply(Defines.StoresResource.Guid, obj, full);
        }

        private static object KeyToJsonModel(ICertificate cert)
        {
            if (cert == null || !cert.HasPrivateKey) {
                return null;
            }

            return new {
                exportable = cert.IsPrivateKeyExportable
            };
        }
    }
}
