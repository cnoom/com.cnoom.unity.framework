namespace CnoomFramework.Core.EventBuss.Contracts
{
    /// <summary>
    /// 模块命令接口 - 用于模块间的单向命令通信
    /// </summary>
    public interface IModuleCommand
    {
        // 标记接口，用于标识模块命令
    }
    
    /// <summary>
    /// 模块查询接口 - 用于模块间的数据查询通信
    /// </summary>
    /// <typeparam name="TResponse">响应数据类型</typeparam>
    public interface IModuleQuery<TResponse>
    {
        // 标记接口，用于标识模块查询
    }
}