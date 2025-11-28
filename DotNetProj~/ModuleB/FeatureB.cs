using System.Reflection;
using BBBirder.DirectAttribute;
using com.bbbirder;
using ModuleA;

namespace ModuleB
{
    public interface IFeatureB : IFeatureA
    {

    }
    public class MarkB :DirectRetrieveAttribute
    {
        public MemberInfo TargetMember { get; set; }
        public bool PreserveTarget { get; }
        public void OnReceiveTarget()
        {
            throw new NotImplementedException();
        }
    }
}
