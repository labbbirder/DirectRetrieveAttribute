using System;
using System.Linq;
using System.Reflection;
using com.bbbirder.DirectAttribute;

namespace com.bbbirder.DirectAttribute{
    public static class AttributeRetriever{
        static BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly
            ;
        /// <summary>
        /// Retrieve Attribute T in all Assemblies
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAll<T>() where T:DirectRetrieveAttribute{
            var attrType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                //.Where(a=>a.GetReferencedAssemblies().Any(a=>a.ToString()== attrAssemblyName))
                .SelectMany(a => GetAll<T>(a))
                .ToArray();
        }
        /// <summary>
        /// Retrieve Attribute T on a specific Assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetAll<T>(Assembly assembly) where T:DirectRetrieveAttribute{
            var attrType = typeof(T);
            return assembly.GetCustomAttributes<GeneratedDirectRetrieveAttribute>()
                .SelectMany(a => {
                    var targetType = a.type;
                    if (a.HasMemberName) {
                        return targetType.GetMember(a.memberName, bindingFlags).SelectMany(
                            m => m.GetCustomAttributes<T>(false).Select(
                                ca => SetAttributeValue(ca, targetType, m)
                            )
                        );
                    } else {
                        return targetType.GetCustomAttributes<T>(false).Select(
                            ca => SetAttributeValue(ca, targetType, null)
                        );
                    }
                })
                .ToArray();
        }
        static T SetAttributeValue<T>(T attr,Type targetType,MemberInfo memberInfo) where T: DirectRetrieveAttribute {
            attr.targetType = targetType;
            attr.memberInfo = memberInfo;
            return attr;
        }
    }
}