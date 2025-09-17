# Cnoom Framework Editor Tools

这是 Cnoom Unity Framework 的完整 Editor 工具集，提供了强大的调试、监控和开发辅助功能。

## 🛠️ 工具概览

### 1. 框架调试器 (FrameworkDebuggerWindow)
**菜单路径**: `Tools/Cnoom Framework/Framework Debugger`

主要的调试控制面板，包含以下标签页：
- **概览**: 框架整体状态和快速操作
- **模块**: 实时模块状态监控和管理
- **事件总线**: 事件流监控和测试
- **性能**: 性能数据可视化和分析
- **配置**: 可视化配置编辑器
- **错误日志**: 错误追踪和分析

**特性**:
- 实时状态监控
- 自动刷新控制
- 模块生命周期管理
- 事件测试发送
- 性能图表显示
- 错误报告导出

### 2. 模块状态查看器 (ModuleStatusViewer)
集成在框架调试器中的模块监控工具。

**功能**:
- 模块状态实时显示
- 按状态和名称过滤
- 模块依赖关系查看
- 单个模块操作控制
- 详细模块信息展示

### 3. 事件总线监视器 (EventBusMonitor)
监控和调试事件系统的专用工具。

**功能**:
- 事件日志记录
- 事件类型过滤
- 测试事件发送
- 事件流量统计
- 实时事件监控

### 4. 性能监控面板 (PerformancePanel)
提供详细的性能数据分析。

**功能**:
- 实时性能指标显示
- 历史数据图表
- FPS、帧时间、内存监控
- 性能警告提示
- 可配置监控参数

### 5. 配置编辑器 (ConfigEditor)
可视化的配置管理工具。

**功能**:
- 配置源管理
- 分组配置显示
- 实时配置编辑
- 配置搜索过滤
- 批量配置操作

### 6. 错误日志查看器 (ErrorLogViewer)
错误追踪和分析工具。

**功能**:
- 错误历史记录
- 按严重级别过滤
- 详细错误信息显示
- 错误报告导出
- 堆栈跟踪查看

### 7. 模块模板生成器 (ModuleTemplateGenerator)
**菜单路径**: `Tools/Cnoom Framework/Tools/Generate Module Template`

快速生成模块代码模板的工具。

**功能**:
- 自定义模块名称和命名空间
- 可选功能组件（事件处理器、命令处理器、查询处理器）
- 自动注册配置
- 代码模板生成

### 8. 模块依赖关系可视化器 (ModuleDependencyVisualizer)
**菜单路径**: `Tools/Cnoom Framework/Module Dependency Visualizer`

可视化模块依赖关系的图形工具。

**功能**:
- 依赖关系图形显示
- 自动布局算法
- 循环依赖检测
- 模块状态可视化
- 交互式节点选择

### 9. 项目设置 (FrameworkProjectSettings)
**菜单路径**: `Project Settings/Cnoom Framework`

框架的项目级配置管理。

**功能**:
- 框架行为配置
- 性能监控设置
- 错误处理配置
- 事件总线参数
- 设置导入导出

### 10. 自定义Inspector (FrameworkManagerInspector)
为 FrameworkManager 组件提供增强的 Inspector 界面。

**功能**:
- 框架状态显示
- 系统组件信息
- 模块列表查看
- 快速调试操作
- 工具窗口快速访问

## 🚀 快速开始

### 安装和设置
1. 确保 Cnoom Framework 已正确安装
2. 所有 Editor 工具会自动注册到 Unity 菜单中
3. 通过 `Tools/Cnoom Framework` 菜单访问各种工具

### 基本工作流程
1. **初始化框架**: 使用 `Tools/Cnoom Framework/Quick Actions/Initialize Framework`
2. **打开调试器**: `Tools/Cnoom Framework/Framework Debugger`
3. **监控状态**: 在调试器中查看模块状态和系统信息
4. **性能分析**: 使用性能面板监控应用性能
5. **错误追踪**: 通过错误日志查看器分析问题

### 开发新模块
1. **生成模板**: `Tools/Cnoom Framework/Tools/Generate Module Template`
2. **编辑代码**: 根据需求修改生成的模板
3. **查看依赖**: 使用依赖关系可视化器检查模块关系
4. **测试模块**: 在框架调试器中监控模块状态

## 📋 菜单结构

```
Tools/Cnoom Framework/
├── Framework Debugger                    # 主调试器
├── Module Dependency Visualizer          # 依赖关系可视化
├── Quick Actions/
│   ├── Initialize Framework             # 初始化框架
│   ├── Shutdown Framework              # 关闭框架
│   └── Restart Framework               # 重启框架
├── Tools/
│   ├── Clear All Configs               # 清空配置
│   ├── Clear Error History             # 清空错误历史
│   └── Generate Module Template        # 生成模块模板
└── Help/
    ├── Framework Documentation         # 文档链接
    ├── Report Issue                   # 问题报告
    └── About Framework               # 关于信息
```

## 🎯 使用技巧

### 性能优化
- 使用性能面板监控 FPS 和内存使用
- 关注性能警告提示
- 调整事件总线参数优化性能

### 调试技巧
- 启用事件日志记录来追踪事件流
- 使用模块状态查看器监控模块生命周期
- 通过错误日志查看器分析异常

### 开发效率
- 使用模块模板生成器快速创建新模块
- 利用依赖关系可视化器理解模块架构
- 通过项目设置统一配置框架行为

## 🔧 自定义和扩展

所有工具都设计为可扩展的，您可以：
- 继承现有的编辑器类添加新功能
- 修改可视化样式和布局
- 添加新的监控指标
- 扩展模板生成器支持更多模式

## 📝 注意事项

1. **性能影响**: 某些监控功能可能对编辑器性能有轻微影响，可根据需要开启/关闭
2. **数据持久化**: 大部分设置会自动保存，但建议定期备份项目设置
3. **版本兼容**: 工具与框架版本紧密关联，升级时请注意兼容性

## 🐛 问题反馈

如果遇到问题或有改进建议，请通过以下方式反馈：
- GitHub Issues: https://github.com/cnoom/unity-framework/issues
- 或通过 `Help/Report Issue` 菜单项直接跳转

---

这套 Editor 工具集旨在提供完整的开发和调试体验，让您能够更高效地使用 Cnoom Framework 开发项目。