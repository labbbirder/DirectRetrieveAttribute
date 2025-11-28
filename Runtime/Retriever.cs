using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.bbbirder.directattribute.Editor")]

namespace BBBirder.DirectAttribute
{
    public static class Retriever
    {
        const BindingFlags AllDeclaredFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly
            ;

        private struct MetadataStorage
        {
            public Dictionary<Type, HashSet<Type>> subTypes;
            public Dictionary<Type, HashSet<MemberInfo>> members;
        }

        static Dictionary<Assembly, MetadataStorage> assemblyStorages = new();
        static Dictionary<Type, HashSet<MemberInfo>> globalMarkedMembers = new();
        static Dictionary<Type, HashSet<Type>> globalSubTypes = new();
        static MetadataStorage globalStorage;

        static Retriever()
        {
            globalStorage = new()
            {
                members = globalMarkedMembers,
                subTypes = globalSubTypes,
            };

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
            foreach (var assembly in assemblies)
            {
                EnsureAssemblyStoragePopulated(assembly);
            }
        }

        private static TValue EnsureKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            if (!dict.TryGetValue(key, out var result))
            {
                dict[key] = result = new();
            }

            return result;
        }

        private static MetadataStorage EnsureAssemblyStoragePopulated(Assembly assembly)
        {
            if (!IsValidAssembly(assembly)) return default;

            if (!assemblyStorages.TryGetValue(assembly, out var storage))
            {
                var typeModule = assembly.GetType("BBBirder.RetrieveModule");
                if (typeModule != null)
                {
                    var targetMembers = typeModule.GetField("targetMembers", AllDeclaredFlags).GetValue(null) as Dictionary<Type, HashSet<MemberInfo>>;
                    var subTypes = typeModule.GetField("subTypes", AllDeclaredFlags).GetValue(null) as Dictionary<Type, HashSet<Type>>;
                    storage = new()
                    {
                        members = targetMembers,
                        subTypes = subTypes,
                    };

                    foreach (var (baseType, types) in subTypes)
                    {
                        globalSubTypes.EnsureKey(baseType).UnionWith(types);
                    }

                    foreach (var (attrType, members) in targetMembers)
                    {
                        globalMarkedMembers.EnsureKey(attrType).UnionWith(members);
                    }
                }

                assemblyStorages[assembly] = storage;
            }

            return storage;
        }

        [ThreadStatic] static Stack<Type> s_waveFront;
        [ThreadStatic] static HashSet<Type> s_visited;
        private static void GetSubtypes(Type baseType, MetadataStorage storage, HashSet<Type> results)
        {
            var visited = s_visited ??= new();
            visited.Clear();

            var waveFront = s_waveFront ??= new();
            waveFront.Clear();

            var baseDefType = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
            waveFront.Push(baseDefType);
            visited.Add(baseDefType);
            while (waveFront.TryPop(out var type))
            {
                if (globalSubTypes.TryGetValue(type, out var subTypes))
                {
                    foreach (var t in subTypes)
                    {
                        if (visited.Add(t))
                        {
                            waveFront.Push(t);
                        }
                    }


                    if (storage.subTypes.TryGetValue(type, out var types))
                    {
                        if (baseDefType.IsGenericType)
                        {
                            if (baseDefType.IsGenericTypeDefinition)
                            {
                                results.UnionWith(types);
                            }
                            else if (baseDefType.IsConstructedGenericType)
                            {
                                foreach (var t in types)
                                {
                                    if (baseDefType.IsAssignableFrom(t))
                                    {
                                        results.Add(t);
                                    }
                                }
                            }
                            else
                            {
                                throw new("Not support partial constructed generic type");
                            }
                        }
                        else
                        {
                            results.UnionWith(types);
                        }
                    }
                }
            }

            visited.Clear();
        }

