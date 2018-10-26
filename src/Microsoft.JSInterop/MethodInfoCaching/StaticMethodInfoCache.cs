using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// <see cref="IStaticMethodInfoCache"/>
    /// </summary>
    internal class StaticMethodInfoCache : IStaticMethodInfoCache
    {
        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, MethodInfoCacheEntry>> _lookupsByAssemblyName
            = new ConcurrentDictionary<string, IReadOnlyDictionary<string, MethodInfoCacheEntry>>();

        /// <summary>
        /// <see cref="IStaticMethodInfoCache.Get(string, string)"/>
        /// </summary>
        public MethodInfoCacheEntry Get(string assemblyName, string methodIdentifier)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(assemblyName));
            }
            if (string.IsNullOrWhiteSpace(methodIdentifier))
            {
                throw new ArgumentException("Cannot be null, empty, or whitespace.", nameof(methodIdentifier));
            }

            IReadOnlyDictionary<string, MethodInfoCacheEntry> cacheItemsByMethodIdentifier =
                _lookupsByAssemblyName.GetOrAdd(assemblyName, ScanAssembly);
            MethodInfoCacheEntry result;
            if (!cacheItemsByMethodIdentifier.TryGetValue(methodIdentifier, out result))
            {
                throw new InvalidOperationException($"The assembly '{assemblyName}' does not contain a class with " +
                    $"a static method decorated with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].");
            }
            return result;
        }

        /// <summary>
        /// Scans the specified assembly for all static methods decorated with [<see cref="JSInvokableAttribute"/>].
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns>A dictionary of <see cref="MethodInfoCacheEntry"/> keyed by method identifier.</returns>
        private IReadOnlyDictionary<string, MethodInfoCacheEntry> ScanAssembly(string assemblyName)
        {
            // TODO: Consider looking first for assembly-level attributes (i.e., if there are any,
            // only use those) to avoid scanning, especially for framework assemblies.
            var result = new Dictionary<string, MethodInfoCacheEntry>();
            // For static invokable methods we need instance methods decorated with [JSInvokable].
            // Because the method identifier must be unique within the assembly we cannot inherit
            // any JSInvokable attributes.
            IEnumerable<MethodInfo> invokableMethods = GetRequiredLoadedAssembly(assemblyName)
                .GetExportedTypes()
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Static))
                .Where(method => method.IsDefined(typeof(JSInvokableAttribute), inherit: false));
            foreach (var methodInfo in invokableMethods)
            {
                EnsureMethodIsInvokable(methodInfo);

                string methodIdentifier = methodInfo.GetCustomAttribute<JSInvokableAttribute>(false).Identifier ?? methodInfo.Name;
                try
                {
                    var cacheItem = new MethodInfoCacheEntry(methodInfo);
                    result.Add(methodIdentifier, cacheItem);
                }
                catch (ArgumentException)
                {
                    if (result.ContainsKey(methodIdentifier))
                    {
                        throw new InvalidOperationException($"The assembly '{assemblyName}' contains more than one " +
                            $"[{nameof(JSInvokableAttribute)}] method with identifier '{methodIdentifier}'. " +
                            $"All [{nameof(JSInvokableAttribute)}] methods on static methods within the same assembly " +
                            $"must have different identifiers. You can pass a custom identifier as a parameter to " +
                            $"the [{nameof(JSInvokableAttribute)}] attribute.");
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
        /// Throws an exception if the class or the method are generic.
        /// </summary>
        /// <param name="methodInfo">
        /// The <see cref="MethodInfo"/> to ensure is statically invokable.
        /// </param>
        private static void EnsureMethodIsInvokable(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType.GetGenericArguments().Length > 0)
            {
                throw new InvalidOperationException($"Static methods of class '{methodInfo.DeclaringType.Name}' " +
                    $"cannot be decorated with {nameof(JSInvokableAttribute)} because the class is generic.");
            }
            if (methodInfo.GetGenericArguments().Length > 0)
            {
                throw new InvalidOperationException($"The static method '{methodInfo.DeclaringType.Name}.{methodInfo.Name}' " +
                    $"cannot be decorated with {nameof(JSInvokableAttribute)} because it is generic.");
            }
        }

        /// <summary>
        /// Gets an <see cref="Assembly"/> reference by name. Only if the assembly is already loaded.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        private static Assembly GetRequiredLoadedAssembly(string assemblyName)
        {
            // We don't want to load assemblies on demand here, because we don't necessarily trust
            // "assemblyName" to be something the developer intended to load. So only pick from the
            // set of already-loaded assemblies.
            // In some edge cases this might force developers to explicitly call something on the
            // target assembly (from .NET) before they can invoke its allowed methods from JS.
            IEnumerable<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            return loadedAssemblies.FirstOrDefault(a => a.GetName().Name.Equals(assemblyName, StringComparison.Ordinal))
                ?? throw new ArgumentException($"There is no loaded assembly with the name '{assemblyName}'.");
        }
    }

}
