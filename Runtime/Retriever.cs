using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using AttributeGroup = System.Collections.Generic.Dictionary<
    System.Type,
    com.bbbirder.DirectRetrieveAttribute[]
>;
//using TypeSet = System.Collections.Generic.HashSet<System.Type>;


namespace com.bbbirder
{
    public static class Retriever
    {
        static BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly
            ;
        static WeakHolder<AttributeGroup> m_attrLut;
        static WeakHolder<AttributeGroup> attrLut => m_attrLut ??= new(() =>
        {
            // var al = AppDomain.CurrentDomain.GetAssemblies()
            //     .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute),false))
            //     .SelectMany(a => a.GetCustomAttributes<GeneratedDirectRetrieveAttribute>())
            //     .Select(a => a.type)
            //     .Distinct()
            //     .ToArray();
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute)))
                .SelectMany(a => GetAllAttributes<DirectRetrieveAttribute>(a))
                .GroupBy(a => a.GetType() ?? typeof(object), a => a)
                .ToDictionary(e => e.Key, e => e.ToArray());
        });
        static WeakHolder<Type[]> m_typeSet;
        static WeakHolder<Type[]> typeSet => m_typeSet ??= new(() =>
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute), false))
                .SelectMany(a => a.GetCustomAttributes<GeneratedDirectRetrieveAttribute>())
                .Select(a => a.type)
                .Distinct()
                .ToArray();
        });


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
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .SelectMany(a =>
                {
                    var targetType = a.type;
                    IEnumerable<DirectRetrieveAttribute> result;
                    if (a.HasMemberName)
                    {
                        result = targetType.GetMember(a.memberName, bindingFlags).SelectMany(
                            member => member.GetCustomAttributes(attributeType, false).Select(
                                ca => SetAttributeValue(ca as DirectRetrieveAttribute, member)
                            )
                        );
                    }
                    else
                    {
                        result = targetType.GetCustomAttributes(attributeType, false).Select(
                            ca => SetAttributeValue(ca as DirectRetrieveAttribute, targetType)
                        );
                    }
                    if (result.Count() == 0)
                    {
                        throw new("cannot find attribute but the assembly metadata preset, please check your script-strip setting");
                    }
                    return result;
                })
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
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .SelectMany(a =>
                {
                    var targetType = a.type;
                    if (a.HasMemberName)
                        return targetType.GetMember(a.memberName, bindingFlags).SelectMany(
                            member => member.GetCustomAttributes<T>(false).Select(
                                ca => SetAttributeValue(ca, member)
                            )
                        );
                    else
                        return targetType.GetCustomAttributes<T>(false).Select(
                            ca => SetAttributeValue(ca, targetType)
                        );
                })
                .ToArray();
        }


        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type[] GetAllSubtypes(Type baseType)
        {
#if DEBUG
            CheckBasetype(baseType);
#endif
            if (baseType.IsGenericTypeDefinition)
            {
                return typeSet.Value
                    .Where(a => a != baseType)
                    .Where(a => GetTypesTowardsBase(a).Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == baseType)
                     || a.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == baseType))
                    .ToArray()
                    ;
            }
            else
            {
                return typeSet.Value
                    .Where(a => a != baseType && baseType.IsAssignableFrom(a))
                    .ToArray()
                    ;
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
#if DEBUG
            CheckBasetype(baseType);
#endif
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .Where(a => IsBaseType(a.type, baseType))
                .Select(a => a.type)
                .Distinct()
                .ToArray()
                ;


        }


        static void CheckAttribute(Type attributeType)
        {
            if (!typeof(DirectRetrieveAttribute).IsAssignableFrom(attributeType))
            {
                throw new($"type {attributeType} is not a DirectRetrieveAttribute");
            }
        }

        // [Conditional("DIRECT_RETRIEVE_ATTRIBUTE_STRICT")]
        static void CheckBasetype(Type baseType)
        {
            if (!IsTypeRetrievable(baseType))
            {
                throw new($"type {baseType} is not retrievable, which should inherit from IDirectRetrieve");
            }
        }

        public static bool IsTypeRetrievable(Type type)
        {
            return IsBaseType(type, typeof(IDirectRetrieve));
        }

        static bool IsBaseType(Type subType, Type baseType)
        {
            if (subType == baseType)
                return false;

            if (baseType.IsInterface)
                return baseType.IsAssignableFrom(subType);
            else
                return subType.IsSubclassOf(baseType);
        }

        static T SetAttributeValue<T>(T attr, MemberInfo targetInfo) where T : DirectRetrieveAttribute
        {
            attr.targetInfo = targetInfo;
            attr.OnReceiveTarget();
            return attr;
        }
    }
}