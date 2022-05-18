namespace NServiceBus.Encryption.MessageProperty
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;

    class EncryptionInspector
    {
        public EncryptionInspector(IsEncryptedPropertyConvention encryptedPropertyConvention)
        {
            this.encryptedPropertyConvention = encryptedPropertyConvention;
        }

        static bool IsIndexedProperty(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;

            return propertyInfo?.GetIndexParameters().Length > 0;
        }

        bool IsEncryptedMember(MemberInfo arg)
        {
            var propertyInfo = arg as PropertyInfo;
            if (propertyInfo != null)
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    if (encryptedPropertyConvention.IsEncryptedProperty(propertyInfo))
                    {
                        throw new Exception("Cannot encrypt or decrypt indexed properties that return a WireEncryptedString.");
                    }

                    return false;
                }

                return encryptedPropertyConvention.IsEncryptedProperty(propertyInfo);
            }

            var fieldInfo = arg as FieldInfo;
            if (fieldInfo != null)
            {
                return fieldInfo.FieldType == typeof(EncryptedString);
            }

            return false;
        }

        public List<Tuple<object, MemberInfo>> ScanObject(object root)
        {
            var visitedMembers = new HashSet<object>();
            return ScanObject(root, visitedMembers);
        }

        List<Tuple<object, MemberInfo>> ScanObject(object root, HashSet<object> visitedMembers)
        {
            if (root == null || visitedMembers.Contains(root))
            {
                return AlreadyVisited;
            }

            visitedMembers.Add(root);

            var members = GetFieldsAndProperties(root);

            var properties = new List<Tuple<object, MemberInfo>>();

            foreach (var member in members)
            {
                if (IsEncryptedMember(member) && member.GetValue(root) != null)
                {
                    var value = member.GetValue(root);
                    if (value is string or EncryptedString)
                    {
                        properties.Add(Tuple.Create(root, member));
                        continue;
                    }
                    throw new Exception("Only string properties are supported for convention based encryption. Check the configured conventions.");
                }

                //don't recurse over primitives or system types
                if (member.ReflectedType.IsPrimitive || member.ReflectedType.IsSystemType())
                {
                    continue;
                }

                // don't try to recurse over members of WireEncryptedString
                if (member.DeclaringType == typeof(EncryptedString))
                {
                    continue;
                }

                if (IsIndexedProperty(member))
                {
                    continue;
                }

                var child = member.GetValue(root);

                if (child is IEnumerable items)
                {
                    foreach (var item in items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        //don't recurse over primitives or system types
                        if (item.GetType().IsPrimitive || item.GetType().IsSystemType())
                        {
                            break;
                        }

                        properties.AddRange(ScanObject(item, visitedMembers));
                    }
                }
                else
                {
                    properties.AddRange(ScanObject(child, visitedMembers));
                }
            }
            return properties;
        }

        static List<MemberInfo> GetFieldsAndProperties(object target)
        {
            if (target == null)
            {
                return NoMembers;
            }

            return cache.GetOrAdd(target.GetType().TypeHandle, typeHandle =>
            {
                var messageType = Type.GetTypeFromHandle(typeHandle);
                var members = new List<MemberInfo>();
                foreach (var member in messageType.GetMembers(BindingFlags.Public | BindingFlags.Instance))
                {
                    var fieldInfo = member as FieldInfo;
                    if (fieldInfo != null && !fieldInfo.IsInitOnly)
                    {
                        members.Add(fieldInfo);
                    }

                    var propInfo = member as PropertyInfo;
                    if (propInfo != null && propInfo.CanWrite)
                    {
                        members.Add(propInfo);
                    }
                }
                return members;
            });
        }

        IsEncryptedPropertyConvention encryptedPropertyConvention;

        static List<MemberInfo> NoMembers = new List<MemberInfo>(0);

        static List<Tuple<object, MemberInfo>> AlreadyVisited = new List<Tuple<object, MemberInfo>>(0);

        static ConcurrentDictionary<RuntimeTypeHandle, List<MemberInfo>> cache = new ConcurrentDictionary<RuntimeTypeHandle, List<MemberInfo>>();
    }
}