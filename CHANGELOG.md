# 变更日志

所有重要的项目更新都将记录在此文件中。



## [1.0.2] - 2025-08-30
### 新增
- **轻量级契约验证系统**：为个人开发者设计的简化验证框架
  - `LightweightContractValidationModule`：自动注册的轻量级验证模块
  - `LightweightContractValidator`：简化的事件契约验证器
  - `ContractValidationSettings`：编辑器可配置的验证设置
- **模块通信API扩展**：
  - `SendCommand<T>()`：发送命令到目标模块
  - `Query<TQuery, TResponse>()`：查询模块数据
  - 完整的命令和查询处理器注册/取消注册API
- **EventBus架构重构**：
  - 新增`RequestBus`：专门处理请求-响应模式
  - 新增`UnicastBus`：专门处理单播通信
  - 保持原有广播接口兼容性

### 修复
- 无

### 更改
- **EventBus重构**：将原有facade模式重构为专门的bus实现
- **方法命名统一**：将Publish方法统一命名为Broadcast
- **模块加载优化**：支持按优先级顺序初始化模块

### 优化
- **性能优化**：事件处理逻辑优化，减少锁竞争
- **错误处理**：改进模块发现和实例创建的异常处理
- **代码可读性**：EventBus核心代码重构，提高可维护性

### 移除
- 移除原有的`RequestFacade`和`UnicastFacade`
- 移除完整的契约验证系统（替换为轻量级版本）

[1.0.2]: https://github.com/cnoom/com.cnoom.unity.framework/compare/v1.0.1...v1.0.2

## [1.0.1] - 2025-08-19
### 新增
- 添加 `AutoRegisterModule` 特性，支持自动注册具有该特性的模块。
- 更新模块注册逻辑，以支持 `AutoRegisterModule` 特性。

### 修复
- 无

### 更改
- 更新 `package.json` 版本至 `1.0.1`。

[1.0.1]: https://github.com/cnoom/com.cnoom.unity.framework/compare/v1.0.0...v1.0.1

## [1.0.0] - 2025-08-19
### 新增
- 初始版本发布。
- 模块化架构：所有功能单元均为模块，生命周期（Init/Start/Shutdown）清晰，支持依赖声明与热替换。
- 事件总线：模块间完全解耦，通信通过事件/请求-响应/广播完成，支持特性自动注册、优先级、异步、继承分发。
- 调试与可视化工具：内置事件日志、事件流可视化（Editor 工具），便于追踪模块状态与事件流。
- Mock与热替换支持：支持Mock模块、生命周期绑定与热替换，便于测试和原型开发。
- 性能分层通信：高频逻辑可直接调用，低频逻辑走事件，支持异步事件处理，降低主线程阻塞。
- 安全防护：契约验证、异常保护，避免单模块崩溃影响全局。
- 事件流可视化、性能监控等工具：见 `Editor/FrameworkDebugger/`。
- 配置管理器：支持多种配置源（内存、PlayerPrefs、JSON文件），并提供加载和保存功能。
- 错误恢复管理器：提供默认的错误恢复策略，支持自定义恢复策略。
- 自动发现模块：自动扫描程序集中的模块类型，并注册具有 `AutoRegisterModule` 特性的模块。

### 修复
- 无

### 更改
- 无

[1.0.0]: https://github.com/cnoom/com.cnoom.unity.framework/compare/v0.0.0...v1.0.0