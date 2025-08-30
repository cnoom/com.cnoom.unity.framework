# Cnoom's Personal Framework

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

一个轻量级的Unity个人开发框架，专注于事件系统和模块化架构。简单易用，适合个人项目快速开发。

> 🎯 **个人项目优先**：这个框架主要是为了我自己的项目而设计的，但如果你觉得有用，欢迎使用！

## 核心特性

- **极简模块系统**：清晰的Init/Start/Shutdown生命周期，支持优先级和自动注册
- **事件驱动架构**：模块间通过事件通信，支持广播、单播和请求响应模式
- **配置管理**：简单的键值对配置，支持多种存储方式
- **错误处理**：完善的异常保护和恢复机制，支持自定义恢复策略
- **调试工具**：内置事件查看器和可视化工具，便于调试和性能监控
- **Mock支持**：支持模块Mock和热替换，便于测试和原型开发

## 快速上手

### 1. 安装框架
将包放入项目的 `Packages/` 目录即可使用。

### 2. 初始化框架
```csharp
// 在游戏启动时调用
FrameworkManager.Instance.Initialize();
```

### 3. 创建你的第一个模块
```csharp
public class PlayerModule : BaseModule
{
    public override string Name => "PlayerModule";
    
    protected override void OnInit()
    {
        // 注册事件处理器
        EventBus.SubscribeBroadcast<PlayerHealthChangedEvent>(OnHealthChanged);
        
        // 注册命令处理器
        EventBus.RegisterCommandHandler<PlayerAttackCommand>(OnPlayerAttack);
        
        // 注册查询处理器
        EventBus.RegisterQueryHandler<PlayerStatsQuery, PlayerStats>(OnPlayerStatsQuery);
    }
    
    private void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        Debug.Log($"生命值变化: {evt.OldValue} -> {evt.NewValue}");
    }
    
    private void OnPlayerAttack(PlayerAttackCommand command)
    {
        // 处理攻击命令
        Debug.Log($"玩家攻击: {command.Damage}");
    }
    
    private PlayerStats OnPlayerStatsQuery(PlayerStatsQuery query)
    {
        // 返回玩家统计数据
        return new PlayerStats { Health = 100, Level = 5 };
    }
}
```

### 4. 使用模块通信
```csharp
// 发送广播事件
var healthEvent = new PlayerHealthChangedEvent(100, 80);
FrameworkManager.Instance.EventBus.Broadcast(healthEvent);

// 发送命令到特定模块
var attackCommand = new PlayerAttackCommand { Damage = 20 };
FrameworkManager.Instance.EventBus.SendCommand(attackCommand);

// 查询模块数据
var statsQuery = new PlayerStatsQuery();
var playerStats = FrameworkManager.Instance.EventBus.Query<PlayerStatsQuery, PlayerStats>(statsQuery);
Debug.Log($"玩家等级: {playerStats.Level}");
```

### 5. 使用轻量级契约验证（可选）
```csharp
// 启用契约验证
var validationModule = FrameworkManager.Instance.GetModule<LightweightContractValidationModule>();
validationModule.SetValidationEnabled(true);

// 注册事件契约
validationModule.RegisterEvent<PlayerHealthChangedEvent>();

// 自动验证发送的事件
FrameworkManager.Instance.EventBus.Broadcast(healthEvent); // 会自动验证
```

## 目录结构
```
Runtime/
  Core/         # 核心功能（模块管理、事件总线、配置）
  Utils/        # 工具类
Editor/
  FrameworkDebugger/  # 调试工具
```

## 依赖
- Unity 2022.3+
- com.unity.nuget.newtonsoft-json (可选，用于JSON配置)

## 文档
- [设计思路](设计文档.md) - 了解框架的设计哲学
- 查看代码注释获取详细API说明

## 许可证
MIT License - 可以自由使用，但请保留原作者的署名。

---

💡 **提示**：这个框架还在不断改进中，主要服务于我自己的项目需求。如果你有任何建议或发现问题，欢迎在GitHub上提出！
