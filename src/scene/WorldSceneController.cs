using UnityEngine;
using GameFramework;
using TH7.UI;

namespace TH7
{
    /// <summary>
    /// 世界场景控制器
    /// 负责创建 WorldContext、管理回合系统、处理城镇/战斗界面
    /// </summary>
    public class WorldSceneController : GameBehaviour
    {
        [Header("Turn Manager")]
        [SerializeField] WorldTurnManager turnManager;

        [Header("Map")]
        [SerializeField] MapManager mapManager;

        [Header("Input")]
        [SerializeField] WorldInputController inputController;
        [SerializeField] Camera worldCamera;

        [Header("UI")]
        [SerializeField] TownPanelUI townPanel;
        [SerializeField] ResourceBarUI resourceBar;

        [Header("Config")]
        [SerializeField] TownConfigDatabase townConfigDatabase;
        [SerializeField] UnitConfigDatabase unitConfigDatabase;

        [Header("Prefabs")]
        [SerializeField] Hero heroPrefab;

        // 上下文
        SessionContext sessionContext;
        WorldContext worldContext;
        TownContext townContext;

        // 回合系统
        ActionExecutor actionExecutor;
        PlayerActionProvider playerProvider;
        IPathfinder pathfinder;

        protected override void Start()
        {
            base.Start();
            InitializeContext();
            InitializeTurnSystem();
            SetupUI();
            StartGame();
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

            // 开发模式：如果没有 SessionContext，自动创建测试会话
            if (sessionContext == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[WorldScene] SessionContext not found, creating dev session...");
                sessionContext = contextSystem.Root.CreateChild<SessionContext>();
                sessionContext.StartNewSession("Dev Player");

                // 添加测试城镇
                var testTown = new TownData
                {
                    TownId = "town_01",
                    TownName = "Test Town",
                    Faction = BiomeType.Greek,
                    MapPosition = new Vector2Int(10, 10)
                };
                sessionContext.Towns.Add(testTown);

                // 添加测试英雄（动态创建 Hero GameObject）
                CreateTestHero("hero_01", "Test Hero", new Vector3Int(5, 5, 0), 0);
#else
                Debug.LogError("[WorldScene] SessionContext not found");
                return;
#endif
            }

            // 创建 WorldContext 并注入 MapManager
            worldContext = sessionContext.CreateChild<WorldContext>();
            worldContext.Setup(mapManager);

            Debug.Log("[WorldScene] WorldContext created");
        }

        void InitializeTurnSystem()
        {
            if (worldContext == null) return;

            // 创建寻路器
            pathfinder = new SimplePathfinder();

            // 创建行动执行器
            actionExecutor = new ActionExecutor(worldContext, this);

            // 创建玩家行动提供者
            var camera = worldCamera != null ? worldCamera : Camera.main;
            playerProvider = new PlayerActionProvider(mapManager, camera, pathfinder);

            // 初始化回合管理器（现在是 GameBehaviour，通过 SerializeField 引用）
            if (turnManager != null)
            {
                turnManager.Initialize(worldContext, actionExecutor);
                turnManager.RegisterProvider(0, playerProvider); // 玩家 0
            }

            // 绑定输入控制器
            if (inputController != null)
            {
                inputController.BindActionProvider(playerProvider);
            }

            Debug.Log("[WorldScene] Turn system initialized");
        }

        void SetupUI()
        {
            // 绑定资源条到 Session 资源
            if (resourceBar != null && sessionContext != null)
                resourceBar.BindToResources(sessionContext.Resources);

            // 初始隐藏城镇面板
            if (townPanel != null)
            {
                townPanel.gameObject.SetActive(false);
                townPanel.OnExitRequested = CloseTown;
            }
        }

        void StartGame()
        {
            if (turnManager == null) return;

            // 启用输入
            inputController?.EnableInput();
            playerProvider?.SetEnabled(true);

            // 开始第一天
            turnManager.StartDay();

            Debug.Log("[WorldScene] Game started");
        }

        // ============================================
        // EventSystem 事件处理
        // ============================================

        [AutoSubscribe]
        void OnEnterTownRequested(EnterTownRequestedEvent e)
        {
            inputController?.DisableInput();
            OpenTown(e.Town);
        }

        [AutoSubscribe]
        void OnBattleRequested(BattleRequestedEvent e)
        {
            inputController?.DisableInput();
            // TODO: 打开战斗界面
            Debug.Log("[WorldScene] Battle triggered (not implemented)");
        }

        [AutoSubscribe]
        void OnDayEnded(DayEndedEvent e)
        {
            Debug.Log($"[WorldScene] Day {e.Day} ended (Week {e.Week}, Month {e.Month})");
            // 可以在这里添加日结算 UI
        }

        [AutoSubscribe]
        void OnActionCompleted(ActionCompletedEvent e)
        {
            Debug.Log($"[WorldScene] Action completed: {e.Action.Type} -> {e.Result.Type}");
        }

        /// <summary>
        /// 打开城镇界面
        /// </summary>
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

        /// <summary>
        /// 关闭城镇界面
        /// </summary>
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

            // 恢复输入和回合
            inputController?.EnableInput();
            playerProvider?.SetEnabled(true);
            turnManager?.Resume();

            Debug.Log("[WorldScene] Closed town, resumed exploration");
        }

        /// <summary>
        /// 创建测试英雄（开发用）
        /// </summary>
        Hero CreateTestHero(string id, string name, Vector3Int position, int ownerId)
        {
            Hero hero;
            if (heroPrefab != null)
            {
                hero = Instantiate(heroPrefab);
            }
            else
            {
                // 没有预制体时创建空 GameObject
                var go = new GameObject($"Hero_{name}");
                hero = go.AddComponent<Hero>();
            }

            hero.Initialize(id, name, position, ownerId);
            sessionContext.RegisterHero(hero);

            // 设置坐标转换器
            if (mapManager != null)
            {
                hero.SetPositionConverter(cell => mapManager.CellToWorld(cell));
            }

            return hero;
        }

        /// <summary>
        /// 打开城镇（通过索引，调试用）
        /// </summary>
        public void OpenTownByIndex(int index)
        {
            if (sessionContext?.Towns == null || index < 0 || index >= sessionContext.Towns.Count)
            {
                Debug.LogWarning($"[WorldScene] Invalid town index: {index}");
                return;
            }
            OpenTown(sessionContext.Towns[index]);
        }

        protected override void OnDestroy()
        {
            // 清理 Provider
            playerProvider?.Dispose();

            // 清理城镇上下文
            if (townContext != null)
                sessionContext?.DisposeChild<TownContext>();

            // 清理世界上下文
            if (worldContext != null)
                sessionContext?.DisposeChild<WorldContext>();

            base.OnDestroy();
        }
    }
}
