using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework;
using TH7.UI;
using DG.Tweening;

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

        [Header("Camera Focus")]
        [SerializeField] Transform cameraFollowTarget;
        [SerializeField] float cameraFocusDuration = 0.3f;
        [SerializeField] Ease cameraFocusEase = Ease.OutQuad;
        [SerializeField] bool followHeroDuringMove = true;

        // 摄像头跟随状态
        Hero followingHero;

        [Header("UI")]
        [SerializeField] TownPanelUI townPanel;
        [SerializeField] ResourceBarUI resourceBar;
        [SerializeField] DayDisplayUI dayDisplayUI;
        [SerializeField] TurnPhaseUI turnPhaseUI;
        [SerializeField] SelectedHeroInfoUI selectedHeroInfoUI;

        [Header("World View")]
        [SerializeField] PathPreview pathPreview;
        [SerializeField] HeroSelectionIndicator selectionIndicator;
        [SerializeField] SelectableHeroIndicator selectableHeroIndicator;

        [Header("Config")]
        [SerializeField] TownConfigDatabase townConfigDatabase;
        [SerializeField] UnitConfigDatabase unitConfigDatabase;

        [Header("Prefabs")]
        [SerializeField] Hero heroPrefab;

        [Header("Map Objects")]
        [SerializeField] MapObjectSpawner mapObjectSpawner;

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
            InitializeMapObjects();
            InitializeTurnSystem();
            SetupUI();
            StartGame();
        }

        void Update()
        {
            // 持续跟随正在移动的英雄
            if (followingHero != null && cameraFollowTarget != null)
            {
                Vector3 heroPos = followingHero.transform.position;
                float z = cameraFollowTarget.position.z;
                cameraFollowTarget.position = new Vector3(heroPos.x, heroPos.y, z);
            }
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
            else
            {
                // 正常流程：使用起始英雄配置创建英雄
                CreateStartingHero();
            }

            // 创建 WorldContext 并注入 MapManager
            worldContext = sessionContext.CreateChild<WorldContext>();
            worldContext.Setup(mapManager);

            Debug.Log("[WorldScene] WorldContext created");
        }

        void InitializeMapObjects()
        {
            if (worldContext == null) return;

            // 随机生成资源堆
            if (mapObjectSpawner != null)
            {
                mapObjectSpawner.Initialize(mapManager);
            }

            // 收集场景中所有地图物件并注册到 WorldContext
            var allObjects = FindObjectsByType<MapObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                worldContext.RegisterMapObject(obj);
            }

            Debug.Log($"[WorldScene] Registered {allObjects.Length} map objects");
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

            // 设置路径预览和选择指示器
            if (pathPreview != null)
                playerProvider.SetPathPreview(pathPreview);
            if (selectionIndicator != null)
                playerProvider.SetSelectionIndicator(selectionIndicator);

            // 事件通过 EventSystem 自动订阅（使用 [AutoSubscribe] 特性）

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

            // 绑定天数显示 UI
            if (dayDisplayUI != null && sessionContext != null)
                dayDisplayUI.Bind(sessionContext);

            // 绑定回合阶段 UI
            if (turnPhaseUI != null && turnManager != null)
                turnPhaseUI.Bind(turnManager);

            // 绑定选中英雄信息 UI
            if (selectedHeroInfoUI != null && playerProvider != null)
                selectedHeroInfoUI.Bind(playerProvider);

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

            // 聚焦到第一个英雄
            FocusOnFirstHero();

            // 开始第一天
            turnManager.StartDay();

            Debug.Log("[WorldScene] Game started");
        }

        void FocusOnFirstHero()
        {
            var heroes = sessionContext?.GetHeroesForPlayer(0);
            if (heroes == null || heroes.Count == 0) return;

            // 立即聚焦（不使用动画）
            FocusOnHero(heroes[0], immediate: true);
        }

        /// <summary>
        /// 聚焦摄像头到指定英雄
        /// </summary>
        /// <param name="hero">目标英雄</param>
        /// <param name="immediate">是否立即移动（无动画）</param>
        void FocusOnHero(Hero hero, bool immediate = true)
        {
            if (hero == null) return;

            // 获取目标位置
            Vector3 targetPos = mapManager != null
                ? mapManager.CellToWorld(hero.CellPosition.Value)
                : hero.transform.position;

            // 使用 Cinemachine Follow Target（如果有），否则直接移动相机
            Transform moveTarget = cameraFollowTarget != null
                ? cameraFollowTarget
                : worldCamera?.transform;

            if (moveTarget == null)
            {
                Debug.LogWarning("[WorldScene] FocusOnHero: moveTarget is null");
                return;
            }

            // 保持 Z 轴不变
            float z = moveTarget.position.z;
            Vector3 destination = new Vector3(targetPos.x, targetPos.y, z);

            Debug.Log($"[WorldScene] FocusOnHero: {hero.HeroName} -> {destination} (immediate={immediate})");

            // 停止之前的 DOTween 动画
            moveTarget.DOKill();

            if (immediate)
            {
                moveTarget.position = destination;
            }
            else
            {
                moveTarget.DOMove(destination, cameraFocusDuration)
                    .SetEase(cameraFocusEase);
            }
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
            StartCoroutine(EnterBattle(e.Hero, e.Enemy));
        }

        /// <summary>
        /// 进入战斗（Additive 场景加载）
        /// </summary>
        IEnumerator EnterBattle(Hero attackerHero, object enemy)
        {
            Debug.Log($"[WorldScene] Entering battle with {attackerHero?.HeroName}");

            // 1. 禁用玩家输入
            playerProvider?.SetEnabled(false);

            // 2. 暂停世界上下文
            worldContext?.Pause();

            // 3. 准备战斗数据
            var battleInitData = new BattleInitData
            {
                AttackerHero = attackerHero,
                AttackerArmy = attackerHero?.Army?.Where(s => s != null && s.Count > 0).ToArray(),
                DefenderArmy = GetEnemyArmy(enemy),
                MapWidth = 15,
                MapHeight = 11,
                Terrain = BattleTerrainType.Grass
            };

            // 4. 创建战斗上下文
            var battleContext = sessionContext.CreateChild<BattleContext>(ctx => ctx.Setup(battleInitData));

            // 5. Additive 加载战斗场景
            var asyncOp = SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            Debug.Log("[WorldScene] Battle scene loaded (Additive)");

            // 战斗场景会自动从 BattleContext 获取数据并开始战斗
            // 战斗结束后 BattleSceneController 会销毁 BattleContext 并卸载场景
            // BattleContext.OnDispose() 会自动恢复 WorldContext
        }

        /// <summary>
        /// 获取敌方军队配置
        /// </summary>
        UnitStack[] GetEnemyArmy(object enemy)
        {
            // 如果是英雄，使用英雄的军队
            if (enemy is Hero enemyHero)
            {
                return enemyHero.Army?.Where(s => s != null && s.Count > 0).ToArray();
            }

            // 如果是地图对象（如野怪），获取其军队配置
            // TODO: 实现野怪军队配置

            // 默认测试军队
            return new UnitStack[]
            {
                new UnitStack { UnitId = "skeleton", Count = 10 },
                new UnitStack { UnitId = "zombie", Count = 5 }
            };
        }

        /// <summary>
        /// 战斗结束后的处理（由 BattleContext.OnDispose 自动触发 WorldContext.Resume）
        /// </summary>
        [AutoSubscribe]
        void OnBattleEnded(BattleEndedEvent e)
        {
            Debug.Log($"[WorldScene] Battle ended with result: {e.Result}");

            // 恢复输入
            inputController?.EnableInput();
            playerProvider?.SetEnabled(true);

            // 恢复回合（如果需要）
            turnManager?.Resume();

            // 可以在这里处理战斗结果
            if (e.Result == BattleResult.Victory)
            {
                // 胜利处理：经验、战利品等
                Debug.Log($"[WorldScene] Victory! Gained {e.Rewards?.Experience ?? 0} experience");
            }
            else if (e.Result == BattleResult.Defeat)
            {
                // 失败处理
                Debug.Log("[WorldScene] Defeat!");
            }
        }

        [AutoSubscribe]
        void OnDayEnded(DayEndedEvent e)
        {
            Debug.Log($"[WorldScene] Day {e.Day} ended (Week {e.Week}, Month {e.Month})");
            // 可以在这里添加日结算 UI
        }

        [AutoSubscribe]
        void OnActionStarted(ActionStartedEvent e)
        {
            // 移动行动开始时，开始跟随英雄
            if (followHeroDuringMove && e.Action is MoveAction moveAction)
            {
                followingHero = moveAction.Hero;
                Debug.Log($"[WorldScene] 开始跟随英雄: {followingHero.HeroName}");
            }
        }

        [AutoSubscribe]
        void OnActionCompleted(ActionCompletedEvent e)
        {
            // 行动完成时停止跟随
            if (followingHero != null)
            {
                Debug.Log($"[WorldScene] 停止跟随英雄: {followingHero.HeroName}");
                followingHero = null;
            }
        }

        /// <summary>
        /// 可选择英雄列表变化时更新高亮
        /// </summary>
        [AutoSubscribe]
        void OnSelectableHeroesChanged(SelectableHeroesChangedEvent e)
        {
            selectableHeroIndicator?.UpdateSelectableHeroes(e.Heroes);
        }

        /// <summary>
        /// 玩家选中英雄时
        /// </summary>
        [AutoSubscribe]
        void OnHeroSelected(HeroSelectedEvent e)
        {
            // 摄像头聚焦到选中的英雄
            if (e.Hero != null)
                FocusOnHero(e.Hero);
        }

        /// <summary>
        /// 玩家取消选中英雄时
        /// </summary>
        [AutoSubscribe]
        void OnHeroDeselected(HeroDeselectedEvent e)
        {
            // 可以在这里处理取消选中后的逻辑
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
        /// 使用起始英雄配置创建英雄
        /// </summary>
        void CreateStartingHero()
        {
            Debug.Log("[WorldScene] CreateStartingHero called");

            var heroConfig = sessionContext.GetStartingHeroConfig();
            if (heroConfig == null)
            {
                Debug.LogError("[WorldScene] No starting hero config found! Check if NewGameWindow passed the hero correctly.");
                return;
            }

            Debug.Log($"[WorldScene] Creating hero from config: {heroConfig.HeroId}, {heroConfig.DisplayName}");

            // 起始位置（可以从地图配置中获取，这里先用默认值）
            Vector3Int startPosition = new Vector3Int(5, 5, 0);

            // 优先使用 HeroConfig 中的 Prefab，如果没有则使用默认的 heroPrefab
            var prefabToUse = heroConfig.Prefab != null ? heroConfig.Prefab : (heroPrefab != null ? heroPrefab.gameObject : null);

            Hero hero;
            if (prefabToUse != null)
            {
                var go = Instantiate(prefabToUse);
                hero = go.GetComponent<Hero>();
                if (hero == null)
                {
                    hero = go.AddComponent<Hero>();
                }
            }
            else
            {
                var go = new GameObject($"Hero_{heroConfig.DisplayName}");
                hero = go.AddComponent<Hero>();
            }

            // 使用 HeroConfig 初始化
            hero.InitializeFromConfig(heroConfig, startPosition, ownerId: 0);
            sessionContext.RegisterHero(hero);

            // 设置坐标转换器
            if (mapManager != null)
            {
                hero.SetPositionConverter(cell => mapManager.CellToWorld(cell));
            }

            Debug.Log($"[WorldScene] Created starting hero: {heroConfig.DisplayName}");
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

        /// <summary>
        /// 结束回合（供 UI 按钮绑定）
        /// </summary>
        public void EndTurn()
        {
            playerProvider?.RequestEndTurn();
        }

        protected override void OnDestroy()
        {
            // 清理 PlayerProvider
            playerProvider?.Dispose();

            // 清理可选英雄指示器
            selectableHeroIndicator?.HideAll();

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
