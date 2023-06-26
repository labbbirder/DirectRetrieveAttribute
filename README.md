# DirectRetrieveAttribute
![GitHub last commit](https://img.shields.io/github/last-commit/labbbirder/directretrieveattribute)
![GitHub package.json version](https://img.shields.io/github/package-json/v/labbbirder/directretrieveattribute)
[![openupm](https://img.shields.io/npm/v/com.bbbirder.directattribute?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.bbbirder.directattribute/)

快速获取用户自定义Attribute；支持通过Attribute获取目标Type和目标MemberInfo。

## 为什么有用
使用额外的全局元数据而不是反射遍历来实现Attribute获取。

可以满足以下需求：
* 希望在运行时获取所有指定类型的Attribute，并且极低开销
* 希望在运行时获取所有指定类型的子类，并且极低开销 (baseType & interface)
* 通过Attribute实例直接获取标记的类或标记的成员


## 快速开始

### 安装
### via Git URL
Package Manager通过git url安装： https://github.com/labbbirder/DirectRetrieveAttribute
### via OpenUPM
```bash
openupm add com.bbbirder.directattribute
```
### 检索Attribute

```csharp
using com.bbbirder;

//自定义Attribute继承DirectRetrieveAttribute
class FooAttribute:DirectRetrieveAttribute {
    public string title { get; private set; }
    public FooAttribute(string title){
        this.title = title;
    }
}

//作出标记
[Foo("whoami")]
class Player{
    [Foo("Hello")]
    void Salute(){

    }
}

//检索当前Domain下所有FooAttribute
FooAttribute[] attributes = Retriever.GetAllAttributes<FooAttribute>(); 
foreach(var attr in attributes){
    print($"{attr.targetType} {attr.memberInfo?.Name} {attr.title}"); 
}
// output: 
//    Player null whoami
//    Player Salute Hello
```

> 继承自`DirectRetrieveAttribute`的自定义Attribute可以通过`targetType`访问目标类型，通过`memberInfo`访问目标成员（可能为空）。但必须是通过`Retriever.GetAllAttributes<T>`返回的Attribute，`Retriever.GetAllAttributes<T>`会在检索过程中填充这两个property。



### 检索子类型

```csharp
using com.bbbirder;

// 定义几个类型
[InheritRetrieve]
public class Battler{

}

public class Hero:Battler{

}

public class Titant:Hero{

}

// 获取Domain下所有Battler子类
Type[] types = Retriever.GetAllSubtypes(typeof(Battler));
foreach(var type in types){
    print($"type.Name");
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
> 众所周知，反射方法效率低，并且会产生大量GC。如果你是一个Package Developer，在你开发的众多Package中可能有不少需要检索Attribute列表的情况，这无疑是灾难性的。（参考[基准测试结果](#基准测试结果)）

### 基准测试结果
![benchmark](Documentation/benchmark.jpg)
* `GetMemberAttributesDefault`使用传统方式检索所有Attribute，
* `GetMemberAttributesWithReferenceCheck`使用传统方式检索，但是先检查Assembly之间的依赖关系。
* `GetMemberAttributesDirect`使用Direct方式检索所有Attribute。

可以得出结论如下：

||运行时间| 内存消耗|每用户代码体积增长|
|--|--|--|--|
|传统方式|100%|100%|开销线性增加|
|Direct|<5%|<1%|开销几乎不变|

[基准测试源码](Documentation/benchmark.md)

值得一提的是，以上的结果还只是基于理论的测试，在实际应用中，DirectRetrieveAttribute因考虑到检索通常集中地发生在开始运行阶段，因此在内部做了一点小小的优化：使用WeakReference缓冲了一下中间计算。结果是，Direct方式开销小到无法察觉（见下图）！
![benchmark](Documentation/benchmark-real.jpg)

实际差异如下：

||运行时间| 内存消耗|每用户代码体积增长|
|--|--|--|--|
|传统方式|100%|100%|开销线性增加|
|Direct|<0.1%|<0.1%|开销几乎不变|


## 实现原理
首先使用源生成方式写入assembly Attribute列表（名为`GeneratedDirectRetrieveAttribute`），并提供RoslynAnalyzer保证代码准确性。在运行时直接从Assembly中读取`GeneratedDirectRetrieveAttribute`。

对于Inherit的Attribute，会额外记录他们的子类。

## Todo List
* ~~**支持 Inherit 参数**~~
* 还可以继续优化，但是收益不大；欢迎PR。
    * `GeneratedDirectRetrieveAttribute` 中增加目标Attribute字段
    * `#NET7_0_OR_GREATER` 宏判断和成员排序
    * 增加 `GeneratedDirectRetrieveAttribute` 数组的起始元信息，实现遍历早停。
    * 增量式检索（多帧异步）
* ~~Auto CI~~
