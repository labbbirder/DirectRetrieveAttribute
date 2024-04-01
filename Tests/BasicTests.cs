using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using com.bbbirder;
using bt_it;

public class BasicTests
{
    static void AssertCount<T>(int count) where T : DirectRetrieveAttribute
    {
        var al = Retriever.GetAllAttributes<T>(typeof(BasicTests).Assembly);
        Assert.AreEqual(count, al.Length);
    }
    static void AssertSubTypeCount<T>(int count)
    {
        var sl = Retriever.GetAllSubtypes(typeof(T), typeof(BasicTests).Assembly);
        Assert.AreEqual(count, sl.Length);
    }
    // A Test behaves as an ordinary method
    [Test]
    public void TestGenerics()
    {
        AssertCount<TGAttribute>(4);
    }
    [Test]
    public void TestNest()
    {
        AssertCount<INNAttribute>(3);
    }
    [Test]
    public void TestBaseType()
    {
        AssertSubTypeCount<MyBaseType>(5);
    }
    [Test]
    public void TestInterface()
    {
        AssertSubTypeCount<IMyInterface>(4);
    }
    [Test]
    public void TestEnum()
    {
        AssertCount<EnumAttribute>(2);
    }
}
class EnumAttribute : DirectRetrieveAttribute { }
class TGAttribute : DirectRetrieveAttribute { }
class INNAttribute : DirectRetrieveAttribute { }
namespace innerA.innerB
{
    public class TestClassA<T>
    {
        [TG]
        public void Bar(T t)
        {

        }
        [TG]
        public void Foo<G>(G g)
        {

        }
        [TG]
        public void Foo2<G, H>(G g)
        {

        }
    }
    [TG]
    internal class TestGenericB<T, F, G>
    {
        [INN]
        internal class Inner
        {
            [INN]
            static void Bar()
            {

            }
            [INN]
            internal class Far
            {

            }
        }
    }
}
namespace bt_it
{
    enum FooEnum
    {
        Default,
        Foo,
        [Enum]
        Bar,
        [Enum]
        Baz,
        Length,
    }
    class MyBaseType : IDirectRetrieve
    {

    }
    class SubA<T> : MyBaseType
    {
        internal class SubA_A<G, F> : MyBaseType { }
    }
    class SubB : SubA<int> { }
    class SubC<T> : SubA<T> { }
    class SubD : SubB { }


    interface IMyInterface : IDirectRetrieve
    {

    }
    class ImpA<T> : IMyInterface
    {
        internal class ImpA_A<G, F> : IMyInterface { }
    }
    class ImpB : ImpA<int> { }
    struct ImpS : IMyInterface { }
}