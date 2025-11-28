# DirectRetrieveAttribute

![GitHub last commit](http://img.shields.io/github/last-commit/labbbirder/directretrieveattribute)
![GitHub package.json version](http://img.shields.io/github/package-json/v/labbbirder/directretrieveattribute)
[![openupm](http://img.shields.io/npm/v/com.bbbirder.directattribute?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.bbbirder.directattribute/)

## 为什么有用

使用额外的全局数据而不是反射遍历来实现Attribute查找和子类查找。（允许自动preserve目标）

可以满足以下三大常见需求：

* **高效**获取Attribute
* **高效**获取子类（提供basetype）和实现类（提供interface）
* 通过Attribute实例**直接获取**标记的类或成员

## 快速开始

### 安装

#### via Git URL

Package Manager通过git url安装： https://github.com/labbbirder/DirectRetrieveAttribute.git

#### via OpenUPM

```bash
openupm add com.bbbirder.directattribute
```

### 检索Attribute

```csharp
using BBBirder.DirectAttribute;

//自定义Attribute继承DirectRetrieveAttribute
class FooAttribute : DirectRetrieveAttribute 
{
    // 自动Preserve标记的成员，防止strip
    public override bool PreserveTarget => true;

    public string title { get; private set; }
    
    public FooAttribute(string title)
    {
        this.title = title;
        // dont access TargetMember here, it will always be null
    }
    
    public override void OnReceiveTarget()
    {
        // TargetMember is available here ^_^
        Debug.Log(TargetMember);
    }
}

//作出标记
[Foo("whoami")]
class Player
{
    [Foo("Hello")]
    void Salute()
    {

    }
}

//检索当前Domain下所有FooAttribute
FooAttribute[] attributes = Retriever.GetAllAttributes<FooAttribute>(); 
foreach(var attr in attributes)
{
    print($"{attr.TargetMember.Name} {attr.title}"); 
}
// output: 
//    Player whoami
//    Salute Hello
```

> 自定义Attribute需要继承`DirectRetrieveAttribute`。`Retriever.GetAllAttributes`会在检索过程中赋值目标成员`TargetMember`并调用`OnReceiveTarget()`通知赋值完成。

### 检索子类型

```csharp
using BBBirder.DirectAttribute;

// 定义几个类型
public class Battler:IDirectRetrieve
{

}

public class Hero:Battler
{

}

public class Titant:Hero
{

}

// 获取Domain下所有Battler子类
Type[] types = Retriever.GetAllSubtypes<Battler>();
foreach(var type in types)
{
    print($"{type.Name}");
}
// output:
//     Hero
//     Titant
```

接口的实现检索与上例类似。

## 传统方式对比

### 传统方式获取Attributes列表

在传统方式下，我们可能会这样获取所有自定义属性：

```csharp
AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a=>a.GetTypes())
    .SelectMany(a=>a.GetMembers()
        .SelectMany(m=>m.GetCustomAttributes(attrType)))
    .ToArray();
```

> 众所周知，反射方法效率低，并且会产生大量GC。如果你是一个Package Developer，在你开发的众多Package中可能有不少需要检索Attribute列表的情况，这无疑是灾难性的。

## 实现原理

开发者编辑代码时使用源生成方式写入额外信息，并提供RoslynAnalyzer保证代码准确性。

> 现在，只有真正编译发生时，才分析语法，并生成这些信息。

使用反编译工具打开Unity自动生成的Dll，可以看到类似下面的额外元数据：

```csharp

namespace BBBirder
{
    internal static class RetrieveModule
    {
        static Dictionary<Type, HashSet<Type>> subTypes;

        static Dictionary<Type, HashSet<MemberInfo>> targetMembers;

        static Type s_attrType;

        static RetrieveModule()
        {
            s_attrType = Type.GetType("BBBirder.DirectAttribute.DirectRetrieveAttribute, BBBirder.DirectAttribute");

            // 涉及到的上层模块类型，需要通过AQN获取
            var @type0 = Type.GetType("MyType1, MyModule");

            // 当前模块的Attribute字典
            targetMembers = new()
            {
                [typeof(MyAttribute)] = new HashSet<MemberInfo>()
                    .Collect(typeof(MyType2), "MyMethod2")
                    .Collect(@type0, "MyMethod")
                    ,
            };

            // 当前模块的类型继承树
            subTypes = new()
            {
                [@type0] = new () {
                    typeof(MyType2),
                    typeof(MyType3),
                    typeof(MyType4),
                },
            };
        }

    }
}

```

## 为什么不实现写入MetadataToken到Assembly Resource？

考虑到有的项目可能需要DLL织入，MetadataToken会发生变化。
