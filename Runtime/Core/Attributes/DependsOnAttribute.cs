using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    ///     模块依赖特性，用于声明模块依赖关系
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        /// <summary>
        ///     构造函数
        /// </summary>
        /// <param name="moduleType">依赖的模块类型</param>
        public DependsOnAttribute(Type moduleType)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
        }

        /// <summary>
        ///     依赖的模块类型
        /// </summary>
        public Type ModuleType { get; }
    }
}