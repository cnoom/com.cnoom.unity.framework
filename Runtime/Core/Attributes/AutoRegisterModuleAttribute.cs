using System;

namespace CnoomFramework.Core.Attributes
{
    /// <summary>
    /// 该属性用于标记需要自动注册到框架管理器中的模块类。通过此属性，可以指定模块实现的接口类型及其加载优先级。
    ///  优先级越小越先加载
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterModuleAttribute : Attribute
    {
        public Type InterfaceType;
        public int Priority = 0;
    }
}