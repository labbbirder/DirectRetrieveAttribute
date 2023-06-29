using com.Another.ad.zxc;
using com.bbbirder;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using INT = System.Int16;
[DirectRetrieve]
partial class Program
{
    static BindingFlags bindingFlags = 0
    | BindingFlags.Instance
    | BindingFlags.Public
    | BindingFlags.NonPublic
    | BindingFlags.Static
    | BindingFlags.DeclaredOnly
    ;
    [Inject("good")]
    public static void Main()
    {

        var attrs = Retriever.GetAllAttributes<InjectAttribute>();
        foreach(var attr in attrs)
        {
            Console.WriteLine($"{attr.targetType} {attr.memberInfo} {attr.name}");
        }
        Console.WriteLine(attrs.Length);
        Console.WriteLine(typeof(Program).Assembly.GetTypes()
            .SelectMany(t=>t.GetMembers(bindingFlags)).Count());
        Console.WriteLine(AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a=>a.GetTypes())
            .SelectMany(t => t.GetMembers(bindingFlags)).Count());
        var types = Retriever.GetAllSubtypes(typeof(IPlayer));
        foreach(var t in types)
        {
            Console.WriteLine($"subtype: {t}");
        }
        Console.WriteLine(typeof(IPlayer).IsDefined(typeof(DirectRetrieveAttribute),false));
        Console.WriteLine(typeof(labbb));

        var genAttrs = typeof(Dog<,>).GetCustomAttributes();
        foreach(var attr in genAttrs) {
            Console.WriteLine($"ga: {attr}");
        }
    }
    internal class Foo
    {
        //[DirectRetrieve(typeof(Program))]
        private void Bar()
        {


        }
    }
}
namespace com.Another{
    namespace ad.zxc {
        //[Inject("whoami")]
        internal class Player<G, TF> {
            [Inject("hello")]
            int age3;
            void Salute() {

            }
            [Foo]
            public class Inner {
                [Foo,Foo()]
                public class Meta<T,asd> {
                    [Foo]
                    void Aka() {

                    }
                }
            }
        }
        [Plain]
        class Dog<T, G> { }
        [Foo]
        interface IPlayer {

        }
        class Leo2 : IPlayer {

            [StructLayout(LayoutKind.Explicit, Size = sizeof(int))]
            private struct FloatIntBits
            {
                [FieldOffset(0)]
                public float f;
                [FieldOffset(0)]
                public int i;
            }
        }

    }
}
//[Foo]
class labbb:Leo2   {
    //[Foo] int n;
}
class PlainAttribute : Attribute { }
class FooAttribute : InjectAttribute
{
    public FooAttribute(string name="") : base(name) { }
}
[AttributeUsage(AttributeTargets.All, AllowMultiple = true,Inherited = true)]
class InjectAttribute : DirectRetrieveAttribute
{
    public string name;
    public InjectAttribute(string name)
    {
        this.name = name;
    }
}
