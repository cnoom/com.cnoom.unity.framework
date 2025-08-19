using System;
using System.Collections.Generic;
using System.Linq;
using CnoomFramework.Core;
using CnoomFramework.Core.Attributes;
using UnityEngine;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// 依赖分析器，用于分析模块间的依赖关系
    /// </summary>
    public static class DependencyAnalyzer
    {
        /// <summary>
        /// 依赖关系信息
        /// </summary>
        public class DependencyInfo
        {
            public string ModuleName { get; set; }
            public List<string> Dependencies { get; set; } = new List<string>();
            public List<string> Dependents { get; set; } = new List<string>();
            public int DependencyDepth { get; set; }
            public bool HasCircularDependency { get; set; }
        }

        /// <summary>
        /// 循环依赖信息
        /// </summary>
        public class CircularDependencyInfo
        {
            public List<string> CircularPath { get; set; } = new List<string>();
            public string Description { get; set; }
        }

        /// <summary>
        /// 分析模块依赖关系
        /// </summary>
        /// <param name="modules">模块列表</param>
        /// <returns>依赖关系信息字典</returns>
        public static Dictionary<string, DependencyInfo> AnalyzeDependencies(IEnumerable<IModule> modules)
        {
            var dependencyInfos = new Dictionary<string, DependencyInfo>();
            var moduleTypes = new Dictionary<string, Type>();

            // 初始化依赖信息
            foreach (var module in modules)
            {
                var moduleType = module.GetType();
                moduleTypes[module.Name] = moduleType;
                
                dependencyInfos[module.Name] = new DependencyInfo
                {
                    ModuleName = module.Name,
                    Dependencies = new List<string>(),
                    Dependents = new List<string>()
                };
            }

            // 分析依赖关系
            foreach (var module in modules)
            {
                var moduleType = module.GetType();
                var dependsOnAttributes = moduleType.GetCustomAttributes(typeof(DependsOnAttribute), true);
                
                foreach (DependsOnAttribute dependsOn in dependsOnAttributes)
                {
                    var dependencyName = dependsOn.ModuleType.Name;
                    
                    // 添加依赖
                    if (dependencyInfos.ContainsKey(module.Name))
                    {
                        dependencyInfos[module.Name].Dependencies.Add(dependencyName);
                    }
                    
                    // 添加被依赖
                    if (dependencyInfos.ContainsKey(dependencyName))
                    {
                        dependencyInfos[dependencyName].Dependents.Add(module.Name);
                    }
                }
            }

            // 计算依赖深度
            foreach (var moduleName in dependencyInfos.Keys)
            {
                CalculateDependencyDepth(moduleName, dependencyInfos, new HashSet<string>());
            }

            // 检测循环依赖
            foreach (var moduleName in dependencyInfos.Keys)
            {
                var visited = new HashSet<string>();
                var path = new List<string>();
                dependencyInfos[moduleName].HasCircularDependency = 
                    DetectCircularDependency(moduleName, moduleName, dependencyInfos, visited, path);
            }

            return dependencyInfos;
        }

        /// <summary>
        /// 计算依赖深度
        /// </summary>
        private static int CalculateDependencyDepth(string moduleName, Dictionary<string, DependencyInfo> dependencyInfos, HashSet<string> visited)
        {
            if (visited.Contains(moduleName))
            {
                return 0; // 避免循环依赖导致的无限递归
            }

            if (!dependencyInfos.ContainsKey(moduleName))
            {
                return 0;
            }

            visited.Add(moduleName);

            var info = dependencyInfos[moduleName];
            if (info.Dependencies.Count == 0)
            {
                info.DependencyDepth = 0;
                return 0;
            }

            int maxDepth = 0;
            foreach (var dependency in info.Dependencies)
            {
                int depth = CalculateDependencyDepth(dependency, dependencyInfos, new HashSet<string>(visited)) + 1;
                maxDepth = Math.Max(maxDepth, depth);
            }

            info.DependencyDepth = maxDepth;
            return maxDepth;
        }

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        private static bool DetectCircularDependency(string startModule, string currentModule, 
            Dictionary<string, DependencyInfo> dependencyInfos, HashSet<string> visited, List<string> path)
        {
            if (!dependencyInfos.ContainsKey(currentModule))
            {
                return false;
            }

            if (path.Contains(currentModule))
            {
                return currentModule == startModule;
            }

            path.Add(currentModule);
            visited.Add(currentModule);

            var info = dependencyInfos[currentModule];
            foreach (var dependency in info.Dependencies)
            {
                if (DetectCircularDependency(startModule, dependency, dependencyInfos, visited, path))
                {
                    return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// 获取所有循环依赖
        /// </summary>
        /// <param name="dependencyInfos">依赖关系信息</param>
        /// <returns>循环依赖列表</returns>
        public static List<CircularDependencyInfo> GetCircularDependencies(Dictionary<string, DependencyInfo> dependencyInfos)
        {
            var circularDependencies = new List<CircularDependencyInfo>();
            var visited = new HashSet<string>();

            foreach (var moduleName in dependencyInfos.Keys)
            {
                if (visited.Contains(moduleName))
                {
                    continue;
                }

                var path = new List<string>();
                var circularPath = FindCircularPath(moduleName, moduleName, dependencyInfos, visited, path);
                
                if (circularPath != null && circularPath.Count > 0)
                {
                    circularDependencies.Add(new CircularDependencyInfo
                    {
                        CircularPath = circularPath,
                        Description = string.Join(" -> ", circularPath) + " -> " + circularPath[0]
                    });
                }
            }

            return circularDependencies;
        }

        /// <summary>
        /// 查找循环路径
        /// </summary>
        private static List<string> FindCircularPath(string startModule, string currentModule, 
            Dictionary<string, DependencyInfo> dependencyInfos, HashSet<string> visited, List<string> path)
        {
            if (!dependencyInfos.ContainsKey(currentModule))
            {
                return null;
            }

            if (path.Contains(currentModule))
            {
                if (currentModule == startModule)
                {
                    var circularPath = new List<string>();
                    int startIndex = path.IndexOf(currentModule);
                    for (int i = startIndex; i < path.Count; i++)
                    {
                        circularPath.Add(path[i]);
                    }
                    return circularPath;
                }
                return null;
            }

            path.Add(currentModule);
            visited.Add(currentModule);

            var info = dependencyInfos[currentModule];
            foreach (var dependency in info.Dependencies)
            {
                var result = FindCircularPath(startModule, dependency, dependencyInfos, visited, path);
                if (result != null)
                {
                    return result;
                }
            }

            path.RemoveAt(path.Count - 1);
            return null;
        }

        /// <summary>
        /// 获取模块的初始化顺序建议
        /// </summary>
        /// <param name="dependencyInfos">依赖关系信息</param>
        /// <returns>建议的初始化顺序</returns>
        public static List<string> GetInitializationOrder(Dictionary<string, DependencyInfo> dependencyInfos)
        {
            var order = new List<string>();
            var visited = new HashSet<string>();
            var tempVisited = new HashSet<string>();

            foreach (var moduleName in dependencyInfos.Keys)
            {
                if (!visited.Contains(moduleName))
                {
                    TopologicalSort(moduleName, dependencyInfos, visited, tempVisited, order);
                }
            }

            order.Reverse(); // 反转以获得正确的初始化顺序
            return order;
        }

        /// <summary>
        /// 拓扑排序
        /// </summary>
        private static void TopologicalSort(string moduleName, Dictionary<string, DependencyInfo> dependencyInfos, 
            HashSet<string> visited, HashSet<string> tempVisited, List<string> order)
        {
            if (tempVisited.Contains(moduleName))
            {
                // 检测到循环依赖
                return;
            }

            if (visited.Contains(moduleName))
            {
                return;
            }

            tempVisited.Add(moduleName);

            if (dependencyInfos.ContainsKey(moduleName))
            {
                var info = dependencyInfos[moduleName];
                foreach (var dependency in info.Dependencies)
                {
                    TopologicalSort(dependency, dependencyInfos, visited, tempVisited, order);
                }
            }

            tempVisited.Remove(moduleName);
            visited.Add(moduleName);
            order.Add(moduleName);
        }

        /// <summary>
        /// 验证依赖关系
        /// </summary>
        /// <param name="dependencyInfos">依赖关系信息</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateDependencies(Dictionary<string, DependencyInfo> dependencyInfos)
        {
            var result = new ValidationResult();
            var circularDependencies = GetCircularDependencies(dependencyInfos);

            if (circularDependencies.Count > 0)
            {
                result.IsValid = false;
                result.Errors.Add($"发现 {circularDependencies.Count} 个循环依赖:");
                foreach (var circular in circularDependencies)
                {
                    result.Errors.Add($"  - {circular.Description}");
                }
            }

            // 检查缺失的依赖
            foreach (var kvp in dependencyInfos)
            {
                var info = kvp.Value;
                foreach (var dependency in info.Dependencies)
                {
                    if (!dependencyInfos.ContainsKey(dependency))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"模块 '{info.ModuleName}' 依赖的模块 '{dependency}' 不存在");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }
    }
}

