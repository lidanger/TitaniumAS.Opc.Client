# TitaniumAS.Opc.Client
OPC DA 开源 .NET 客户端库，为你提供对 OPC DA 互操作性的 .NET COM 封装。

## 特性
- 支持本地和网络上的 OPC DA 服务器。
- 支持 OPC DA 1.0, 2.05A, 3.0。
- 支持浏览 OPC DA 服务器。
- 支持 Async/await 读写操作。
- 支持通过 .NET 事件订阅数据变化。
- 支持服务器关闭事件。
- 支持简单的资源管理。

## 安装
在 NuGet 包管理器控制台中运行以下命令：
```
PM> Install-Package TitaniumAS.Opc.Client
```
参考 [NuGet 包](https://www.nuget.org/packages/TitaniumAS.Opc.Client).

## 基本用法
下面的示例覆盖了此库的基本用法。假定你的应用程序安装了此库的 NuGet 包。

#### 库初始化
在你的应用程序开始的时候调用 `Bootstrap.Initialize()`。由于库初始过程中的 [CoInitializeSecurity](http://www.pinvoke.net/default.aspx/ole32/CoInitializeSecurity.html) 调用，应用程序进程应该以 MTA 多线程单元状态启动。参考 [explanation](http://www.pinvoke.net/default.aspx/ole32/CoInitializeSecurity.html).

#### 连接到一个 OPC DA 服务器
首先，你应该创建一个 OPC DA 服务器实例，然后连接。
```csharp
// 使用构建器生成一个 OPC DA 服务器 URL。
Uri url = UrlBuilder.Build("Matrikon.OPC.Simulation.1");
using (var server = new OpcDaServer(url))
{
    // 首先连接到服务器。
    server.Connect();
    ...
}
```

#### 浏览元素
你可以使用 `OpcDaBrowserAuto` 浏览任意版本 OPC DA 服务器的所有元素。
```csharp
// 创建一个浏览器并递归浏览所有元素。
var browser = new OpcDaBrowserAuto(server);
BrowseChildren(browser);
...

void BrowseChildren(IOpcDaBrowser browser, string itemId = null, int indent = 0)
{
    // 如果 itemId 为 null，将会浏览根元素。
    OpcDaBrowseElement[] elements = browser.GetElements(itemId);

    // 输出元素。
    foreach (OpcDaBrowseElement element in elements)
    {
        // 输出元素。
        Console.Write(new String(' ', indent));
        Console.WriteLine(element);

        // 跳过没有子节点的元素。
        if (!element.HasChildren)
            continue;

        // 输出元素的子节点。
        BrowseChildren(browser, element.ItemId, indent + 2);
    }
}
```

#### 创建一个包含项的组
你可以添加一个包含项的组到 OPC DA 服务器。 
```csharp
// 创建一个包含项的组。
OpcDaGroup group = server.AddGroup("MyGroup");
group.IsActive = true;

var definition1 = new OpcDaItemDefinition
{
    ItemId = "Random.Int2",
    IsActive = true
};
var definition2 = new OpcDaItemDefinition
{
    ItemId = "Bucket Brigade.Int4",
    IsActive = true
};
OpcDaItemDefinition[] definitions = { definition1, definition2 };
OpcDaItemResult[] results = group.AddItems(definitions);

// 处理添加结果。
foreach (OpcDaItemResult result in results)
{
    if (result.Error.Failed)
        Console.WriteLine("Error adding items: {0}", result.Error);
}
...
```

#### 读取值
组中的项既可以同步读取也可以异步读取。
```csharp
// 同步读取组中的所有项。
OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
...

// 异步读取组中的所有项。
OpcDaItemValue[] values = await group.ReadAsync(group.Items);
...
```

#### 写入值
同时，组中的既可以同步写入也可以异步写入。
```csharp
// 准备项。
OpcDaItem int2 = group.Items.FirstOrDefault(i => i.ItemId == "Bucket Brigade.Int2");
OpcDaItem int4 = group.Items.FirstOrDefault(i => i.ItemId == "Bucket Brigade.Int4");
OpcDaItem[] items = { int2, int4 };

// 同步写入值到项。
object[] values = { 1, 2 };
HRESULT[] results = group.Write(items, values);
...

// 异步写入值到项。
object[] values = { 3, 4 };
HRESULT[] results = await group.WriteAsync(items, values);
...
```

#### 通过订阅获取值
组可以配置为，当值变化时提供一个新值的客户端。
```csharp
// 订阅配置。
group.ValuesChanged += OnGroupValuesChanged;
group.UpdateRate = TimeSpan.FromMilliseconds(100); // 如果为0，则不会触发 ValuesChanged
...

static void OnGroupValuesChanged(object sender, OpcDaItemValuesChangedEventArgs args)
{
    // 输出值。
    foreach (OpcDaItemValue value in args.Values)
    {
        Console.WriteLine("ItemId: {0}; Value: {1}; Quality: {2}; Timestamp: {3}",
            value.Item.ItemId, value.Value, value.Quality, value.Timestamp);
    }
}
```

## 错误处理
* 首先检查系统是否安装 Opc Core Components (https://opcfoundation.org/developer-tools/developer-kits-classic/core-components)。很有可能你没有安装 OPCEnum 服务。
* 要在 NUnit 中运行单元测试，应该配置使用 x86 环境。
* 在 Visual Studio 中，设置你的项目使用 "Prefer 32-bit"：项目属性 → 构建 → 平台目标 "Prefer 32-bit"，这样代码会编译为 32 位。

## API 文档
即将上线...

## 许可证
The MIT License (MIT) – [LICENSE](https://github.com/titanium-as/TitaniumAS.Opc.Client/blob/master/LICENSE).
