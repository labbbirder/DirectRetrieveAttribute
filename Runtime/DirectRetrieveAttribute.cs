using System;
using System.Reflection;

namespace com.bbbirder.DirectAttribute
{
    public class DirectRetrieveAttribute : Attribute
    {
        public Type targetType {get; internal set;}
        public MemberInfo memberInfo {get; internal set;}
    }
}
