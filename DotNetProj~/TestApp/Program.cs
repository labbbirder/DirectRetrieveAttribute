using System.Reflection;
using BBBirder.DirectAttribute;
using com.bbbirder;
using ModuleB;
using NS1;

namespace NS1
{
    namespace NS2
    {
        public class ImplementB<T> : IFeatureB
        {
            [MarkB]
            public void Test()
            {

            }
            [MarkB]
            public void Test<K>()
            {
            }
            private class Foo1 : IFeatureB
            {
                [MarkB] private string Name { get; set; }
            }
            private class Foo2: IFeatureB
            {
                [MarkB] private string Name { get; set; }
            }
        }
    }

    internal enum FooEnum
    {
        Foo,
        [MarkB] Bar,
    }

    public class Provider : IProvider<string> { }

    [RetrieveSubtype]
    internal interface IProvider<T> { }
}

[DirectRetrieve]
internal partial class Program
{
    public static void Main()
    {
        var types = Retriever.GetAllSubtypes(typeof(IProvider<>));
        foreach (var type in types)
        {
            Console.WriteLine($"{type}");
        }
        Console.WriteLine($"\n\n");

        // var attrs = Retriever.GetAllAttributes<MarkB>();
        // foreach (var attr in attrs)
        // {
        //     Console.WriteLine($"{attr} {attr.targetInfo}");
        // }
    }
}
