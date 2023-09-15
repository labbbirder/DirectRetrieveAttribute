using System;
using System.Reflection;

#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace com.bbbirder
{
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
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

        /// <summary>
        /// on receive target type and member
        /// </summary>
        public virtual void OnReceiveTarget(){

        }
    }
}