        [ThreadStatic] static HashSet<Type> s_attrTypes;
        private static void GetAttributes(Type attrType, MetadataStorage storage, List<DirectRetrieveAttribute> results)
        {
            var attrTypes = s_attrTypes ??= new();
            attrTypes.Clear();
            GetSubtypes(attrType, globalStorage, attrTypes);
            attrTypes.Add(attrType);
            foreach (var type in attrTypes)
            {
                if (storage.members.TryGetValue(type, out var members))
                {
                    foreach (var member in members)
                    {
                        foreach (var attr in member.GetCustomAttributes(attrType))
                        {
                            if (attr is DirectRetrieveAttribute dattr)
                            {
                                dattr.TargetMember = member;
                                dattr.OnReceiveTarget();
                                results.Add(dattr);
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsValidAssembly(Assembly assembly)
        {
            return assembly.IsDefined(typeof(AssemblyMetadataAttribute))
                && assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(attr => attr.Key == "direct-attribute")
                ;
        }

        /// <summary>
        /// Retrieve attributes of type T in all loaded assemblies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetAllAttributes<T>() where T : DirectRetrieveAttribute
        {
            return GetAllAttributes(typeof(T)).OfType<T>();
        }

        /// <summary>
        /// Retrieve attributes of target type in all loaded assemblies
        /// </summary>
        /// <param name="attributeType"></param>
        /// <returns></returns>
        public static IEnumerable<DirectRetrieveAttribute> GetAllAttributes(Type attributeType)
        {
#if DEBUG
            CheckAttribute(attributeType);
#endif
            var results = new List<DirectRetrieveAttribute>();
            GetAttributes(attributeType, globalStorage, results);
            return results;
        }

        /// <summary>
        /// Retrieve attributes of target type in a specific assembly
        /// </summary>
        /// <param name="attributeType"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<DirectRetrieveAttribute> GetAllAttributes(Type attributeType, Assembly assembly)
        {
#if DEBUG
            CheckAttribute(attributeType);
#endif
            if (!IsValidAssembly(assembly))
                return Array.Empty<DirectRetrieveAttribute>();

            var storage = EnsureAssemblyStoragePopulated(assembly);

            var results = new List<DirectRetrieveAttribute>();
            GetAttributes(attributeType, storage, results);
            return results;
        }

        /// <summary>
        /// Retrieve attributes of type T in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetAllAttributes<T>(Assembly assembly) where T : DirectRetrieveAttribute
        {
            return GetAllAttributes(typeof(T), assembly).OfType<T>();
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static HashSet<Type> GetAllSubtypes(Type baseType)
        {
            var results = new HashSet<Type>();
            GetSubtypes(baseType, globalStorage, results);
            return results;
        }

        internal static HashSet<Type> GetAllSubtypesInCurrentAppDomain()
        {
            var results = new HashSet<Type>();
            foreach (var (baseType, types) in globalSubTypes)
            {
                GetSubtypes(baseType, globalStorage, results);
            }

            return results;
        }

        internal static IEnumerable<DirectRetrieveAttribute> GetAllAttributesInCurrentAppDomain()
        {
            return GetAllAttributes(typeof(DirectRetrieveAttribute));
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllSubtypes<T>()
        {
            return GetAllSubtypes(typeof(T));
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllSubtypes<T>(Assembly assembly)
        {
            return GetAllSubtypes(typeof(T), assembly);
        }

        /// <summary>
        /// Retrieve all subclasses that inherit from the target type in a specific assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllSubtypes(Type baseType, Assembly assembly)
        {
            if (!IsValidAssembly(assembly))
                return Array.Empty<Type>();

            var storage = EnsureAssemblyStoragePopulated(assembly);
            var results = new HashSet<Type>();
            GetSubtypes(baseType, storage, results);
            return results;
        }

        private static void CheckAttribute(Type attributeType)
        {
            if (!typeof(DirectRetrieveAttribute).IsAssignableFrom(attributeType))
            {
                throw new($"type {attributeType} is not a DirectRetrieveAttribute");
            }
        }
    }
}
