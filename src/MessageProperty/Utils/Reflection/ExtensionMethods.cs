namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;

    static class TypeExtensionMethods
    {
        static bool IsClrType(byte[] a1)
        {
            IStructuralEquatable structuralEquatable = a1;
            return structuralEquatable.Equals(MsPublicKeyToken, StructuralComparisons.StructuralEqualityComparer);
        }

        public static bool IsSystemType(this Type type)
        {
            bool result;

            if (!IsSystemTypeCache.TryGetValue(type.TypeHandle, out result))
            {
                var nameOfContainingAssembly = type.Assembly.GetName().GetPublicKeyToken();
                IsSystemTypeCache[type.TypeHandle] = result = IsClrType(nameOfContainingAssembly);
            }

            return result;
        }

        static byte[] MsPublicKeyToken = typeof(string).Assembly.GetName().GetPublicKeyToken();

        static ConcurrentDictionary<RuntimeTypeHandle, bool> IsSystemTypeCache = new ConcurrentDictionary<RuntimeTypeHandle, bool>();
    }
}