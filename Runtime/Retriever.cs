using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AttributeGroup = System.Collections.Generic.Dictionary<
    System.Type,
    com.bbbirder.DirectRetrieveAttribute[]
>;

[assembly: InternalsVisibleTo("com.bbbirder.directattribute.Editor")]

namespace com.bbbirder
{
    public static class Retriever
    {
        private static BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly
            ;
        private static WeakHolder<AttributeGroup> m_attrLut;

        private static WeakHolder<AttributeGroup> attrLut => m_attrLut ??= new(() =>
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(IsValidAssembly)
                .SelectMany(GetRecords)
                .SelectMany(record => GetBaseAttributes(record, typeof(DirectRetrieveAttribute)))
                .GroupBy(a => a.GetType() ?? typeof(object), a => a)
                .ToDictionary(e => e.Key, e => e.ToArray());
        });

        private static WeakHolder<Type[]> m_typeSet;

        private static WeakHolder<Type[]> typeSet => m_typeSet ??= new(() =>
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(IsValidAssembly)
                .SelectMany(GetRecords)
                .Select(record => record.type)
                .Distinct()
                .ToArray()
                ;
        });

        internal static bool IsValidAssembly(Assembly assembly)
        {
            return assembly.IsDefined(typeof(AssemblyMetadataAttribute))
                && assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(attr => attr.Key == "direct-attribute")
                ;
        }

        internal static (Type type, string memberName)[] GetRecords(Assembly assembly)
        {
            var type = assembly.GetType("com.bbbirder.RetrievableMetadata");
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            return type.GetField("records", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(null) as (Type type, string memberName)[];
        }

        private static IEnumerable<DirectRetrieveAttribute> GetBaseAttributes((Type type, string memberName) record, Type attributeType)
        {
            var (type, memberName) = record;
            if (memberName != null)
            {
                return type.GetMember(memberName, bindingFlags).SelectMany(
                    member => member.GetCustomAttributes(attributeType, false).Select(
                        ca => SetAttributeValue(ca as DirectRetrieveAttribute, member)
                    )
                );
            }
            else
            {
                return type.GetCustomAttributes(attributeType, false).Select(
                    ca => SetAttributeValue(ca as DirectRetrieveAttribute, type)
                );
            }
        }

        /// <summary>
        /// Retrieve attributes of type T in all loaded assemblies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAllAttributes<T>() where T : DirectRetrieveAttribute
        {
            return GetAllAttributes(typeof(T)).OfType<T>().ToArray();
        }

        /// <summary>
        /// Retrieve attributes of target type in all loaded assemblies
        /// </summary>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static DirectRetrieveAttribute[] GetAllAttributes(Type attributeType)
        {
#if DEBUG
            CheckAttribute(attributeType);
#endif
            return attrLut.Value.ToArray()
                .Where(a => attributeType.IsAssignableFrom(a.Key))
                .SelectMany(a => a.Value)
                .ToArray()
                ;
        }

        /// <summary>
        /// Retrieve attributes of target type in a specific assembly
        /// </summary>
        /// <param name="attributeType"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static DirectRetrieveAttribute[] GetAllAttributes(Type attributeType, Assembly assembly)
        {
#if DEBUG
            CheckAttribute(attributeType);
#endif
            if (!IsValidAssembly(assembly))
                return Array.Empty<DirectRetrieveAttribute>();
            return GetRecords(assembly)
                .SelectMany(record => GetBaseAttributes(record, attributeType))
                .ToArray();
        }

        /// <summary>
        /// Retrieve attributes of type T in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAllAttributes<T>(Assembly assembly) where T : DirectRetrieveAttribute
        {
            var attrType = typeof(T);
            return GetAllAttributes(attrType, assembly).OfType<T>().ToArray();
        }


        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type[] GetAllSubtypes(Type baseType)
        {
            return typeSet.Value.Where(t => t != baseType)
                .Distinct()
                .Where(t => t != baseType && IsTypeOf(t, baseType))
                .ToArray()
                ;
        }

        private static bool IsTypeOf(Type type, Type baseType)
        {
            if (baseType.IsGenericTypeDefinition)
            {
                return GetTypesTowardsBase(type).Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == baseType)
                         || type.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == baseType)
                    ;
            }
            else
            {
                return baseType.IsAssignableFrom(type);
            }
            static IEnumerable<Type> GetTypesTowardsBase(Type type)
            {
                while (type != null)
                {
                    yield return type;
                    type = type.BaseType;
                }
            }
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Type[] GetAllSubtypes<T>()
        {
            return GetAllSubtypes(typeof(T));
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Type[] GetAllSubtypes<T>(Assembly assembly)
        {
            return GetAllSubtypes(typeof(T), assembly);
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type[] GetAllSubtypes(Type baseType, Assembly assembly)
        {
            if (!IsValidAssembly(assembly))
                return Array.Empty<Type>();

            return GetRecords(assembly)
                .Select(record => record.type)
                .Distinct()
                .Where(t => t != baseType && IsTypeOf(t, baseType))
                .ToArray();
        }

        private static void CheckAttribute(Type attributeType)
        {
            if (!typeof(DirectRetrieveAttribute).IsAssignableFrom(attributeType))
            {
                throw new($"type {attributeType} is not a DirectRetrieveAttribute");
            }
        }

        // [Conditional("DIRECT_RETRIEVE_ATTRIBUTE_STRICT")]
        //static void CheckBasetype(Type baseType)
        //{
        //    if (!IsTypeRetrievable(baseType))
        //    {
        //        throw new($"type {baseType} is not retrievable, which should inherit from IDirectRetrieve");
        //    }
        //}

        // public static bool IsTypeRetrievable(Type type)
        // {
        //     return IsBaseType(type, typeof(IDirectRetrieve));
        // }

        // private static bool IsBaseType(Type subType, Type baseType)
        // {
        //     if (subType == baseType)
        //         return false;

        //     if (baseType.IsInterface)
        //         return baseType.IsAssignableFrom(subType);
        //     else
        //         return subType.IsSubclassOf(baseType);
        // }

        private static T SetAttributeValue<T>(T attr, MemberInfo targetInfo) where T : DirectRetrieveAttribute
        {
            attr.targetInfo = targetInfo;
            attr.OnReceiveTarget();
            return attr;
        }
    }
}
