# DirectRetrieveAttribute
快速获取用户自定义Attribute；支持通过Attribute获取目标Type和目标MemberInfo。

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

### DirectAttribute获取Attributes列表
借助DirectAttribute，我们可以在运行时快速检索所有Attributes。

这是一个完整示例：
```csharp
using com.bbbirder.DirectAttribute;

//自定义Attribute需要继承DirectRetrieveAttribute
class FooAttribute:DirectRetrieveAttribute {
    public string title { get; private set; }
    public FooAttribute(string title){
        this.title = title;
    }
}

[Foo("whoami")]
class Player{
    [Foo("Hello")]
    void Salute(){

    }
}

//检索当前Domain下所有Assembly中所有FooAttribute
FooAttribute[] attributes = AttributeRetriever.GetAll<FooAttribute>(); 
foreach(var attr in attributes){
    print($"{attr.targetType} {attr.memberInfo?.Name} {attr.title}"); 
}
// output: 
//    Player null whoami
//    Player Salute Hello
```
> 继承自`DirectRetrieveAttribute`的自定义Attribute可以通过`targetType`访问目标类型，通过`memberInfo`访问目标成员（可能为空）。但必须是通过`AttributeRetriever.GetAll<T>`返回的Attribute，`AttributeRetriever.GetAll<T>`会在检索过程中填充这两个property。

### 基准测试结果
![benchmark](Documentation/benchmark.jpg)
* `GetMemberAttributesDefault`使用传统方式检索所有Attribute，
* `GetMemberAttributesWithReferenceCheck`使用传统方式检索，但是先检查Assembly之间的依赖关系。
* `GetMemberAttributesDirect`使用`DirectAttribute`提供的方式检索所有Attribute。

在VisualStudio和Unity测试总结：`DirectAttribute`方式在运行时间上，比传统方式优化了95%左右；在内存消耗上比传统方式优化了99%以上。并且随着用户代码的体积增长，`DirectAttribute`方式的开销几乎不变。

[基准测试源码](Documentation/benchmark.md)
## 安装
Package Manager通过git url安装： https://github.com/labbbirder/DirectRetrieveAttribute


## 实现原理
首先使用源生成方式写入assembly Attribute列表（名为`GeneratedDirectRetrieveAttribute`），并提供RoslynAnalyzer保证代码准确性。在运行时直接从Assembly中读取`GeneratedDirectRetrieveAttribute`。

## Todo List
* **支持 Inherit 参数**
* 还可以继续优化，但是收益不大；欢迎PR。
    * `GeneratedDirectRetrieveAttribute` 中增加目标Attribute字段
    * `#NET7_0_OR_GREATER` 宏判断和成员排序
    * 增加 `GeneratedDirectRetrieveAttribute` 数组的起始元信息，实现遍历早停。
    * 增量式检索（多帧异步）
* Auto CI
