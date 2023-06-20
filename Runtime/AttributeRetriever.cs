using System;
using System.Linq;
using System.Reflection;
using com.bbbirder.DirectAttribute;

namespace com.bbbirder.DirectAttribute{
    static class AttributeRetriever{
        static BindingFlags bindingFlags = 0
            | BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            ;
        public static T[] GetAll<T>() where T:DirectRetrieveAttribute{
            var attrType = typeof(T);
            return AppDomain.CurrentDomain.GetAssemblies()
                //.Where(a=>a.GetReferencedAssemblies().Any(a=>a.ToString()== attrAssemblyName))
                .SelectMany(a => a.GetCustomAttributes<GeneratedDirectRetrieveAttribute>())
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