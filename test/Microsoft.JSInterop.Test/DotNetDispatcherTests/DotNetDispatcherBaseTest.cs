// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

//TODO: Separate static / instance tests
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.JSInterop.Test.DotNetDispatcherTests
{
    public class DotNetDispatcherBaseTest
    {
        protected readonly static string ThisAssemblyName
            = typeof(DotNetDispatcherBaseTest).Assembly.GetName().Name;
        protected readonly TestJSRuntime ThisJSRuntime
            = new TestJSRuntime();



        // Instance method tests


        [Fact]
        public Task CanInvokeAsyncMethod() => WithJSRuntime(async jsRuntime =>
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var targetInstance = new SomePublicType();
            var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance), new DotNetObjectRef(arg2));

            // Arrange: all args
            var argsJson = Json.Serialize(new object[]
            {
                new TestDTO { IntVal = 1000, StringVal = "String via JSON" },
                "__dotNetObject:2"
            });

            // Act
            var callId = "123";
            var resultTask = jsRuntime.NextInvocationTask;
            DotNetDispatcher.BeginInvoke(callId, null, "InvokableAsyncMethod", 1, argsJson);
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
            var resultDto1 = (TestDTO)jsRuntime.ArgSerializerStrategy.DeserializeObject(resultValue[0], typeof(TestDTO));
            Assert.Equal("STRING VIA JSON", resultDto1.StringVal);
            Assert.Equal(2000, resultDto1.IntVal);

            // Assert: Second result value marshalled by ref
            var resultDto2Ref = (string)resultValue[1];
            Assert.Equal("__dotNetObject:3", resultDto2Ref);
            var resultDto2 = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal("MY STRING", resultDto2.StringVal);
            Assert.Equal(2468, resultDto2.IntVal);
        });

        // Supporting methods and classes

        protected Task WithJSRuntime(Action<TestJSRuntime> testCode)
        {
            return WithJSRuntime(jsRuntime =>
            {
                testCode(jsRuntime);
                return Task.CompletedTask;
            });
        }

        protected async Task WithJSRuntime(Func<TestJSRuntime, Task> testCode)
        {
            // Since the tests rely on the asynclocal JSRuntime.Current, ensure we
            // are on a distinct async context with a non-null JSRuntime.Current
            await Task.Yield();

            var runtime = new TestJSRuntime();
            JSRuntime.SetCurrentJSRuntime(runtime);
            await testCode(runtime);
        }


        public class TestJSRuntime : JSInProcessRuntimeBase
        {
            private TaskCompletionSource<object> _nextInvocationTcs = new TaskCompletionSource<object>();
            public Task NextInvocationTask => _nextInvocationTcs.Task;
            public long LastInvocationAsyncHandle { get; private set; }
            public string LastInvocationIdentifier { get; private set; }
            public string LastInvocationArgsJson { get; private set; }

            protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
            {
                LastInvocationAsyncHandle = asyncHandle;
                LastInvocationIdentifier = identifier;
                LastInvocationArgsJson = argsJson;
                _nextInvocationTcs.SetResult(null);
                _nextInvocationTcs = new TaskCompletionSource<object>();
            }

            protected override string InvokeJS(string identifier, string argsJson)
            {
                LastInvocationAsyncHandle = default;
                LastInvocationIdentifier = identifier;
                LastInvocationArgsJson = argsJson;
                _nextInvocationTcs.SetResult(null);
                _nextInvocationTcs = new TaskCompletionSource<object>();
                return null;
            }
        }
    }
}
