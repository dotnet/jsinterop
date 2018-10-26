using System;

namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// Cached information about a <see cref="MethodInfo"/> class, to avoid reflection lookups.
    /// </summary>
    internal interface IInstanceMethodInfoCache
    {
        /// <summary>
        /// Gets the cached MethodInfo for a specific class and method identifier.
        /// </summary>
        /// <param name="classType">The class type to search for the method.</param>
        /// <param name="methodIdentifier">The identifier of the method to retrieve information about.</param>
        /// <returns>Cached information about the method.</returns>
        MethodInfoCacheEntry Get(Type classType, string methodIdentifier);
        /// <summary>
        /// Gets the cached MethodInfo for a specific class and method identifier.
        /// </summary>
        /// <param name="classType">The class type to search for the method identifier.</param>
        /// <param name="methodIdentifier">The identifier of the method to retrieve information about.</param>
        /// <param name="result">Cached information about the method.</param>
        /// <returns>True if the method identifier was found on the class, otherwise false</returns>
        bool TryGet(Type classType, string methodIdentifier, out MethodInfoCacheEntry result);
    }
}