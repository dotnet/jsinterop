using System;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// <see cref="IMethodInfoCache"/>
    /// </summary>
    internal class MethodInfoCache : IMethodInfoCache
    {
        private readonly IInstanceMethodInfoCache InstanceMethodInfoCache;
        private readonly IStaticMethodInfoCache StaticMethodInfoCache;

        /// <summary>
        /// Constructs an instance of <see cref="MethodInfoCache"/>.
        /// </summary>
        /// <param name="instanceMethodInfoCache">Cache for instance methods.</param>
        /// <param name="staticMethodInfoCache">Cache for static methods.</param>
        public MethodInfoCache(IInstanceMethodInfoCache instanceMethodInfoCache, IStaticMethodInfoCache staticMethodInfoCache)
        {
            InstanceMethodInfoCache = instanceMethodInfoCache ?? throw new ArgumentNullException(nameof(instanceMethodInfoCache));
            StaticMethodInfoCache = staticMethodInfoCache ?? throw new ArgumentNullException(nameof(staticMethodInfoCache));
        }

        /// <summary>
        /// <see cref="IMethodInfoCache.GetInstanceMethodInfo(Type, string)"/>
        /// </summary>
        public MethodInfoCacheEntry GetInstanceMethodInfo(Type classType, string methodIdentifier)
            => InstanceMethodInfoCache.Get(classType, methodIdentifier);

        /// <summary>
        /// <see cref="IMethodInfoCache.TryGetInstanceMethodInfo(Type, string, out MethodInfoCacheEntry)"/>
        /// </summary>
        public bool TryGetInstanceMethodInfo(Type classType, string methodIdentifier, out MethodInfoCacheEntry result)
            => InstanceMethodInfoCache.TryGet(classType, methodIdentifier, out result);

        /// <summary>
        /// <see cref="IMethodInfoCache.GetStaticMethodInfo(string, string)"/>
        /// </summary>
        public MethodInfoCacheEntry GetStaticMethodInfo(string assemblyName, string methodIdentifier)
            => StaticMethodInfoCache.Get(assemblyName, methodIdentifier);
    }

}
