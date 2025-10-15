using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using com.bbbirder;
using System.Reflection;

BenchmarkRunner.Run<BenchmarkProgram>();

[MemoryDiagnoser]
public class BenchmarkProgram
{
    readonly Type attrType = typeof(DirectRetrieveAttribute);
    readonly string attrAssemblyName = typeof(DirectRetrieveAttribute).Assembly.GetName().ToString();
    static BindingFlags bindingFlags = 0
        | BindingFlags.Instance
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly
        ;
    [Benchmark]
    public void GetMemberAttributesDefault()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes());
        types.SelectMany(t => t.GetCustomAttributes(attrType))
            .ToArray();
        types.SelectMany(a => a.GetMembers(bindingFlags))
            .SelectMany(m => m.GetCustomAttributes(attrType))
            .ToArray();
    }
    [Benchmark(Baseline = true)]
    public void GetMemberAttributesWithReferenceCheck()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().ToString() == attrAssemblyName
                || a.GetReferencedAssemblies().Any(a => a.ToString() == attrAssemblyName))
            .SelectMany(a => a.GetTypes());
        types.SelectMany(t => t.GetCustomAttributes(attrType))
            .ToArray();
        types.SelectMany(a => a.GetMembers(bindingFlags))
            .SelectMany(m => m.GetCustomAttributes(attrType))
            .ToArray();
    }

    [Benchmark]
    public void GetMemberAttributesDirect()
    {
        Retriever.GetAllAttributes<DirectRetrieveAttribute>();
    }
}