namespace Microsoft.JSInterop.MethodInfoCaching
{
    /// <summary>
    /// A cache for quickly looking up invokable static methods within an assembly.
    /// </summary>
    internal interface IStaticMethodInfoCache
    {
        /// <summary>
        /// Gets the cache MethodInfo for a static method within an assembly.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly in which the method is declared.</param>
        /// <param name="methodIdentifier">The assembly-unique identifier of the method to retrieve information about.</param>
        /// <returns>The <see cref="MethodInfoCacheEntry"/> for the method identifier within the named assembly.</returns>
        MethodInfoCacheEntry Get(string assemblyName, string methodIdentifier);
    }
}