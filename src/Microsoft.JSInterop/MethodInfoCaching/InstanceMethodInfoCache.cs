using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// A cache for quickly looking up invokable instance methods on a class.
    /// </summary>
    internal class InstanceMethodInfoCache : IInstanceMethodInfoCache
    {
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, MethodInfoCacheEntry>> _lookup
            = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, MethodInfoCacheEntry>>();

        /// <summary>
        /// <see cref="IInstanceMethodInfoCache.Get(Type, string)"/>
        /// </summary>
        public MethodInfoCacheEntry Get(Type classType, string methodIdentifier)
        {
            MethodInfoCacheEntry result;
            if (!TryGet(classType, methodIdentifier, out result))
            {
                throw new ArgumentException($"The class '{classType.Name}' does not contain a method" +
                    $" with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
            }
            return result;
        }

        /// <summary>
        /// <see cref="IInstanceMethodInfoCache.TryGet(Type, string, out MethodInfoCacheEntry)"/>
        /// </summary>
        public bool TryGet(Type classType, string methodIdentifier, out MethodInfoCacheEntry result)
        {
            IReadOnlyDictionary<string, MethodInfoCacheEntry> cacheItemByMethodIdentifier =
                _lookup.GetOrAdd(classType, ScanClassForInvokableMethods);
            return cacheItemByMethodIdentifier.TryGetValue(methodIdentifier, out result);
        }

        /// <summary>
        /// Scans a class for all methods decorated with [<see cref="JSInvokableAttribute"/>]
        /// </summary>
        /// <param name="classType">The class type to search for the method identifier.</param>
        /// <returns>A collection of cached method info indexed by method identifier string.</returns>
        private IReadOnlyDictionary<string, MethodInfoCacheEntry> ScanClassForInvokableMethods(Type classType)
        {
            var result = new Dictionary<string, MethodInfoCacheEntry>();
            // For instance invokable methods we need public instance methods decorated with [JSInvokable]
            // including any that have been inherited
            IEnumerable<MethodInfo> invokableMethods = classType
                .GetMethods(
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .Where(method => method.IsDefined(typeof(JSInvokableAttribute), inherit: true));
            foreach (MethodInfo methodInfo in invokableMethods)
            {
                EnsureMethodIsInvokable(classType, methodInfo);

                string methodIdentifier = methodInfo.GetCustomAttribute<JSInvokableAttribute>(true).Identifier?? methodInfo.Name;
                var cacheItem = new MethodInfoCacheEntry(methodInfo);
                try
                {
                    result.Add(methodIdentifier, cacheItem);
                }
                catch (ArgumentException)
                {
                    if (result.ContainsKey(methodIdentifier))
                    {
                        throw new InvalidOperationException($"The class '{classType.Name}' contains more than one " +
                            $"[{nameof(JSInvokableAttribute)}] method with identifier '{methodIdentifier}'. " +
                            $"All instance methods within the same class must have different identifiers.");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Throws an exception if the class has open generic types or the method is generic.
        /// </summary>
        /// <param name="classType">The class type to which the method info belongs.</param>
        /// <param name="methodInfo">The method info to ensure is invokable.</param>
        private void EnsureMethodIsInvokable(Type classType, MethodInfo methodInfo)
        {
            // Prohibit the calling of methods on classes with undefined generic arguments.
            // As this is ultimately called from an instance this shouldn't be possible.
            if (classType.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"Cannot determine generic argument types for class '{classType.Name}'");
            }
            // Prohibit the calling of methods with generic parameters
            if (methodInfo.GetGenericArguments().Length > 0)
            {
                throw new InvalidOperationException($"Cannot determine generic argument types " +
                    $"for method '{classType.Name}.{methodInfo.Name}'");
            }
        }
    }

}
