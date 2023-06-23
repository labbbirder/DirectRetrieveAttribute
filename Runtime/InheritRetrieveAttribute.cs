using System;
using System.Collections.Generic;
using System.Text;

namespace com.bbbirder {
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface,Inherited =true)]
    public sealed partial class InheritRetrieveAttribute : DirectRetrieveAttribute{
        
    }
}
