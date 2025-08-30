using UnityEngine;

namespace CnoomFramework.Core.Contracts
{
    /// <summary>
    /// 契约验证设置 - 允许开发者在编辑器中配置验证行为
    /// </summary>
    [CreateAssetMenu(fileName = "ContractValidationSettings", menuName = "CnoomFramework/Contract Validation Settings")]
    public class ContractValidationSettings : ScriptableObject
    {
        [Header("基础设置")]
        [Tooltip("是否启用契约验证功能")]
        public bool enableContractValidation = false;
        
        [Tooltip("验证失败时是否记录警告")]
        public bool logWarningsOnFailure = true;
        
        [Tooltip("验证失败时是否抛出异常")]
        public bool throwOnValidationFailure = false;

        [Header("性能优化")]
        [Tooltip("在发布版本中禁用所有验证")]
        public bool disableInReleaseBuilds = true;
        
        [Tooltip("在编辑器中也禁用验证以提升性能")]
        public bool disableInEditor = false;

        private static ContractValidationSettings _instance;
        
        /// <summary>
        /// 获取或创建默认设置
        /// </summary>
        public static ContractValidationSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = CreateInstance<ContractValidationSettings>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 检查当前是否应该进行验证
        /// </summary>
        public bool ShouldValidate()
        {
            if (!enableContractValidation) return false;
            
#if !UNITY_EDITOR
            if (disableInReleaseBuilds) return false;
#else
            if (disableInEditor) return false;
#endif

            return true;
        }
    }
}