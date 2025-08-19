# Cnoom Unity Framework

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

一个为个人独立游戏开发设计的Unity通用基础框架，包含事件系统、配置管理和日志服务等核心功能。

## 特性概览

- **模块化架构**：所有功能单元均为模块，生命周期（Init/Start/Shutdown）清晰，支持依赖声明与热替换。
- **事件总线**：模块间完全解耦，通信通过事件/请求-响应/广播完成，支持特性自动注册、优先级、异步、继承分发。
- **调试与可视化**：内置事件日志、事件流可视化（Editor 工具），便于追踪模块状态与事件流。
- **Mock与热替换**：支持Mock模块、生命周期绑定与热替换，便于测试和原型开发。
- **性能分层通信**：高频逻辑可直接调用，低频逻辑走事件，支持异步事件处理，降低主线程阻塞。
- **安全防护**：契约验证、异常保护，避免单模块崩溃影响全局。

## 目录结构

```
Runtime/
  Core/         # 框架核心（模块基类、事件总线、依赖、异常等）
  Examples/     # 示例模块与脚本
  Utils/        # 日志等工具
Editor/
  FrameworkDebugger/  # 事件流可视化、性能监控等调试工具
```

## 快速开始

1. **导入包**：将本包放入 `Packages/` 目录，Unity 2022.3+ 兼容。
2. **初始化框架**：
   ```csharp
   FrameworkInitializer.Initialize();
   ```
3. **创建模块**：继承 `BaseModule`，实现生命周期方法。
   ```csharp
   public class ExampleModule : BaseModule { ... }
   ```
4. **事件通信**：
   - 发布事件：
     ```csharp
     _eventBus.Publish<IHealthChangedEvent>(new HealthChangedEvent(oldValue, newValue));
     ```
   - 订阅事件：
     ```csharp
     [SubscribeEvent(typeof(IHealthChangedEvent))]
     private void OnHealthChanged(IHealthChangedEvent evt) { ... }
     ```
5. **调试与可视化**：在 Unity Editor 菜单中打开 FrameworkDebugger 工具。

## 示例

- 参考 `Runtime/Examples/` 目录下的模块实现与测试脚本。
- 事件流可视化、性能监控等工具见 `Editor/FrameworkDebugger/`。

## 依赖
- Unity 2022.3+
- com.unity.addressables
- com.unity.nuget.newtonsoft-json

## 文档
- [设计文档](设计文档.md)
- [API 文档](https://github.com/cnoom/unity-framework)

## 许可证

本项目基于 MIT License 开源，详见 [LICENSE.md](LICENSE.md)。
