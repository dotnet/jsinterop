namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public static class TestModelMethodNames
    {
        // Static classes
        public const string InternalStaticClass_PublicStaticVoidMethod = nameof(InternalStaticClass_PublicStaticVoidMethod);
        public const string PublicStaticClass_PrivateStaticVoidMethod = nameof(PublicStaticClass_PrivateStaticVoidMethod);
        public const string PublicStaticClass_PublicStaticVoidMethod = nameof(PublicStaticClass_PublicStaticVoidMethod);
        public const string PublicStaticClass_PublicStaticNonVoidMethod = nameof(PublicStaticClass_PublicStaticNonVoidMethod);
        public const string PublicStaticClass_PublicStaticNonVoidMethodWithParams = nameof(PublicStaticClass_PublicStaticNonVoidMethodWithParams);

        // Instance classes
        public const string MethodOnBaseClass = nameof(MethodOnBaseClass);
        public const string SameMethodIdentifierOnUnrelatedClasses = nameof(SameMethodIdentifierOnUnrelatedClasses);
        public const string InternalInstanceClass_PublicInstanceVoidMethod = nameof(InternalInstanceClass_PublicInstanceVoidMethod);
        public const string PublicInstanceClass_PrivateInstanceVoidMethod = nameof(PublicInstanceClass_PrivateInstanceVoidMethod);
        public const string PublicInstanceClass_ProtectedInstanceVoidMethod = nameof(PublicInstanceClass_ProtectedInstanceVoidMethod);
        public const string PublicInstanceClass_PublicInstanceVoidMethod = nameof(PublicInstanceClass_PublicInstanceVoidMethod);
        public const string PublicInstanceClass_PublicInstanceNonVoidMethod = nameof(PublicInstanceClass_PublicInstanceNonVoidMethod);
        public const string PublicInstanceClass_PublicInstanceNonVoidMethodWithParams = nameof(PublicInstanceClass_PublicInstanceNonVoidMethodWithParams);
        public const string PublicInstanceClass_PublicInstanceNonVoidMethodWithTwoParams = nameof(PublicInstanceClass_PublicInstanceNonVoidMethodWithTwoParams);
        public const string PublicInstanceClass_PublicInstanceAsyncMethod = nameof(PublicInstanceClass_PublicInstanceAsyncMethod);
        public const string PublicGenericClass_PublicInstanceVoidMethod = nameof(PublicGenericClass_PublicInstanceVoidMethod);
        public const string PublicGenericClass_PublicInstanceNonVoidMethod = nameof(PublicGenericClass_PublicInstanceNonVoidMethod);
        public const string PublicGenericClass_PublicInstanceNonVoidMethodWithParams = nameof(PublicGenericClass_PublicInstanceNonVoidMethodWithParams);
    }

}
