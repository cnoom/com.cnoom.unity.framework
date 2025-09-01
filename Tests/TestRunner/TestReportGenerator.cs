using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 测试报告生成器
    /// </summary>
    public class TestReportGenerator
    {
        private readonly List<TestResult> _testResults = new List<TestResult>();
        private readonly Dictionary<string, PerformanceMetrics> _performanceMetrics = new Dictionary<string, PerformanceMetrics>();
        private DateTime _testRunStartTime;
        private DateTime _testRunEndTime;

        public void StartTestRun()
        {
            _testRunStartTime = DateTime.Now;
            _testResults.Clear();
            _performanceMetrics.Clear();
            
            Debug.Log($"[TestReportGenerator] 测试运行开始: {_testRunStartTime:yyyy-MM-dd HH:mm:ss}");
        }

        public void EndTestRun()
        {
            _testRunEndTime = DateTime.Now;
            Debug.Log($"[TestReportGenerator] 测试运行结束: {_testRunEndTime:yyyy-MM-dd HH:mm:ss}");
        }

        public void RecordTestResult(TestResult result)
        {
            _testResults.Add(result);
            Debug.Log($"[TestReportGenerator] 记录测试结果: {result.TestName} - {result.Status}");
        }

        public void RecordPerformanceMetrics(string testName, PerformanceMetrics metrics)
        {
            _performanceMetrics[testName] = metrics;
            Debug.Log($"[TestReportGenerator] 记录性能指标: {testName} - {metrics.AverageExecutionTime:F2}ms");
        }

        public string GenerateReport(TestSuiteConfig config = null)
        {
            var report = new StringBuilder();
            
            // 标题和概述
            report.AppendLine("# Cnoom Framework 测试报告");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"测试运行时长: {(_testRunEndTime - _testRunStartTime).TotalSeconds:F2} 秒");
            report.AppendLine();

            // 测试结果汇总
            GenerateTestSummary(report);

            // 详细测试结果
            if (config?.GenerateDetailedReport == true)
            {
                GenerateDetailedResults(report);
            }

            // 性能指标
            if (config?.IncludePerformanceMetrics == true)
            {
                GeneratePerformanceReport(report);
            }

            // 建议和总结
            GenerateRecommendations(report);

            var reportContent = report.ToString();
            
            // 导出到文件
            if (config?.ExportToFile == true)
            {
                ExportReportToFile(reportContent, config.ReportOutputPath);
            }

            return reportContent;
        }

        private void GenerateTestSummary(StringBuilder report)
        {
            var totalTests = _testResults.Count;
            var passedTests = 0;
            var failedTests = 0;
            var skippedTests = 0;

            foreach (var result in _testResults)
            {
                switch (result.Status)
                {
                    case TestStatus.Passed:
                        passedTests++;
                        break;
                    case TestStatus.Failed:
                        failedTests++;
                        break;
                    case TestStatus.Skipped:
                        skippedTests++;
                        break;
                }
            }

            var passRate = totalTests > 0 ? (double)passedTests / totalTests * 100 : 0;

            report.AppendLine("## 测试结果汇总");
            report.AppendLine($"- 总测试数: {totalTests}");
            report.AppendLine($"- 通过: {passedTests} ({passRate:F1}%)");
            report.AppendLine($"- 失败: {failedTests}");
            report.AppendLine($"- 跳过: {skippedTests}");
            report.AppendLine();
        }

        private void GenerateDetailedResults(StringBuilder report)
        {
            report.AppendLine("## 详细测试结果");
            
            // 按类别分组
            var categories = new Dictionary<string, List<TestResult>>();
            foreach (var result in _testResults)
            {
                if (!categories.ContainsKey(result.Category))
                {
                    categories[result.Category] = new List<TestResult>();
                }
                categories[result.Category].Add(result);
            }

            foreach (var category in categories)
            {
                report.AppendLine($"### {category.Key}");
                
                foreach (var result in category.Value)
                {
                    var statusIcon = result.Status switch
                    {
                        TestStatus.Passed => "✅",
                        TestStatus.Failed => "❌",
                        TestStatus.Skipped => "⏭️",
                        _ => "❓"
                    };

                    report.AppendLine($"{statusIcon} **{result.TestName}** ({result.ExecutionTime:F2}ms)");
                    
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        report.AppendLine($"   错误: {result.ErrorMessage}");
                    }
                    
                    if (!string.IsNullOrEmpty(result.Details))
                    {
                        report.AppendLine($"   详情: {result.Details}");
                    }
                }
                
                report.AppendLine();
            }
        }

        private void GeneratePerformanceReport(StringBuilder report)
        {
            if (_performanceMetrics.Count == 0)
                return;

            report.AppendLine("## 性能指标");
            report.AppendLine("| 测试名称 | 平均执行时间 | 最大执行时间 | 最小执行时间 | 内存使用 |");
            report.AppendLine("|----------|-------------|-------------|-------------|----------|");

            foreach (var metric in _performanceMetrics)
            {
                report.AppendLine($"| {metric.Key} | {metric.Value.AverageExecutionTime:F2}ms | " +
                                $"{metric.Value.MaxExecutionTime:F2}ms | {metric.Value.MinExecutionTime:F2}ms | " +
                                $"{metric.Value.MemoryUsage / 1024:F1}KB |");
            }
            
            report.AppendLine();
        }

        private void GenerateRecommendations(StringBuilder report)
        {
            report.AppendLine("## 建议和总结");

            var failedTests = _testResults.FindAll(r => r.Status == TestStatus.Failed);
            if (failedTests.Count > 0)
            {
                report.AppendLine("### ⚠️ 需要关注的问题");
                foreach (var failed in failedTests)
                {
                    report.AppendLine($"- {failed.TestName}: {failed.ErrorMessage}");
                }
                report.AppendLine();
            }

            // 性能建议
            if (_performanceMetrics.Count > 0)
            {
                report.AppendLine("### 🚀 性能优化建议");
                
                foreach (var metric in _performanceMetrics)
                {
                    if (metric.Value.AverageExecutionTime > 100) // 超过100ms
                    {
                        report.AppendLine($"- {metric.Key}: 平均执行时间较长 ({metric.Value.AverageExecutionTime:F2}ms)，建议优化");
                    }
                    
                    if (metric.Value.MemoryUsage > 1024 * 1024) // 超过1MB
                    {
                        report.AppendLine($"- {metric.Key}: 内存使用较高 ({metric.Value.MemoryUsage / 1024 / 1024:F1}MB)，建议检查内存泄漏");
                    }
                }
                report.AppendLine();
            }

            // 总体评估
            var passRate = _testResults.Count > 0 ? 
                (double)_testResults.FindAll(r => r.Status == TestStatus.Passed).Count / _testResults.Count * 100 : 0;

            report.AppendLine("### 📊 总体评估");
            if (passRate >= 95)
            {
                report.AppendLine("🎉 优秀！框架质量很高，所有核心功能都运行正常。");
            }
            else if (passRate >= 80)
            {
                report.AppendLine("👍 良好！大部分功能正常，有少量问题需要修复。");
            }
            else if (passRate >= 60)
            {
                report.AppendLine("⚠️ 一般！存在较多问题，需要重点关注失败的测试。");
            }
            else
            {
                report.AppendLine("🚨 需要改进！框架存在重大问题，建议全面检查。");
            }
        }

        private void ExportReportToFile(string reportContent, string outputPath)
        {
            try
            {
                var fullPath = $"{outputPath}TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.md";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
                System.IO.File.WriteAllText(fullPath, reportContent, Encoding.UTF8);
                
                Debug.Log($"[TestReportGenerator] 测试报告已导出到: {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestReportGenerator] 导出报告失败: {ex.Message}");
            }
        }
    }

    #region 数据类

    [Serializable]
    public class TestResult
    {
        public string TestName { get; set; }
        public string Category { get; set; }
        public TestStatus Status { get; set; }
        public float ExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
        public string Details { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public TestResult(string testName, string category = "General")
        {
            TestName = testName;
            Category = category;
            Status = TestStatus.Pending;
            StartTime = DateTime.Now;
        }

        public void MarkPassed(string details = null)
        {
            Status = TestStatus.Passed;
            EndTime = DateTime.Now;
            ExecutionTime = (float)(EndTime - StartTime).TotalMilliseconds;
            Details = details;
        }

        public void MarkFailed(string errorMessage, string details = null)
        {
            Status = TestStatus.Failed;
            EndTime = DateTime.Now;
            ExecutionTime = (float)(EndTime - StartTime).TotalMilliseconds;
            ErrorMessage = errorMessage;
            Details = details;
        }

        public void MarkSkipped(string reason = null)
        {
            Status = TestStatus.Skipped;
            EndTime = DateTime.Now;
            Details = reason;
        }
    }

    [Serializable]
    public class PerformanceMetrics
    {
        public float AverageExecutionTime { get; set; }
        public float MaxExecutionTime { get; set; }
        public float MinExecutionTime { get; set; }
        public long MemoryUsage { get; set; }
        public int SampleCount { get; set; }
        public float TotalExecutionTime { get; set; }

        public void AddSample(float executionTime, long memoryDelta = 0)
        {
            SampleCount++;
            TotalExecutionTime += executionTime;
            AverageExecutionTime = TotalExecutionTime / SampleCount;
            
            if (SampleCount == 1)
            {
                MaxExecutionTime = MinExecutionTime = executionTime;
            }
            else
            {
                MaxExecutionTime = Mathf.Max(MaxExecutionTime, executionTime);
                MinExecutionTime = Mathf.Min(MinExecutionTime, executionTime);
            }
            
            MemoryUsage += memoryDelta;
        }
    }

    public enum TestStatus
    {
        Pending,
        Running,
        Passed,
        Failed,
        Skipped
    }

    #endregion
}