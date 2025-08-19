using System.Collections.Generic;

namespace CnoomFramework.Core.Mock
{
    /// <summary>
    ///     支持状态导入导出的模块接口
    /// </summary>
    public interface IStatefulModule : IModule
    {
        /// <summary>
        ///     导出模块状态
        /// </summary>
        /// <returns>包含模块状态的字典</returns>
        Dictionary<string, object> ExportState();

        /// <summary>
        ///     导入模块状态
        /// </summary>
        /// <param name="state">包含模块状态的字典</param>
        void ImportState(Dictionary<string, object> state);
    }
}