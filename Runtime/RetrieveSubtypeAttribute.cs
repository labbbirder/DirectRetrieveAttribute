using System;

#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace BBBirder.DirectAttribute
{
    /// <summary>
    /// Define on a non-sealed class or interface to retrieve all subtypes and implements
    /// </summary>
#if UNITY_5_3_OR_NEWER
    [Preserve]
#endif
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true)]
    public sealed class RetrieveSubtypeAttribute : Attribute
    {
        public readonly bool PreserveSubtypes = false;

        public RetrieveSubtypeAttribute() : this(preserveSubtypes: false) { }

        public RetrieveSubtypeAttribute(bool preserveSubtypes)
        {
            this.PreserveSubtypes = preserveSubtypes;
        }
    }
}
