using System;
using System.Collections.Generic;
using System.Text;

namespace com.bbbirder.DirectAttribute
{
    [AttributeUsage(AttributeTargets.Assembly,AllowMultiple = true)]
    public class GeneratedDirectRetrieveAttribute:Attribute
    {
        public Type type { get; private set; }
        public string memberName { get; private set; }
        public bool HasMemberName { get;private set; }
        public GeneratedDirectRetrieveAttribute(Type type, string memberName = null)
        {
            this.type = type;
            this.memberName = memberName;
            HasMemberName = memberName != null;
        }

    }
}
