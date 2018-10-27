using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    internal class SomeInteralType
    {
        [JSInvokable("MethodOnInternalType")] public void MyMethod() { }
    }

    public class SomePublicType
    {
        public static bool DidInvokeMyInvocableStaticVoid;
        public bool DidInvokeMyInvocableInstanceVoid;

        [JSInvokable("PrivateMethod")] private static void MyPrivateMethod() { }
        [JSInvokable("ProtectedMethod")] protected static void MyProtectedMethod() { }
        protected static void StaticMethodWithoutAttribute() { }
        protected static void InstanceMethodWithoutAttribute() { }

        [JSInvokable("InvocableStaticVoid")]
        public static void MyInvocableVoid()
        {
            DidInvokeMyInvocableStaticVoid = true;
        }

        [JSInvokable("InvocableStaticNonVoid")]
        public static object MyInvocableNonVoid()
            => new TestDTO { StringVal = "Test", IntVal = 123 };

        [JSInvokable("InvocableStaticWithParams")]
        public static object[] MyInvocableWithParams(TestDTO dtoViaJson, int[] incrementAmounts, TestDTO dtoByRef)
            => new object[]
            {
                    new TestDTO // Return via JSON marshalling
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal + incrementAmounts.Sum()
                    },
                    new DotNetObjectRef(new TestDTO // Return by ref
                    {
                        StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.IntVal + incrementAmounts.Sum()
                    })
            };

        [JSInvokable(nameof(InvokableInstanceVoid))]
        public void InvokableInstanceVoid()
        {
            DidInvokeMyInvocableInstanceVoid = true;
        }

        [JSInvokable(nameof(InvokableInstanceMethod))]
        public object[] InvokableInstanceMethod(string someString, TestDTO someDTO)
        {
            // Returning an array to make the point that object references
            // can be embedded anywhere in the result
            return new object[]
            {
                    $"You passed {someString}",
                    new DotNetObjectRef(new TestDTO
                    {
                        IntVal = someDTO.IntVal + 1,
                        StringVal = someDTO.StringVal.ToUpperInvariant()
                    })
            };
        }

        [JSInvokable(nameof(InvokableAsyncMethod))]
        public async Task<object[]> InvokableAsyncMethod(TestDTO dtoViaJson, TestDTO dtoByRef)
        {
            await Task.Delay(50);
            return new object[]
            {
                    new TestDTO // Return via JSON
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal * 2,
                    },
                    new DotNetObjectRef(new TestDTO // Return by ref
                    {
                        StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.IntVal * 2,
                    })
            };
        }
    }

    public class BaseClass
    {
        public bool DidInvokeMyBaseClassInvocableInstanceVoid;

        [JSInvokable(nameof(BaseClassInvokableInstanceVoid))]
        public void BaseClassInvokableInstanceVoid()
        {
            DidInvokeMyBaseClassInvocableInstanceVoid = true;
        }
    }

    public class DerivedClass : BaseClass
    {
    }

    public class TestDTO
    {
        public string StringVal { get; set; }
        public int IntVal { get; set; }
    }

}
