using UnityEngine;
using GameFramework;
using TH7.UI;

namespace TH7
{
    // 世界场景控制器，负责创建 WorldContext 并管理城镇/战斗界面
    public class WorldSceneController : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] MapManager mapManager;

        [Header("UI")]
        [SerializeField] TownPanelUI townPanel;
        [SerializeField] ResourceBarUI resourceBar;

        [Header("Config")]
        [SerializeField] TownConfigDatabase townConfigDatabase;
        [SerializeField] UnitConfigDatabase unitConfigDatabase;

        SessionContext sessionContext;
        WorldContext worldContext;
        TownContext townContext;

        void Start()
        {
            InitializeContext();
            SetupUI();
        }

        void InitializeContext()
        {
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            if (contextSystem == null)
            {
                Debug.LogError("[WorldScene] ContextSystem not found");
                return;
            }

            sessionContext = contextSystem.Root.GetChild<SessionContext>();
            if (sessionContext == null)
            {
                Debug.LogError("[WorldScene] SessionContext not found");
                return;
            }

            // 创建 WorldContext 并注入 MapManager
            worldContext = sessionContext.CreateChild<WorldContext>();
            worldContext.Setup(mapManager);

            Debug.Log("[WorldScene] WorldContext created");
        }

        void SetupUI()
        {
            // 初始隐藏城镇面板
            if (townPanel != null)
            {
                townPanel.gameObject.SetActive(false);
                townPanel.OnExitRequested = CloseTown;
            }
        }

        // 打开城镇界面（由地图点击事件调用）
        public void OpenTown(TownData townData)
        {
            if (townData == null || townPanel == null) return;

            // 暂停探索
            worldContext?.Pause();

            // 创建城镇上下文
            townContext = sessionContext.CreateChild<TownContext>();
            townContext.Setup(townData, townConfigDatabase);

            // 显示城镇面板
            townPanel.gameObject.SetActive(true);
            townPanel.Bind(townContext);

            Debug.Log($"[WorldScene] Opened town: {townData.TownName}");
        }

        // 关闭城镇界面
        void CloseTown()
        {
            // 隐藏城镇面板
            if (townPanel != null)
                townPanel.gameObject.SetActive(false);

            // 销毁城镇上下文
            if (townContext != null)
            {
                sessionContext?.DisposeChild<TownContext>();
                townContext = null;
            }

            // 保存存档
            sessionContext?.SaveSession();

            // 恢复探索
            worldContext?.Resume();

            Debug.Log("[WorldScene] Closed town, resumed exploration");
        }

        // 打开城镇（通过索引，调试用）
        public void OpenTownByIndex(int index)
        {
            if (sessionContext?.Towns == null || index < 0 || index >= sessionContext.Towns.Count)
            {
                Debug.LogWarning($"[WorldScene] Invalid town index: {index}");
                return;
            }
            OpenTown(sessionContext.Towns[index]);
        }

        void OnDestroy()
        {
            // 清理城镇上下文
            if (townContext != null)
                sessionContext?.DisposeChild<TownContext>();

            // 清理世界上下文
            if (worldContext != null)
                sessionContext?.DisposeChild<WorldContext>();
        }
    }
}
