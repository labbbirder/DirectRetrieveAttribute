using System;
using System.Reflection;

namespace com.bbbirder
{
    public partial class DirectRetrieveAttribute : Attribute
    {
        /// <summary>
        /// the type marked with this attribute
        /// </summary>
        /// <value></value>
        public Type targetType { get; internal set; }
        /// <summary>
        /// the member marked with this attribute, if exists.
        /// </summary>
        /// <value></value>
        public MemberInfo targetMember { get; internal set; }
    }
}
