using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public class DotNetDispatcherIntanceMethodTest : DotNetDispatcherBaseTest
    {
        [Fact]
        public Task CanInvokeInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new SomePublicType();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.DidInvokeMyInvocableInstanceVoid);
        });

        [Fact]
        public Task CanInvokeBaseInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new DerivedClass();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "BaseClassInvokableInstanceVoid", 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.DidInvokeMyBaseClassInvocableInstanceVoid);
        });

        [Fact]
        public Task CannotUseDotNetObjectRefAfterDisposal() => WithJSRuntime(jsRuntime =>
        {
            // This test addresses the case where the developer calls objectRef.Dispose()
            // from .NET code, as opposed to .dispose() from JS code

            // Arrange: Track some instance, then dispose it
            var targetInstance = new SomePublicType();
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
            var targetInstance = new SomePublicType();
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
            var targetInstance = new SomePublicType();
            var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant",
                new DotNetObjectRef(targetInstance),
                new DotNetObjectRef(arg2));
            var argsJson = "[\"myvalue\",\"__dotNetObject:2\"]";

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "InvokableInstanceMethod", 1, argsJson);

            // Assert
            Assert.Equal("[\"You passed myvalue\",\"__dotNetObject:3\"]", resultJson);
            var resultDto = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal(1235, resultDto.IntVal);
            Assert.Equal("MY STRING", resultDto.StringVal);
        });
    }

}
