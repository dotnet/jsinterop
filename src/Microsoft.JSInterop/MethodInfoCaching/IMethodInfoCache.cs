using System;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// A cache for quickly looking up invokable methods.
    /// </summary>
    internal interface IMethodInfoCache
    {
        /// <summary>
        /// <see cref="IInstanceMethodInfoCache.Get(Type, string)"/>
        /// </summary>
        MethodInfoCacheEntry GetInstanceMethodInfo(Type classType, string methodIdentifier);
        /// <summary>
        /// <see cref="IStaticMethodInfoCache.Get(string, string)"/>
        /// </summary>
        MethodInfoCacheEntry GetStaticMethodInfo(string assemblyName, string methodIdentifier);
        /// <summary>
        /// <see cref="IInstanceMethodInfoCache.TryGet(Type, string, out MethodInfoCacheEntry)"/>
        /// </summary>
        bool TryGetInstanceMethodInfo(Type classType, string methodIdentifier, out MethodInfoCacheEntry result);
    }
}