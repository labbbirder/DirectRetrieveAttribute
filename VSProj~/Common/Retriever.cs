using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.bbbirder;
using AttributeGroup = System.Collections.Generic.Dictionary<
    System.Type,
    com.bbbirder.DirectRetrieveAttribute[]
>;
//using TypeSet = System.Collections.Generic.HashSet<System.Type>;


namespace com.bbbirder
{

    public class WeakHolder<T> where T : class
    {
        WeakReference<T> wr;
        Func<T> creator;
        public T Value
        {
            get
            {
                if (!wr.TryGetTarget(out var target))
                {
                    wr.SetTarget(target = creator());
                }
                return target;
            }
        }
        public WeakHolder(Func<T> creator)
        {
            this.creator = creator;
            this.wr = new WeakReference<T>(creator());
        }

    }
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
        static WeakHolder<AttributeGroup> attrLut => m_attrLut ??= new(() => {
            var al = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute),false))
                .SelectMany(a => a.GetCustomAttributes<GeneratedDirectRetrieveAttribute>())
                .Select(a => a.type)
                .Distinct()
                .ToArray();
            foreach (var a in al) Console.WriteLine($"r: {a}");
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute)))
                .SelectMany(a => GetAllAttributes<DirectRetrieveAttribute>(a))
                .GroupBy(a => a.GetType()??typeof(object), a => a)
                .ToDictionary(e => e.Key, e => e.ToArray());
        });
        static WeakHolder<Type[]> m_typeSet;
        static WeakHolder<Type[]> typeSet => m_typeSet ??= new(() => {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDefined(typeof(GeneratedDirectRetrieveAttribute), false))
                .SelectMany(a => a.GetCustomAttributes<GeneratedDirectRetrieveAttribute>())
                .Select(a => a.type)
                .Distinct()
                .ToArray();
        });
        /// <summary>
        /// Retrieve Attribute T in all Assemblies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAllAttributes<T>() where T : DirectRetrieveAttribute
        {
            var attrType = typeof(T);
            return attrLut.Value.ToArray()
                .Where(a=> attrType.IsAssignableFrom(a.Key))
                .SelectMany(a=>a.Value)
                .OfType<T>()
                .ToArray()
                ;
        }
        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type[] GetAllSubtypes(Type baseType)
        {
            //foreach(var t in lut.Value.ToArray().SelectMany(a => a.Value)) {
            //    Console.WriteLine($"t: {t.targetType}");
            //}
            return typeSet.Value
                .Where(a=>a!=baseType && baseType.IsAssignableFrom(a))
                .ToArray()
                ;
        }
        /// <summary>
        /// Retrieve Attribute T on a specific Assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAllAttributes<T>(Assembly assembly) where T : DirectRetrieveAttribute
        {
            var attrType = typeof(T);
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .SelectMany(a => {
                    var targetType = a.type;
                    if (a.HasMemberName)
                    {
                        return targetType.GetMember(a.memberName, bindingFlags).SelectMany(
                            m => m.GetCustomAttributes<T>(false).Select(
                                ca => SetAttributeValue(ca, targetType, m)
                            )
                        );
                    }
                    else
                    {
                        return targetType.GetCustomAttributes<T>(false).Select(
                            ca => SetAttributeValue(ca, targetType, null)
                        );
                    }
                })
                .ToArray();
        }
        /// <summary>
        /// Retrieve all subclasses that inherit from the target type
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static Type[] GetAllSubtypes(Assembly assembly, Type baseType)
        {
#if DEBUG
            var attributesOnBase = baseType.GetCustomAttributes(true)
                .OfType<DirectRetrieveAttribute>()
                .ToArray();
            if (attributesOnBase.Length == 0)
            {
                throw new($"there is no DirectRetrieveAttribute on type {baseType}");
            }
            var hasInherit = attributesOnBase
                .Select(a => a.GetType().GetCustomAttribute<AttributeUsageAttribute>())
                .Any(usage => usage.Inherited);
            if (!hasInherit)
            {
                throw new($"DirectRetrieveAttribute on {baseType} should be Inherited");
            }
#endif
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .Where(a => a.type.IsSubclassOf(baseType))
                .Select(a => a.type)
                .Distinct()
                .ToArray()
                ;
        }
        ///// <summary>
        ///// Retrieve all classes that implement the target interface
        ///// </summary>
        ///// <param name="assembly"></param>
        ///// <param name="interfaceType"></param>
        ///// <returns></returns>
        //public static Type[] GetAllImplements(Assembly assembly,Type interfaceType){
        //    if (!interfaceType.IsInterface)
        //    {
        //        throw new($"type {interfaceType} is not interface");
        //    }
        //    return GetAllSubtypes(assembly, interfaceType);
        //}

        static T SetAttributeValue<T>(T attr, Type targetType, MemberInfo memberInfo) where T : DirectRetrieveAttribute
        {
            attr.targetType = targetType;
            attr.memberInfo = memberInfo;
            return attr;
        }
    }
}