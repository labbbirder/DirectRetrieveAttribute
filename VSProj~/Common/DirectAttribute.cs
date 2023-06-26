using System;
using System.Reflection;

namespace com.bbbirder
{
    public partial class DirectRetrieveAttribute : Attribute
    {
        public Type targetType { get; internal set; }
        public MemberInfo memberInfo { get; internal set; }
    }
}
