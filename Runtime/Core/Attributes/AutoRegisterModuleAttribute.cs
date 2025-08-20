using System;

namespace CnoomFramework.Core.Attributes
{
    
    /// <summary>
    /// RegisterModule 是一个用于标记类的属性，指示该类应该被注册为模块。这通常用于框架或应用程序中需要自动发现和注册组件的场景。
    /// 通过将此属性应用于某个类，可以告知系统在启动时自动加载并初始化这个类作为可用模块之一。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterModuleAttribute : Attribute
    {
        public Type InterfaceType;
    }
}