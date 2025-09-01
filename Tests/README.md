# Cnoom Framework 单元测试套件

## 📋 概述

本测试套件为 Cnoom Unity Framework 提供全面的单元测试覆盖，确保框架的质量和稳定性。

## 🎯 测试范围

### 1. 核心组件测试
- **事件总线系统** (`EventBusTests.cs`)
  - 广播/单播/请求-响应模式测试
  - 优先级处理和异步执行测试
  - 性能和并发安全测试
  - 错误处理和恢复测试

- **模块管理系统** (`ModuleSystemTests.cs`)
  - 模块生命周期管理测试
  - 依赖注入和自动注册测试
  - 模块状态管理和异常处理测试
  - 热替换和Mock支持测试

- **配置管理系统** (`ConfigManagerTests.cs`)
  - 多种配置源支持测试
  - 数据类型序列化测试
  - 持久化和优先级测试
  - 性能和并发访问测试

- **错误处理系统** (`ErrorHandlingTests.cs`)
  - SafeExecutor安全执行测试
  - 错误恢复策略测试
  - 框架异常类型测试
  - 异步错误处理测试

- **性能监控系统** (`PerformanceMonitorTests.cs`)
  - 性能采样和指标记录测试
  - 内存使用监控测试
  - 嵌套采样和并发安全测试
  - 性能开销基准测试

### 2. 集成测试
- **完整框架生命周期测试** (`IntegrationTests.cs`)
  - 框架初始化和关闭流程测试
  - 跨模块事件通信测试
  - 配置与事件系统集成测试
  - 错误恢复集成测试
  - 性能负载和内存管理测试
  - 多模块协作测试

### 3. Editor工具测试
- **调试工具测试** (`EditorToolsTests.cs`)
  - 框架调试器功能测试
  - 事件流可视化测试
  - 性能监控器测试
  - Mock管理器测试
  - 配置编辑器测试
  - 自动化测试工具测试

## 🚀 运行测试

1. 打开 `Window` → `General` → `Test Runner`
2. 选择 `PlayMode` 或 `EditMode` 标签
3. 运行相应的测试套件

## ⚙️ 配置选项

通过创建 `TestSuiteConfig` 资源文件来配置测试行为：

```csharp
[CreateAssetMenu(fileName = "TestSuiteConfig", menuName = "CnoomFramework/Test Suite Config")]
```

### 可配置项：
- **测试运行配置**
  - `enablePerformanceTests`: 是否启用性能测试
  - `enableIntegrationTests`: 是否启用集成测试
  - `enableEditorTests`: 是否启用Editor测试
  - `performanceTestIterations`: 性能测试迭代次数
  - `performanceTimeoutSeconds`: 性能测试超时时间

- **测试报告配置**
  - `generateDetailedReport`: 是否生成详细报告
  - `includePerformanceMetrics`: 是否包含性能指标
  - `exportToFile`: 是否导出报告到文件
  - `reportOutputPath`: 报告输出路径

## 📊 测试报告

测试完成后会自动生成详细的测试报告，包含：

### 报告内容
1. **测试结果汇总**
   - 总测试数、通过率、失败数、跳过数
   - 测试运行时长和生成时间

2. **详细测试结果**
   - 按类别分组的测试结果
   - 每个测试的执行时间和状态
   - 失败测试的错误信息和详情

3. **性能指标**
   - 平均/最大/最小执行时间
   - 内存使用情况
   - 性能对比和趋势分析

4. **建议和总结**
   - 需要关注的问题列表
   - 性能优化建议
   - 总体质量评估

### 报告示例
```markdown
# Cnoom Framework 测试报告
生成时间: 2024-01-15 14:30:25
测试运行时长: 45.67 秒

## 测试结果汇总
- 总测试数: 156
- 通过: 152 (97.4%)
- 失败: 3
- 跳过: 1

## 性能指标
| 测试名称 | 平均执行时间 | 最大执行时间 | 最小执行时间 | 内存使用 |
|----------|-------------|-------------|-------------|----------|
| EventBus_Performance | 0.23ms | 1.45ms | 0.12ms | 12.3KB |
| Module_Performance | 15.67ms | 25.43ms | 8.92ms | 45.6KB |
```

## 🔧 自定义测试

### 添加新的测试类
1. 在相应的测试目录下创建新的测试类
2. 继承或使用 NUnit 测试框架的特性
3. 实现 `SetUp` 和 `TearDown` 方法

```csharp
public class YourCustomTests
{
    [SetUp]
    public void SetUp()
    {
        // 测试前的初始化
    }

    [Test]
    public void YourTest_ShouldWork()
    {
        // Arrange
        // Act
        // Assert
    }

    [TearDown]
    public void TearDown()
    {
        // 测试后的清理
    }
}
```

### 添加性能基准测试
1. 在 `FrameworkTestRunner.cs` 中添加新的基准测试方法
2. 使用 `PerformanceMetrics` 记录性能数据
3. 在报告中包含性能分析

## 📈 CI/CD 集成

### GitHub Actions 示例
```yaml
name: Unity Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - uses: game-ci/unity-test-runner@v2
      with:
        projectPath: .
        testMode: PlayMode
        artifactsPath: test-results
    - uses: actions/upload-artifact@v2
      with:
        name: Test Results
        path: test-results
```

### Jenkins 集成
```groovy
pipeline {
    agent any
    stages {
        stage('Test') {
            steps {
                bat 'Unity -batchmode -runTests -testPlatform PlayMode'
            }
            post {
                always {
                    publishTestResults 'TestResults.xml'
                }
            }
        }
    }
}
```

## 🐛 故障排除

### 常见问题

1. **测试运行失败**
   - 检查Unity版本是否为2022.3+
   - 确认所有依赖包已正确安装
   - 检查测试程序集引用是否正确

2. **性能测试超时**
   - 增加 `performanceTimeoutSeconds` 配置
   - 减少 `performanceTestIterations` 数值
   - 检查系统资源是否充足

3. **Editor测试失败**
   - 确认在Unity Editor环境中运行
   - 检查Editor脚本编译是否成功
   - 验证Editor窗口类型是否存在

4. **内存泄漏检测**
   - 运行前执行强制垃圾回收
   - 使用Unity Profiler监控内存使用
   - 检查测试用的GameObject是否正确销毁

### 调试技巧

1. **启用详细日志**
   ```csharp
   // 在测试中添加详细日志
   Debug.Log($"[Test] {testName} - {details}");
   ```

2. **使用断点调试**
   - 在Visual Studio中设置断点
   - 使用Unity的调试模式运行测试

3. **性能分析**
   - 使用Unity Profiler监控性能
   - 记录关键操作的执行时间
   - 分析内存分配和释放

## 📝 贡献指南

### 添加新测试
1. 确定测试类别和测试范围
2. 创建相应的测试文件
3. 编写全面的测试用例
4. 更新本README文档
5. 提交Pull Request

### 测试质量标准
- 测试覆盖率应达到90%以上
- 每个测试应该有明确的Arrange-Act-Assert结构
- 测试名称应该清晰描述测试的目的
- 异常情况和边界条件应该被充分测试
- 性能测试应该有合理的基准和超时设置

## 📞 支持

如果您在使用测试套件时遇到问题，请：

1. 查看本文档的故障排除部分
2. 检查Console中的错误信息
3. 在GitHub Issues中搜索相关问题
4. 提交新的Issue并提供详细的错误信息

---

**注意**: 这个测试套件是Cnoom Framework的重要组成部分，请确保在每次代码修改后运行完整的测试套件以保证框架的稳定性。