using System;
using System.Reflection;
using System.Runtime.InteropServices;


#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace BBBirder.DirectAttribute
{
    /// <summary>
    /// Define on arbitrary type or member to retrieve the target directly at runtime.
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
    [RetrieveSubtype]
    public partial class DirectRetrieveAttribute : Attribute
    {
        /// <summary>
        /// the member marked with this attribute, if exists.
        /// </summary>
        /// <value></value>
        public MemberInfo TargetMember { get; set; }

        /// <summary>
        /// Whether the target should be automatically preserved in build.
        /// </summary>
        public virtual bool PreserveTarget => false;

        /// <summary>
        /// on receive target type and member
        /// </summary>
        public virtual void OnReceiveTarget()
        {

        }
    }
}
