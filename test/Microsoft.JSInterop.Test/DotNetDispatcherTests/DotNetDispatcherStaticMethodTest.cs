using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public class DotNetDispatcherStaticMethodTest : DotNetDispatcherBaseTest
    {
        [Fact]
        public Task CanInvokeVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange/Act
            SomePublicType.DidInvokeMyInvocableStaticVoid = false;
            var resultJson = DotNetDispatcher.Invoke(ThisAssemblyName, "InvocableStaticVoid", default, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(SomePublicType.DidInvokeMyInvocableStaticVoid);
        });

        // Static method tests
        [Fact]
        public void CannotInvokeMethodsOnUnloadedAssembly()
        {
            var assemblyName = "Some.Fake.Assembly";
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(assemblyName, "SomeMethod", default, null);
            });

            Assert.Equal($"There is no loaded assembly with the name '{assemblyName}'.", ex.Message);
        }

        [Fact]
        public Task CanInvokeNonVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange/Act
            var resultJson = DotNetDispatcher.Invoke(ThisAssemblyName, "InvocableStaticNonVoid", default, null);
            var result = Json.Deserialize<TestDTO>(resultJson);

            // Assert
            Assert.Equal("Test", result.StringVal);
            Assert.Equal(123, result.IntVal);
        });

        [Fact]
        public Task CanInvokeMethodWithParams() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track a .NET object to use as an arg
            var arg3 = new TestDTO { IntVal = 999, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(arg3));

            // Arrange: Remaining args
            var argsJson = Json.Serialize(new object[] {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
                "__dotNetObject:1"
            });

            // Act
            var resultJson = DotNetDispatcher.Invoke(ThisAssemblyName, "InvocableStaticWithParams", default, argsJson);
            var result = Json.Deserialize<object[]>(resultJson);

            // Assert: First result value marshalled via JSON
            var resultDto1 = (TestDTO)jsRuntime.ArgSerializerStrategy.DeserializeObject(result[0], typeof(TestDTO));
            Assert.Equal("ANOTHER STRING", resultDto1.StringVal);
            Assert.Equal(756, resultDto1.IntVal);

            // Assert: Second result value marshalled by ref
            var resultDto2Ref = (string)result[1];
            Assert.Equal("__dotNetObject:2", resultDto2Ref);
            var resultDto2 = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(2);
            Assert.Equal("MY STRING", resultDto2.StringVal);
            Assert.Equal(1299, resultDto2.IntVal);
        });
    }

}
