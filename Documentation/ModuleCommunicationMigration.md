# 模块通信迁移指南

本文档指导如何将模块间的直接调用迁移到事件驱动的单播和请求-响应模式。

## 迁移的好处

1. **更好的解耦**：模块间无直接依赖
2. **更高的灵活性**：可以动态替换模块实现
3. **更好的可测试性**：可以模拟其他模块的行为
4. **性能优化**：使用优化的事件总线实现

## 迁移步骤

### 1. 识别直接调用

查找代码中的直接模块调用：

```csharp
// 迁移前 - 直接调用
var playerModule = FrameworkManager.GetModule<PlayerModule>();
var playerData = playerModule.GetPlayerData();

var configModule = FrameworkManager.GetModule<ConfigModule>();  
configModule.SaveGame();
```

### 2. 定义通信协议

为每个交互创建命令或查询：

```csharp
// 定义查询协议
public struct GetPlayerDataQuery : IModuleQuery<PlayerData>
{
    public bool IncludeInventory;
}

// 定义命令协议  
public struct SaveGameCommand : IModuleCommand
{
    public string SaveSlotName;
}
```

### 3. 实现处理器

在目标模块中注册处理器：

```csharp
// 在PlayerModule中
protected override void OnStart()
{
    ModuleCommunicator.RegisterQueryHandler<GetPlayerDataQuery, PlayerData>(HandleGetPlayerData);
    ModuleCommunicator.RegisterCommandHandler<SaveGameCommand>(HandleSaveGame);
}

private PlayerData HandleGetPlayerData(GetPlayerDataQuery query)
{
    // 返回玩家数据
    return _playerData;
}

private void HandleSaveGame(SaveGameCommand command)
{
    // 处理保存逻辑
    SaveToSlot(command.SaveSlotName);
}
```

### 4. 替换调用代码

将直接调用改为事件通信：

```csharp
// 迁移后 - 使用事件通信
var query = new GetPlayerDataQuery { IncludeInventory = true };
var playerData = ModuleCommunicator.Query<GetPlayerDataQuery, PlayerData>(query);

var saveCommand = new SaveGameCommand { SaveSlotName = "manual_save" };
ModuleCommunicator.SendCommand(saveCommand);
```

## 常见场景迁移示例

### 场景1：数据获取

**迁移前：**
```csharp
// 紧耦合的数据获取
var config = FrameworkManager.GetModule<ConfigModule>().GetConfig();
```

**迁移后：**
```csharp
// 定义查询
public struct GetConfigQuery : IModuleQuery<GameConfig> { }

// 注册处理器（ConfigModule中）
ModuleCommunicator.RegisterQueryHandler<GetConfigQuery, GameConfig>(HandleGetConfig);

// 使用查询
var config = ModuleCommunicator.Query<GetConfigQuery, GameConfig>(new GetConfigQuery());
```

### 场景2：状态变更

**迁移前：**
```csharp
// 直接调用状态变更
FrameworkManager.GetModule<UIModule>().ShowDialog("Hello");
```

**迁移后：**
```csharp
// 定义命令
public struct ShowDialogCommand : IModuleCommand
{
    public string Message;
    public float Duration;
}

// 注册处理器（UIModule中）  
ModuleCommunicator.RegisterCommandHandler<ShowDialogCommand>(HandleShowDialog);

// 发送命令
ModuleCommunicator.SendCommand(new ShowDialogCommand 
{ 
    Message = "Hello", 
    Duration = 2f 
});
```

### 场景3：复杂操作

**迁移前：**
```csharp
// 复杂的直接调用链
var player = FrameworkManager.GetModule<PlayerModule>();
var inventory = FrameworkManager.GetModule<InventoryModule>();

var item = inventory.GetItem("sword");
player.EquipItem(item);
```

**迁移后：**
```csharp
// 定义复合命令
public struct EquipItemCommand : IModuleCommand
{
    public string ItemId;
}

// 在PlayerModule中处理完整逻辑
ModuleCommunicator.RegisterCommandHandler<EquipItemCommand>(HandleEquipItem);

private void HandleEquipItem(EquipItemCommand command)
{
    var inventory = FrameworkManager.GetModule<InventoryModule>();
    var item = inventory.GetItem(command.ItemId);
    EquipItem(item); // 内部方法
}
```

## 最佳实践

### 1. 协议设计原则

- **单一职责**：每个命令/查询只做一件事
- **明确命名**：使用清晰的动词+名词命名
- **数据封装**：包含所有必要的数据字段

### 2. 错误处理

```csharp
// 在查询处理器中处理错误
private PlayerData HandleGetPlayerData(GetPlayerDataQuery query)
{
    try
    {
        if (!IsPlayerLoaded())
            throw new InvalidOperationException("Player not loaded");
            
        return _playerData;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Query failed: {ex.Message}");
        return null; // 或者抛出特定异常
    }
}
```

### 3. 性能考虑

- 对于高频调用，考虑使用结构体而非类
- 避免在命令/查询中包含大型数据
- 使用合适的缓存策略

### 4. 版本兼容性

- 添加新字段时使用默认值保持向后兼容
- 避免删除或重命名字段
- 使用[Obsolete]标记过时的协议

## 调试技巧

### 1. 日志记录

```csharp
// 在ModuleCommunicator中添加调试日志
#if UNITY_EDITOR
Debug.Log($"Sending command: {typeof(TCommand).Name}");
#endif
```

### 2. 事件追踪

使用框架的调试工具查看事件流：
- 打开Framework Debugger
- 查看事件发送和接收情况
- 监控性能指标

### 3. 断点调试

在处理器方法中设置断点，跟踪命令/查询的执行流程。

## 常见问题

### Q: 如何处理模块尚未初始化的情况？
A: 事件总线会缓存未处理的事件，待模块注册后自动处理。

### Q: 如果多个模块注册了相同的处理器会怎样？
A: 单播模式只允许一个处理器，后注册的会替换先前的。

### Q: 如何确保线程安全？
A: 事件总线内置线程安全机制，无需额外处理。

## 总结

迁移到事件驱动的通信模式需要一些前期工作，但会带来更好的架构和可维护性。建议逐步迁移，先从简单的交互开始，逐步覆盖所有模块间通信。

如有问题，参考示例代码或查看框架文档。