﻿using System;
using System.Reflection;

#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace com.bbbirder
{
    /// <summary>
    /// Define on arbitrary type or member to retrieve the target directly at runtime.
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
    public partial class DirectRetrieveAttribute : Attribute
    {
        /// <summary>
        /// the member marked with this attribute, if exists.
        /// </summary>
        /// <value></value>
        public MemberInfo targetInfo { get; internal set; }

        /// <summary>
        /// on receive target type and member
        /// </summary>
        public virtual void OnReceiveTarget()
        {

        }
    }

    /// <summary>
    /// Define on a non-sealed class or interface to retrieve all subtypes and implements
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
    public partial class RetrieveSubtypeAttribute : Attribute { }
}
