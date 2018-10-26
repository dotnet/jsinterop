// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Internal;
using Microsoft.JSInterop.MethodInfoCaching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides methods that receive incoming calls from JS to .NET.
    /// </summary>
    public static class DotNetDispatcher
    {
        private static readonly IMethodInfoCache _methodInfoCache;

        static DotNetDispatcher()
        {
            _methodInfoCache = new MethodInfoCache(
                instanceMethodInfoCache: new InstanceMethodInfoCache(),
                staticMethodInfoCache: new StaticMethodInfoCache());
        }

        /// <summary>
        /// Receives a call from JS to .NET, locating and invoking the specified method.
        /// </summary>
        /// <param name="assemblyName">The assembly containing the method to be invoked.</param>
        /// <param name="methodIdentifier">The identifier of the method to be invoked. The method must be annotated with a <see cref="JSInvokableAttribute"/> matching this identifier string.</param>
        /// <param name="dotNetObjectId">For instance method calls, identifies the target object.</param>
        /// <param name="argsJson">A JSON representation of the parameters.</param>
        /// <returns>A JSON representation of the return value, or null.</returns>
        public static string Invoke(string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            // DotNetDispatcher only works with JSRuntimeBase instances.
            var jsRuntime = (JSRuntimeBase)JSRuntime.Current;

            var targetInstance = (object)null;
            if (dotNetObjectId != default)
            {
                targetInstance = jsRuntime.ArgSerializerStrategy.FindDotNetObject(dotNetObjectId);
            }

            var syncResult = InvokeSynchronously(assemblyName, methodIdentifier, targetInstance, argsJson);
            return syncResult == null ? null : Json.Serialize(syncResult, jsRuntime.ArgSerializerStrategy);
        }

        /// <summary>
        /// Receives a call from JS to .NET, locating and invoking the specified method asynchronously.
        /// </summary>
        /// <param name="callId">A value identifying the asynchronous call that should be passed back with the result, or null if no result notification is required.</param>
        /// <param name="assemblyName">The assembly containing the method to be invoked.</param>
        /// <param name="methodIdentifier">The identifier of the method to be invoked. The method must be annotated with a <see cref="JSInvokableAttribute"/> matching this identifier string.</param>
        /// <param name="dotNetObjectId">For instance method calls, identifies the target object.</param>
        /// <param name="argsJson">A JSON representation of the parameters.</param>
        /// <returns>A JSON representation of the return value, or null.</returns>
        public static void BeginInvoke(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson)
        {
            // This method doesn't need [JSInvokable] because the platform is responsible for having
            // some way to dispatch calls here. The logic inside here is the thing that checks whether
            // the targeted method has [JSInvokable]. It is not itself subject to that restriction,
            // because there would be nobody to police that. This method *is* the police.

            // DotNetDispatcher only works with JSRuntimeBase instances.
            // If the developer wants to use a totally custom IJSRuntime, then their JS-side
            // code has to implement its own way of returning async results.
            var jsRuntimeBaseInstance = (JSRuntimeBase)JSRuntime.Current;

            var targetInstance = dotNetObjectId == default
                ? null
                : jsRuntimeBaseInstance.ArgSerializerStrategy.FindDotNetObject(dotNetObjectId);

            object syncResult = null;
            Exception syncException = null;

            try
            {
                syncResult = InvokeSynchronously(assemblyName, methodIdentifier, targetInstance, argsJson);
            }
            catch (Exception ex)
            {
                syncException = ex;
            }

            // If there was no callId, the caller does not want to be notified about the result
            if (callId != null)
            {
                // Invoke and coerce the result to a Task so the caller can use the same async API
                // for both synchronous and asynchronous methods
                var task = CoerceToTask(syncResult, syncException);

                task.ContinueWith(completedTask =>
                {
                    try
                    {
                        var result = TaskGenericsUtil.GetTaskResult(completedTask);
                        jsRuntimeBaseInstance.EndInvokeDotNet(callId, true, result);
                    }
                    catch (Exception ex)
                    {
                        ex = UnwrapException(ex);
                        jsRuntimeBaseInstance.EndInvokeDotNet(callId, false, ex);
                    }
                });
            }
        }

        private static Task CoerceToTask(object syncResult, Exception syncException)
        {
            if (syncException != null)
            {
                return Task.FromException(syncException);
            }
            else if (syncResult is Task syncResultTask)
            {
                return syncResultTask;
            }
            else
            {
                return Task.FromResult(syncResult);
            }
        }

        private static object InvokeSynchronously(string assemblyName, string methodIdentifier, object targetInstance, string argsJson)
        {
            if (targetInstance != null)
            {
                if (assemblyName != null)
                {
                    throw new ArgumentException($"For instance method calls, '{nameof(assemblyName)}' should be null. Value received: '{assemblyName}'.");
                }

                assemblyName = targetInstance.GetType().Assembly.GetName().Name;
            }

            MethodInfoCacheEntry cachedMethodInfo;
            if (targetInstance == null)
            {
                cachedMethodInfo = _methodInfoCache.GetStaticMethodInfo(assemblyName, methodIdentifier);
            }
            else
            {
                cachedMethodInfo = _methodInfoCache.GetInstanceMethodInfo(targetInstance.GetType(), methodIdentifier);
            }
            MethodInfo methodInfo = cachedMethodInfo.MethodInfo;
            IReadOnlyList<Type> parameterTypes = cachedMethodInfo.ParameterTypes;

            // There's no direct way to say we want to deserialize as an array with heterogenous
            // entry types (e.g., [string, int, bool]), so we need to deserialize in two phases.
            // First we deserialize as object[], for which SimpleJson will supply JsonObject
            // instances for nonprimitive values.
            var suppliedArgs = (object[])null;
            var suppliedArgsLength = 0;
            if (argsJson != null)
            {
                suppliedArgs = Json.Deserialize<SimpleJson.JsonArray>(argsJson).ToArray<object>();
                suppliedArgsLength = suppliedArgs.Length;
            }
            if (suppliedArgsLength != parameterTypes.Count)
            {
                throw new ArgumentException($"In call to '{methodIdentifier}', " +
                    $"expected {parameterTypes.Count} parameters but received {suppliedArgsLength}.");
            }

            // Second, convert each supplied value to the type expected by the method
            var runtime = (JSRuntimeBase)JSRuntime.Current;
            var serializerStrategy = runtime.ArgSerializerStrategy;
            for (var i = 0; i < suppliedArgsLength; i++)
            {
                if (parameterTypes[i] == typeof(JSAsyncCallResult))
                {
                    // For JS async call results, we have to defer the deserialization until
                    // later when we know what type it's meant to be deserialized as
                    suppliedArgs[i] = new JSAsyncCallResult(suppliedArgs[i]);
                }
                else
                {
                    suppliedArgs[i] = serializerStrategy.DeserializeObject(
                        suppliedArgs[i], parameterTypes[i]);
                }
            }

            try
            {
                return methodInfo.Invoke(targetInstance, suppliedArgs);
            }
            catch (Exception ex)
            {
                throw UnwrapException(ex);
            }
        }

        /// <summary>
        /// Receives notification that a call from .NET to JS has finished, marking the
        /// associated <see cref="Task"/> as completed.
        /// </summary>
        /// <param name="asyncHandle">The identifier for the function invocation.</param>
        /// <param name="succeeded">A flag to indicate whether the invocation succeeded.</param>
        /// <param name="result">If <paramref name="succeeded"/> is <c>true</c>, specifies the invocation result. If <paramref name="succeeded"/> is <c>false</c>, gives the <see cref="Exception"/> corresponding to the invocation failure.</param>
        [JSInvokable(nameof(DotNetDispatcher) + "." + nameof(EndInvoke))]
        public static void EndInvoke(long asyncHandle, bool succeeded, JSAsyncCallResult result)
            => ((JSRuntimeBase)JSRuntime.Current).EndInvokeJS(asyncHandle, succeeded, result.ResultOrException);

        /// <summary>
        /// Releases the reference to the specified .NET object. This allows the .NET runtime
        /// to garbage collect that object if there are no other references to it.
        ///
        /// To avoid leaking memory, the JavaScript side code must call this for every .NET
        /// object it obtains a reference to. The exception is if that object is used for
        /// the entire lifetime of a given user's session, in which case it is released
        /// automatically when the JavaScript runtime is disposed.
        /// </summary>
        /// <param name="dotNetObjectId">The identifier previously passed to JavaScript code.</param>
        [JSInvokable(nameof(DotNetDispatcher) + "." + nameof(ReleaseDotNetObject))]
        public static void ReleaseDotNetObject(long dotNetObjectId)
        {
            // DotNetDispatcher only works with JSRuntimeBase instances.
            var jsRuntime = (JSRuntimeBase)JSRuntime.Current;
            jsRuntime.ArgSerializerStrategy.ReleaseDotNetObject(dotNetObjectId);
        }

        private static Exception UnwrapException(Exception ex)
        {
            while ((ex is AggregateException || ex is TargetInvocationException) && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }
    }
}
