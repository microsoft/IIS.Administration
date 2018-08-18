// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using System;
    using System.Reflection;

    enum X500NameFlags
    {
        XCN_CERT_NAME_STR_NONE = 0
    }

    enum EncodingType
    {
        XCN_CRYPT_STRING_BASE64HEADER = 0
    }

    enum ObjectIdGroupId
    {
        XCN_CRYPT_FIRST_ALG_OID_GROUP_ID = 1
    }

    enum ObjectIdPublicKeyFlags
    {
        XCN_CRYPT_OID_INFO_PUBKEY_ANY = 0
    }

    enum X509CertificateEnrollmentContext
    {
        ContextMachine = 2
    }

    enum AlgorithmFlags
    {
        AlgorithmFlagsNone = 0
    }

    enum X509KeyUsageFlags
    {
        XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE = 0x80,
        XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE = 0x20
    }

    enum AlternativeNameType
    {
        XCN_CERT_ALT_NAME_DNS_NAME = 3
    }

    enum InstallResponseRestrictionFlags
    {
        AllowUntrustedCertificate = 2
    }

    //
    // Helper for calling CertEnroll COM APIs without having to rely on class member order matching
    abstract class CertEnrollWrapper
    {
        protected object _instance;
        private Type _type = null;

        public CertEnrollWrapper()
        {
            if (Type != null)
            {
                _instance = Activator.CreateInstance(Type);
            }
        }

        protected abstract string TypeName { get; }

        public CertEnrollWrapper(object instance)
        {
            _instance = instance;
        }

        public object Instance {
            get => _instance;
        }

        protected Type Type {
            get {

                if (_type == null)
                {
                    _type = Type.GetTypeFromProgID(TypeName);
                }

                return _type;
            }
        }

        protected object InvokeMember(Type type, string memberName, BindingFlags flags, object binder, object target, object[] args)
        {
            object returnValue = null;

            switch (flags)
            {
                case BindingFlags.InvokeMethod:

                    var method = type.GetTypeInfo().GetDeclaredMethod(memberName);

                    returnValue = method.Invoke(target, args);

                    break;

                case BindingFlags.GetProperty:

                    var getter = type.GetTypeInfo().GetDeclaredProperty(memberName);

                    returnValue = getter.GetValue(target);

                    break;

                case BindingFlags.SetProperty:

                    var setter = type.GetTypeInfo().GetDeclaredProperty(memberName);

                    setter.SetValue(target, args[0]);

                    break;
            }

            return returnValue;
        }
    }

    class CX509Enrollment : CertEnrollWrapper
    {
        public CX509Enrollment() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509Enrollment";
        }

        public string CreateRequest(EncodingType Encoding)
        {
            return (string)InvokeMember(Type, nameof(CreateRequest), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { Encoding });
        }

        public void InitializeFromRequest(CX509CertificateRequestCertificate pRequest)
        {
            var info = pRequest.GetType().GetTypeInfo();

            InvokeMember(Type, nameof(InitializeFromRequest), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { pRequest.Instance });
        }

        public void InstallResponse(InstallResponseRestrictionFlags Restrictions, string strResponse, EncodingType Encoding, string strPassword)
        {
            InvokeMember(Type, nameof(InstallResponse), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { Restrictions, strResponse, Encoding, strPassword });
        }

        public string CertificateFriendlyName {
            get => (string)InvokeMember(Type, nameof(CertificateFriendlyName), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(CertificateFriendlyName), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }
    }

    class CAlternativeName : CertEnrollWrapper
    {
        public CAlternativeName() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CAlternativeName";
        }

        public void InitializeFromString(AlternativeNameType type, string strValue)
        {
            InvokeMember(Type, nameof(InitializeFromString), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { type, strValue });
        }
    }

    class CAlternativeNames : CertEnrollWrapper
    {
        public CAlternativeNames() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CAlternativeNames";
        }

        public void Add(CAlternativeName pVal)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { pVal.Instance });
        }
    }

    class CX509ExtensionAlternativeNames : CertEnrollWrapper
    {
        public CX509ExtensionAlternativeNames() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509ExtensionAlternativeNames";
        }

        public void InitializeEncode(CAlternativeNames pValue)
        {
            InvokeMember(Type, nameof(InitializeEncode), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { pValue.Instance });
        }
    }

    class CObjectId : CertEnrollWrapper
    {
        public CObjectId() : base() { }

        public CObjectId(object instance) : base(instance) { }

        protected override string TypeName {
            get => "X509Enrollment.CObjectId";
        }

        public void InitializeFromAlgorithmName(ObjectIdGroupId GroupId, ObjectIdPublicKeyFlags KeyFlags, AlgorithmFlags AlgFlags, string strAlgorithmName)
        {
            InvokeMember(Type, nameof(InitializeFromAlgorithmName), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { GroupId, KeyFlags, AlgFlags, strAlgorithmName });
        }

        public void InitializeFromValue(string strValue)
        {
            InvokeMember(Type, nameof(InitializeFromValue), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { strValue });
        }
    }

    class CObjectIds : CertEnrollWrapper
    {
        public CObjectIds() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CObjectIds";
        }

        public void Add(CObjectId value)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }
    }

    class CX509Extension : CertEnrollWrapper
    {
        public CX509Extension() : base() { }

        public CX509Extension(object instance) : base(instance) { }

        protected override string TypeName {
            get => "X509Enrollment.CX509Extension";
        }

        public void Initialize(CObjectIds pObjectId, string strEncodedData)
        {
            InvokeMember(Type, nameof(Initialize), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { pObjectId.Instance, strEncodedData });
        }
    }

    class CX509ExtensionEnhancedKeyUsage : CertEnrollWrapper
    {
        public CX509ExtensionEnhancedKeyUsage() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509ExtensionEnhancedKeyUsage";
        }

        public void InitializeEncode(CObjectIds value)
        {
            InvokeMember(Type, nameof(InitializeEncode), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }
    }

    class CX509ExtensionKeyUsage : CertEnrollWrapper
    {
        public CX509ExtensionKeyUsage() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509ExtensionKeyUsage";
        }

        public void InitializeEncode(X509KeyUsageFlags UsageFlags)
        {
            InvokeMember(Type, nameof(InitializeEncode), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { UsageFlags });
        }
    }

    class CX509Extensions : CertEnrollWrapper
    {
        public CX509Extensions() : base() { }

        public CX509Extensions(object instance) : base(instance) { }

        protected override string TypeName {
            get => "X509Enrollment.CX509Extensions";
        }

        public void Add(CX509Extension value)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }

        public void Add(CX509ExtensionEnhancedKeyUsage value)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }

        public void Add(CX509ExtensionKeyUsage value)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }

        public void Add(CX509ExtensionAlternativeNames value)
        {
            InvokeMember(Type, nameof(Add), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { value.Instance });
        }
    }

    class CX500DistinguishedName : CertEnrollWrapper
    {
        public CX500DistinguishedName() : base() { }

        public CX500DistinguishedName(object instance) : base(instance) { }

        protected override string TypeName {
            get => "X509Enrollment.CX500DistinguishedName";
        }

        public string Name {
            get {
                return (string)InvokeMember(Type, nameof(Name), System.Reflection.BindingFlags.GetProperty, null, _instance, null);
            }
        }

        public void Encode(string name, X500NameFlags flags)
        {
            InvokeMember(Type, nameof(Encode), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { name, flags });
        }
    }

    class CX509PrivateKey : CertEnrollWrapper
    {
        public CX509PrivateKey() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509PrivateKey";
        }

        public int Length {
            get => (int)InvokeMember(Type, nameof(Length), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(Length), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }

        public bool MachineContext {
            get => (bool)InvokeMember(Type, nameof(MachineContext), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(MachineContext), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }

        public string ProviderName {
            get => (string)InvokeMember(Type, nameof(ProviderName), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(ProviderName), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }

        public void Create()
        {
            InvokeMember(Type, nameof(Create), System.Reflection.BindingFlags.InvokeMethod, null, _instance, null);
        }
    }

    class CX509CertificateRequestCertificate : CertEnrollWrapper
    {
        public CX509CertificateRequestCertificate() : base() { }

        protected override string TypeName {
            get => "X509Enrollment.CX509CertificateRequestCertificate";
        }

        public CObjectId HashAlgorithm {
            get => new CObjectId(InvokeMember(Type, nameof(HashAlgorithm), System.Reflection.BindingFlags.GetProperty, null, _instance, null));

            set => InvokeMember(Type, nameof(HashAlgorithm), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value.Instance });
        }

        public CX500DistinguishedName Issuer {
            get => new CX500DistinguishedName(InvokeMember(Type, nameof(Issuer), System.Reflection.BindingFlags.GetProperty, null, _instance, null));

            set => InvokeMember(Type, nameof(Issuer), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value.Instance });
        }

        public DateTime NotAfter {
            get => (DateTime)InvokeMember(Type, nameof(NotAfter), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(NotAfter), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }

        public DateTime NotBefore {
            get => (DateTime)InvokeMember(Type, nameof(NotBefore), System.Reflection.BindingFlags.GetProperty, null, _instance, null);

            set => InvokeMember(Type, nameof(NotBefore), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value });
        }

        public CX500DistinguishedName Subject {
            get => new CX500DistinguishedName(InvokeMember(Type, nameof(Subject), System.Reflection.BindingFlags.GetProperty, null, _instance, null));

            set => InvokeMember(Type, nameof(Subject), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value.Instance });
        }

        public CX509Extensions X509Extensions {
            get => new CX509Extensions(InvokeMember(Type, nameof(X509Extensions), System.Reflection.BindingFlags.GetProperty, null, _instance, null));

            set => InvokeMember(Type, nameof(X509Extensions), System.Reflection.BindingFlags.SetProperty, null, _instance, new object[] { value.Instance });
        }

        public void Encode()
        {
            InvokeMember(Type, nameof(Encode), System.Reflection.BindingFlags.InvokeMethod, null, _instance, null);
        }

        public void InitializeFromPrivateKey(X509CertificateEnrollmentContext Context, CX509PrivateKey pPrivateKey, string strTemplateName)
        {
            InvokeMember(Type, nameof(InitializeFromPrivateKey), System.Reflection.BindingFlags.InvokeMethod, null, _instance, new object[] { Context, pPrivateKey.Instance, strTemplateName });
        }
    }
}
