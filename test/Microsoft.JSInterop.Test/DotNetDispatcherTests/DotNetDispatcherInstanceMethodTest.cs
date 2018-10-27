using System;
using System.Threading.Tasks;
using Microsoft.JSInterop.Test.DotNetDispatcherTests.InstanceClassTestModels;
using Microsoft.JSInterop.Test.DotNetDispatcherTests.SharedTestModels;
using Xunit;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public class DotNetDispatcherInstanceMethodTest : DotNetDispatcherBaseTest
    {
        [Fact]
        public Task CanInvokeInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new PublicInstanceClass();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, TestModelMethodNames.PublicInstanceClass_PublicInstanceVoidMethod, 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.InstanceMethodWasInvoked);
        });

        [Fact]
        public Task CanInvokeBaseInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new DerivedClass();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, TestModelMethodNames.MethodOnBaseClass, 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.MethodOnBaseClassWasInvoked);
        });

        [Fact]
        public Task CannotUseDotNetObjectRefAfterDisposal() => WithJSRuntime(jsRuntime =>
        {
            // This test addresses the case where the developer calls objectRef.Dispose()
            // from .NET code, as opposed to .dispose() from JS code

            // Arrange: Track some instance, then dispose it
            var targetInstance = new PublicInstanceClass();
            var objectRef = new DotNetObjectRef(targetInstance);
            jsRuntime.Invoke<object>("unimportant", objectRef);
            objectRef.Dispose();

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(
                () => DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null));
            Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
        });

        [Fact]
        public Task CannotUseDotNetObjectRefAfterReleaseDotNetObject() => WithJSRuntime(jsRuntime =>
        {
            // This test addresses the case where the developer calls .dispose()
            // from JS code, as opposed to objectRef.Dispose() from .NET code

            // Arrange: Track some instance, then dispose it
            var targetInstance = new PublicInstanceClass();
            var objectRef = new DotNetObjectRef(targetInstance);
            jsRuntime.Invoke<object>("unimportant", objectRef);
            DotNetDispatcher.ReleaseDotNetObject(1);

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(
                () => DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null));
            Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
        });

        [Fact]
        public Task CanInvokeInstanceMethodWithParams() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var targetInstance = new PublicInstanceClass();
            var arg2 = new TestDto { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant",
                new DotNetObjectRef(targetInstance),
                new DotNetObjectRef(arg2));
            var argsJson = "[\"myvalue\",\"__dotNetObject:2\"]";

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, TestModelMethodNames.PublicInstanceClass_PublicInstanceNonVoidMethodWithTwoParams, 1, argsJson);

            // Assert
            Assert.Equal("[\"You passed myvalue\",\"__dotNetObject:3\"]", resultJson);
            var resultDto = (TestDto)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal(1235, resultDto.IntVal);
            Assert.Equal("MY STRING", resultDto.StringVal);
        });

        [Theory]
        [InlineData(TestModelMethodNames.PublicInstanceClass_PrivateInstanceVoidMethod)]
        [InlineData(TestModelMethodNames.PublicInstanceClass_ProtectedInstanceVoidMethod)]
        public Task CannotInvokeUnsuitableMethods(string methodIdentifier) => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var targetInstance = new PublicInstanceClass();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(null, methodIdentifier, 1, null);
            });

            Assert.Equal(
                expected: 
                    $"The class '{nameof(PublicInstanceClass)}' does not contain a public method" +
                    $" with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].",
                actual: ex.Message);
        });

        [Fact]
        public Task CanInvokeAsyncMethod() => WithJSRuntime(async jsRuntime =>
        {
        // Arrange: Track some instance plus another object we'll pass as a param
        var targetInstance = new PublicInstanceClass();
            var arg2 = new TestDto { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance), new DotNetObjectRef(arg2));

        // Arrange: all args
        var argsJson = Json.Serialize(new object[]
        {
                new TestDto { IntVal = 1000, StringVal = "String via JSON" },
                "__dotNetObject:2"
                });

        // Act
        var callId = "123";
            var resultTask = jsRuntime.NextInvocationTask;
            DotNetDispatcher.BeginInvoke(callId, null, TestModelMethodNames.PublicInstanceClass_PublicInstanceAsyncMethod, 1, argsJson);
            await resultTask;
            var result = Json.Deserialize<SimpleJson.JsonArray>(jsRuntime.LastInvocationArgsJson);
            var resultValue = (SimpleJson.JsonArray)result[2];

        // Assert: Correct info to complete the async call
        Assert.Equal(0, jsRuntime.LastInvocationAsyncHandle); // 0 because it doesn't want a further callback from JS to .NET
            Assert.Equal("DotNet.jsCallDispatcher.endInvokeDotNetFromJS", jsRuntime.LastInvocationIdentifier);
            Assert.Equal(3, result.Count);
            Assert.Equal(callId, result[0]);
            Assert.True((bool)result[1]); // Success flag

        // Assert: First result value marshalled via JSON
        var resultDto1 = (TestDto)jsRuntime.ArgSerializerStrategy.DeserializeObject(resultValue[0], typeof(TestDto));
            Assert.Equal("STRING VIA JSON", resultDto1.StringVal);
            Assert.Equal(2000, resultDto1.IntVal);

        // Assert: Second result value marshalled by ref
        var resultDto2Ref = (string)resultValue[1];
            Assert.Equal("__dotNetObject:3", resultDto2Ref);
            var resultDto2 = (TestDto)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal("MY STRING", resultDto2.StringVal);
            Assert.Equal(2468, resultDto2.IntVal);
        });
    }

}
