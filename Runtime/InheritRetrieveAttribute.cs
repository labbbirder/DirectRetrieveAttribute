using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;

namespace com.bbbirder {
    [Preserve]
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface,Inherited =true)]
    public sealed partial class InheritRetrieveAttribute : DirectRetrieveAttribute{
        
    }
}
