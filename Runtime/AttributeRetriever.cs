using System;
using System.Linq;
using System.Reflection;
using com.bbbirder.DirectAttribute;

namespace com.bbbirder.DirectAttribute{
    public class AttributeRetriever{
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
                .Where(a=>a.HasMemberName)
                .SelectMany(a => a.type.GetMember(a.memberName,bindingFlags))
                .SelectMany(m=> m.GetCustomAttributes<T>(false))
                .ToArray();
        }
    }
}