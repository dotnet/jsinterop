using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// Cached information about a <see cref="MethodInfo"/> class, to avoid reflection lookups.
    /// </summary>
    internal class MethodInfoCacheEntry
    {
        /// <summary>
        /// The MethodInfo that this cache entry holds information about.
        /// </summary>
        public readonly MethodInfo MethodInfo;
        /// <summary>
        /// The types of the parameters the method requires, in parameter order.
        /// </summary>
        public readonly IReadOnlyCollection<Type> ParameterTypes;
        /// <summary>
        /// The return type of the method.
        /// </summary>
        public readonly Type ReturnType;

        /// <summary>
        /// Constructs an instance of <see cref="MethodInfoCacheEntry"/>.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> to be cached.</param>
        public MethodInfoCacheEntry(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
            ReturnType = methodInfo.ReturnType;
            ParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToList().AsReadOnly();
        }

        /// <summary>
        /// A string representation of the cache entry.
        /// </summary>
        /// <returns>A string representing the declaring type name and method name.</returns>
        public override string ToString()
        {
            return $"{MethodInfo.DeclaringType.Name}.{MethodInfo.Name}";
        }
    }

}
