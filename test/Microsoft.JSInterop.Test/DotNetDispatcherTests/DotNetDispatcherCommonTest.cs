using System;
using Xunit;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public class DotNetDispatcherCommonTest : DotNetDispatcherBaseTest
    {
        [Fact]
        public void CannotInvokeWithEmptyAssemblyNameAndNoObjectId()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(" ", "SomeMethod", default, "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("assemblyName", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeWithEmptyMethodIdentifier()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke("SomeAssembly", " ", default, "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("methodIdentifier", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeWithIncorrectNumberOfParams()
        {
            // Arrange
            string argsJson = Json.Serialize(new object[] { 1, 2, 3, 4 });

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(ThisAssemblyName, TestModelMethodNames.PublicStaticClass_PublicStaticNonVoidMethodWithParams, default, argsJson);
            });

            Assert.Equal($"In call to '{TestModelMethodNames.PublicStaticClass_PublicStaticNonVoidMethodWithParams}', expected 3 parameters but received 4.", ex.Message);
        }
    }

}
