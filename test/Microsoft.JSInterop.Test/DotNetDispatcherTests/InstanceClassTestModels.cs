using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop.Test.DotNetDispatcherTests.SharedTestModels;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests.InstanceClassTestModels
{
    // Instance test models
    internal class InternalInstanceClass
    {
        [JSInvokable(TestModelMethodNames.InternalInstanceClass_PublicInstanceVoidMethod)]
        public void PublicInstanceVoidMethod() { }
    }

    public class PublicInstanceClass
    {
        public bool InstanceMethodWasInvoked;

        // Can invoke
        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PublicInstanceVoidMethod)]
        public void PublicInstanceVoidMethod() => InstanceMethodWasInvoked = true;

        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PublicInstanceNonVoidMethod)]
        public object PublicInstanceNonVoidMethod() => new TestDto { StringVal = "Test", IntVal = 123 };

        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PublicInstanceNonVoidMethodWithParams)]
        public object[] PublicInstanceNonVoidMethodWithParams(
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
        
        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PublicInstanceAsyncMethod)]
        public async Task<object[]> PublicInstanceAsyncMethod(TestDto dtoViaJson, TestDto dtoByRef)
        {
            await Task.Delay(50);
            return new object[]
            {
                    new TestDto // Return via JSON
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal * 2,
                    },
                    new DotNetObjectRef(new TestDto // Return by ref
                    {
                        StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.IntVal * 2,
                    })
            };
        }

        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PublicInstanceNonVoidMethodWithTwoParams)]
        public object[] InvokableInstanceMethod(string someString, TestDto someDto)
        {
            // Returning an array to make the point that object references
            // can be embedded anywhere in the result
            return new object[]
            {
                    $"You passed {someString}",
                    new DotNetObjectRef(new TestDto
                    {
                        IntVal = someDto.IntVal + 1,
                        StringVal = someDto.StringVal.ToUpperInvariant()
                    })
            };
        }

        // Cannot invoke
        [JSInvokable(TestModelMethodNames.PublicInstanceClass_PrivateInstanceVoidMethod)]
        private void PrivateInstanceVoidMethod() => InstanceMethodWasInvoked = true;

        [JSInvokable(TestModelMethodNames.PublicInstanceClass_ProtectedInstanceVoidMethod)]
        protected void ProtectedInstanceVoidMethod() => InstanceMethodWasInvoked = true;

        public void PublicInstanceVoidMethodWithoutAttribute() => InstanceMethodWasInvoked = true;
    }

    public class PublicGenericClass<T>
    {
        public bool InstanceMethodWasInvoked;

        // Can invoke
        [JSInvokable(TestModelMethodNames.PublicGenericClass_PublicInstanceVoidMethod)]
        public void PublicInstanceVoidMethod() => InstanceMethodWasInvoked = true;

        [JSInvokable(TestModelMethodNames.PublicGenericClass_PublicInstanceNonVoidMethod)]
        public T PublicInstanceNonVoidMethod()
        {
            InstanceMethodWasInvoked = true;
            return default;
        }

        [JSInvokable(TestModelMethodNames.PublicGenericClass_PublicInstanceNonVoidMethodWithParams)]
        public T PublicInstanceNonVoidMethodWithParams(T value)
        {
            InstanceMethodWasInvoked = true;
            return value;
        }
    }

    public class BaseClass
    {
        public bool MethodOnBaseClassWasInvoked;

        [JSInvokable(TestModelMethodNames.MethodOnBaseClass)]
        public void BaseClassInvokableInstanceVoid() => MethodOnBaseClassWasInvoked = true;
    }

    public class DerivedClass : BaseClass
    {
    }

    public class FirstClassWithSameInstanceMethodIdentifier
    {
        [JSInvokable(TestModelMethodNames.SameMethodIdentifierOnUnrelatedClasses)]
        public int SameMethodIdentifierOnUnrelatedClasses() => 1;
    }

    public class UnrelatedClassWithSameInstanceMethodIdentifier
    {
        [JSInvokable(TestModelMethodNames.SameMethodIdentifierOnUnrelatedClasses)]
        public int SameMethodIdentifierOnUnrelatedClasses() => 2;
    }

}
