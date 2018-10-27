using System.Linq;
using Microsoft.JSInterop.Test.DotNetDispatcherTests.SharedTestModels;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests.StaticClassTestModels
{
    // Static test models
    internal static class InternalStaticClass
    {
        [JSInvokable(TestModelMethodNames.InternalStaticClass_PublicStaticVoidMethod)]
        public static void PublicStaticVoidMethod() { }
    }

    public static class PublicStaticClass
    {
        public static bool StaticMethodWasInvoked;

        [JSInvokable(TestModelMethodNames.PublicStaticClass_PrivateStaticVoidMethod)]
        private static void PrivateStaticVoidMethod() => StaticMethodWasInvoked = true;

        [JSInvokable(TestModelMethodNames.PublicStaticClass_PublicStaticVoidMethod)]
        public static void PublicStaticVoidMethod() => StaticMethodWasInvoked = true;

        [JSInvokable(TestModelMethodNames.PublicStaticClass_PublicStaticNonVoidMethod)]
        public static object PublicStaticNonVoidMethod() => new TestDto { StringVal = "Test", IntVal = 123 };

        [JSInvokable(TestModelMethodNames.PublicStaticClass_PublicStaticNonVoidMethodWithParams)]
        public static object[] PublicStaticNonVoidMethodWithParams(
            TestDto dtoViaJson, int[] incrementAmounts, TestDto dtoByRef)
        {
            return new object[]
            {
                new TestDto // Return via JSON marshalling
                {
                    StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                    IntVal = dtoViaJson.IntVal + incrementAmounts.Sum()
                },
                new DotNetObjectRef(new TestDto // Return by ref
                {
                    StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                    IntVal = dtoByRef.IntVal + incrementAmounts.Sum()
                })
            };
        }
    }

}
