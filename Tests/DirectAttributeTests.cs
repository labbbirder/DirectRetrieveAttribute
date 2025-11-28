using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BBBirder.DirectAttribute;
using NameA;
using NUnit.Framework;
using UnityEngine;

public class DirectAttributeTests
{
    static void AssertCount<T>(int count) where T : DirectRetrieveAttribute
    {
        var al = Retriever.GetAllAttributes<T>(typeof(DirectAttributeTests).Assembly).ToArray();
        if (al.Length != count)
        {
            Debug.Log("only found:");
            foreach (var a in al)
            {
                Debug.Log(a.TargetMember);
            }
        }

        Assert.AreEqual(count, al.Length);
    }

    static void AssertSubTypeCount<T>(int count)
    {
        var sl = Retriever.GetAllSubtypes(typeof(T), typeof(DirectAttributeTests).Assembly).ToArray();
        if (sl.Length != count)
        {
            Debug.Log("only found:");
            foreach (var s in sl)
            {
                Debug.Log(s);
            }
        }

        Assert.AreEqual(count, sl.Length);
    }

    [Test]
    public void AttributeMarked_ShouldBeCollected()
    {
        AssertCount<TestGenericAttribute>(4);
    }

    [Test]
    public void Nested_ShouldBeCollected()
    {
        AssertCount<TestNestedAttribute>(8);
    }

    [Test]
    public void HasARetrievableBaseType_ShouldBeCollected()
    {
        AssertSubTypeCount<MyBaseType>(5);
    }

    [Test]
    public void HasARetrievableInterface_ShouldBeCollected()
    {
        AssertSubTypeCount<IMyInterface>(4);
    }

    [Test]
    public void AttributeOnEnumField_ShouldBeCollected()
    {
        AssertCount<TestEnumAttribute>(2);
    }

    [Test]
    public void GenericInstanceSubtype_ShouldBeCollected()
    {
        AssertSubTypeCount<GenericImplementA<int>>(1);
    }
}


class TestEnumAttribute : DirectRetrieveAttribute
{
    public override bool PreserveTarget => true;
}

class TestGenericAttribute : DirectRetrieveAttribute
{
    public override bool PreserveTarget => true;
}

class TestNestedAttribute : DirectRetrieveAttribute
{
    public override bool PreserveTarget => true;
}

namespace NameA.SubnameB
{
    class GenericType<T>
    {
        [TestGeneric]
        void PlainMethodInGenericType(T t)
        {

        }

        [TestGeneric]
        void GenericMethodInGenericType<T0>(T0 g)
        {

        }

        [TestGeneric]
        void GenericMethodInGenericType2<T0, T1>(T1 g)
        {
        }
    }

    [TestGeneric]
    class GenericType2<T0, T1, T2>
    {
        [TestNested]
        static void StaticMethodInGenericType()
        {

        }

        [TestNested]
        class NestType
        {
            [TestNested]
            static void Bar()
            {

            }

            [TestNested]
            class NestType2
            {

            }
        }
    }

    internal class TestNestDeclaringType
    {
        [TestNested]
        static void StaticMethodInPlaingType()
        {

        }

        [TestNested]
        class Inner
        {
            [TestNested]
            static void Bar()
            {

            }

            [TestNested]
            class Far
            {

            }
        }
    }
}


namespace NameA
{
    enum FooEnum
    {
        Default,
        Foo,
        [TestEnum]
        Bar,
        [TestEnum]
        Baz,
        Length,
    }

    [RetrieveSubtype]
    class MyBaseType
    {

    }

    class GenericBaseType<T> : MyBaseType
    {
        class MySubTypeA<T0, T1> : MyBaseType { }
    }

    class PlainSubTypeB : GenericBaseType<int> { }

    class GenericSubTypeC<T> : GenericBaseType<T> { }

    class SubTypeD : PlainSubTypeB { }

    [RetrieveSubtype]
    interface IMyInterface { }

    class GenericImplementA<T> : IMyInterface
    {
        class NestedGenericImplement<T0, T1> : IMyInterface { }
    }

    class PlainImplementB : GenericImplementA<int> { }

    struct PlainImplementValueType : IMyInterface { }
}
