using System;
using System.Collections.Generic;
using UnityEngine;

namespace CnoomFramework.Tests
{
    /// <summary>
    /// 测试套件配置
    /// </summary>
    [CreateAssetMenu(fileName = "TestSuiteConfig", menuName = "CnoomFramework/Test Suite Config")]
    public class TestSuiteConfig : ScriptableObject
    {
        [Header("测试运行配置")]
        [SerializeField] private bool enablePerformanceTests = true;
        [SerializeField] private bool enableIntegrationTests = true;
        [SerializeField] private bool enableEditorTests = true;
        [SerializeField] private int performanceTestIterations = 1000;
        [SerializeField] private float performanceTimeoutSeconds = 30f;

        [Header("测试报告配置")]
        [SerializeField] private bool generateDetailedReport = true;
        [SerializeField] private bool includePerformanceMetrics = true;
        [SerializeField] private bool exportToFile = false;
        [SerializeField] private string reportOutputPath = "TestReports/";

        [Header("测试数据配置")]
        [SerializeField] private List<TestScenario> testScenarios = new List<TestScenario>();

        public bool EnablePerformanceTests => enablePerformanceTests;
        public bool EnableIntegrationTests => enableIntegrationTests;
        public bool EnableEditorTests => enableEditorTests;
        public int PerformanceTestIterations => performanceTestIterations;
        public float PerformanceTimeoutSeconds => performanceTimeoutSeconds;
        public bool GenerateDetailedReport => generateDetailedReport;
        public bool IncludePerformanceMetrics => includePerformanceMetrics;
        public bool ExportToFile => exportToFile;
        public string ReportOutputPath => reportOutputPath;
        public List<TestScenario> TestScenarios => testScenarios;

        private void OnValidate()
        {
            performanceTestIterations = Mathf.Max(1, performanceTestIterations);
            performanceTimeoutSeconds = Mathf.Max(1f, performanceTimeoutSeconds);
        }
    }

    [Serializable]
    public class TestScenario
    {
        [SerializeField] private string name;
        [SerializeField] private string description;
        [SerializeField] private List<string> requiredModules = new List<string>();
        [SerializeField] private Dictionary<string, object> parameters = new Dictionary<string, object>();

        public string Name => name;
        public string Description => description;
        public List<string> RequiredModules => requiredModules;
        public Dictionary<string, object> Parameters => parameters;
    }
}