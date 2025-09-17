using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CnoomFramework.Core;
using CnoomFramework.Core.Attributes;
using CommandHandlerAttribute = CnoomFramework.Core.Attributes.CommandHandlerAttribute;

namespace CnoomFramework.Editor
{
    /// <summary>
    /// Cnoom Framework 编辑器工具类
    /// </summary>
    public static class FrameworkEditorUtilities
    {
        /// <summary>
        /// 获取所有可用的模块类型
        /// </summary>
        public static List<Type> GetAllModuleTypes()
        {
            var moduleTypes = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => typeof(IModule).IsAssignableFrom(t) &&
                                   !t.IsInterface &&
                                   !t.IsAbstract)
                        .ToList();
                    
                    moduleTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            return moduleTypes.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// 获取模块的依赖关系
        /// </summary>
        public static List<Type> GetModuleDependencies(Type moduleType)
        {
            var dependencies = new List<Type>();
            
            var dependsOnAttributes = moduleType.GetCustomAttributes<DependsOnAttribute>();
            foreach (var attr in dependsOnAttributes)
            {
                dependencies.Add(attr.ModuleType);
            }
            
            return dependencies;
        }

        /// <summary>
        /// 检查模块是否有自动注册属性
        /// </summary>
        public static bool HasAutoRegisterAttribute(Type moduleType)
        {
            return moduleType.GetCustomAttribute<AutoRegisterModuleAttribute>() != null;
        }

        /// <summary>
        /// 获取模块的自动注册属性
        /// </summary>
        public static AutoRegisterModuleAttribute GetAutoRegisterAttribute(Type moduleType)
        {
            return moduleType.GetCustomAttribute<AutoRegisterModuleAttribute>();
        }

        /// <summary>
        /// 获取模块的事件处理器方法
        /// </summary>
        public static List<MethodInfo> GetEventHandlerMethods(Type moduleType)
        {
            var methods = new List<MethodInfo>();
            
            var allMethods = moduleType.GetMethods(
                BindingFlags.Instance | 
                BindingFlags.Public | 
                BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);
            
            foreach (var method in allMethods)
            {
                if (method.GetCustomAttribute<BroadcastHandlerAttribute>() != null ||
                    method.GetCustomAttribute<CommandHandlerAttribute>() != null ||
                    method.GetCustomAttribute<QueryHandlerAttribute>() != null)
                {
                    methods.Add(method);
                }
            }
            
            return methods;
        }

        /// <summary>
        /// 验证模块类型的有效性
        /// </summary>
        public static ValidationResult ValidateModuleType(Type moduleType)
        {
            var result = new ValidationResult();
            
            // 检查是否实现了IModule接口
            if (!typeof(IModule).IsAssignableFrom(moduleType))
            {
                result.AddError($"Type {moduleType.Name} does not implement IModule interface");
                return result;
            }
            
            // 检查是否是抽象类或接口
            if (moduleType.IsAbstract || moduleType.IsInterface)
            {
                result.AddError($"Type {moduleType.Name} cannot be abstract or interface");
                return result;
            }
            
            // 检查是否有无参构造函数
            var constructor = moduleType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                result.AddError($"Type {moduleType.Name} must have a parameterless constructor");
            }
            
            // 检查依赖关系
            var dependencies = GetModuleDependencies(moduleType);
            foreach (var dependency in dependencies)
            {
                if (!typeof(IModule).IsAssignableFrom(dependency))
                {
                    result.AddWarning($"Dependency {dependency.Name} does not implement IModule interface");
                }
            }
            
            // 检查事件处理器方法
            var eventHandlers = GetEventHandlerMethods(moduleType);
            foreach (var handler in eventHandlers)
            {
                var broadcastAttr = handler.GetCustomAttribute<BroadcastHandlerAttribute>();
                var commandAttr = handler.GetCustomAttribute<CommandHandlerAttribute>();
                var queryAttr = handler.GetCustomAttribute<QueryHandlerAttribute>();
                
                if (broadcastAttr != null || commandAttr != null)
                {
                    // 检查广播和命令处理器
                    var parameters = handler.GetParameters();
                    if (parameters.Length != 1)
                    {
                        result.AddError($"Event handler {handler.Name} must have exactly one parameter");
                    }
                    
                    if (handler.ReturnType != typeof(void))
                    {
                        result.AddError($"Event handler {handler.Name} must return void");
                    }
                }
                else if (queryAttr != null)
                {
                    // 检查查询处理器
                    var parameters = handler.GetParameters();
                    if (parameters.Length != 1)
                    {
                        result.AddError($"Query handler {handler.Name} must have exactly one parameter");
                    }
                    
                    if (handler.ReturnType == typeof(void))
                    {
                        result.AddError($"Query handler {handler.Name} must have a return value");
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 创建模块依赖关系图
        /// </summary>
        public static Dictionary<Type, List<Type>> CreateDependencyGraph(List<Type> moduleTypes)
        {
            var graph = new Dictionary<Type, List<Type>>();
            
            foreach (var moduleType in moduleTypes)
            {
                graph[moduleType] = GetModuleDependencies(moduleType);
            }
            
            return graph;
        }

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        public static List<List<Type>> DetectCircularDependencies(Dictionary<Type, List<Type>> dependencyGraph)
        {
            var circularDependencies = new List<List<Type>>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            var path = new List<Type>();
            
            foreach (var moduleType in dependencyGraph.Keys)
            {
                if (!visited.Contains(moduleType))
                {
                    DetectCircularDependenciesRecursive(moduleType, dependencyGraph, visited, visiting, path, circularDependencies);
                }
            }
            
            return circularDependencies;
        }

        private static void DetectCircularDependenciesRecursive(
            Type current,
            Dictionary<Type, List<Type>> graph,
            HashSet<Type> visited,
            HashSet<Type> visiting,
            List<Type> path,
            List<List<Type>> circularDependencies)
        {
            if (visiting.Contains(current))
            {
                // 找到循环依赖
                var cycleStart = path.IndexOf(current);
                var cycle = path.Skip(cycleStart).Concat(new[] { current }).ToList();
                circularDependencies.Add(cycle);
                return;
            }
            
            if (visited.Contains(current))
                return;
            
            visiting.Add(current);
            path.Add(current);
            
            if (graph.ContainsKey(current))
            {
                foreach (var dependency in graph[current])
                {
                    DetectCircularDependenciesRecursive(dependency, graph, visited, visiting, path, circularDependencies);
                }
            }
            
            visiting.Remove(current);
            path.RemoveAt(path.Count - 1);
            visited.Add(current);
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F2} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// 获取友好的类型名称
        /// </summary>
        public static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var genericTypeName = type.Name.Substring(0, type.Name.IndexOf('`'));
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericTypeName}<{genericArgs}>";
            }
            
            return type.Name;
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        
        public bool IsValid => Errors.Count == 0;
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}