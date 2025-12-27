using UnityEngine;
using GameFramework;

namespace TH7
{
    // 调试用入口，用于直接运行非 Boot 场景时初始化系统
    // 正式流程应从 BootScene 启动
    public class GameSystem : MonoBehaviour
    {
        [SerializeField] bool autoInit = true;

        void Awake()
        {
            // 如果系统已初始化则跳过（从 Boot 场景启动的情况）
            if (GameEntry.Instance.GetSystem<ContextSystem>() != null)
                return;

            if (autoInit)
                InitializeSystems();
        }

        void InitializeSystems()
        {
            // GameEntry.Instance 会自动初始化核心系统
            _ = GameEntry.Instance;
            Debug.Log("[GameSystem] 调试模式：系统初始化完成");
        }
    }
}
