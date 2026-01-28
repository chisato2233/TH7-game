using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗场景控制器
    /// 纯代码驱动，无需在编辑器中设置任何对象
    /// 场景中只需要一个挂载此脚本的空 GameObject
    /// </summary>
    public class BattleSceneController : GameBehaviour
    {
        // ============================================
        // 配置（可通过 Resources 或 SessionContext 获取）
        // ============================================

        [Header("Config (Optional - will load from Resources if null)")]
        [SerializeField] UnitConfigDatabase unitConfigDatabase;
        [SerializeField] GameObject unitPrefab;
        [SerializeField] GameObject hudPrefab;
        [SerializeField] TileBase grassTile;
        [SerializeField] TileBase obstacleTile;

        // ============================================
        // 运行时创建的对象
        // ============================================

        CinemachineCamera battleVirtualCamera;
        Camera battleCamera; // 仅用于开发模式（直接打开 BattleScene 时）
        Grid battleGrid;
        UnityEngine.Tilemaps.Tilemap battleTilemap;
        Transform unitContainer;
        BattleTurnManager turnManager;
        BattleInputController inputController;
        BattleHUD battleHUD;

        // 上下文
        SessionContext sessionContext;
        BattleContext battleContext;

        // 系统
        BattleActionExecutor actionExecutor;
        BattleActionProvider playerProvider;
        BattleAIProvider aiProvider;
        BattleUnitFactory unitFactory;

        // 单位实例
        readonly List<BattleUnit> allUnits = new();

        protected override void Start()
        {
            base.Start();

            LoadConfig();
            InitializeContext();

            if (battleContext != null)
            {
                CreateSceneObjects();
                InitializeBattle();
                StartBattle();
            }
        }

        /// <summary>
        /// 加载配置（从 Resources 或其他来源）
        /// </summary>
        void LoadConfig()
        {
            // 如果没有在 Inspector 设置，尝试从 Resources 加载
            if (unitConfigDatabase == null)
            {
                unitConfigDatabase = Resources.Load<UnitConfigDatabase>("Database/UnitConfigDatabase");
            }

            if (unitPrefab == null)
            {
                unitPrefab = Resources.Load<GameObject>("Prefabs/BattleUnit");
            }

            if (hudPrefab == null)
            {
                hudPrefab = Resources.Load<GameObject>("Prefabs/BattleHUD");
            }

            // 瓦片可以从 Resources 加载或使用纯色 Sprite
            if (grassTile == null)
            {
                grassTile = Resources.Load<TileBase>("Tiles/GrassTile");
            }
        }

        void InitializeContext()
        {
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            sessionContext = contextSystem?.Root?.GetChild<SessionContext>();
            battleContext = sessionContext?.GetChild<BattleContext>();

            if (battleContext == null)
            {
                Debug.LogError("[BattleScene] BattleContext not found! Make sure it's created before loading this scene.");

#if UNITY_EDITOR
                // 开发模式：创建测试战斗
                CreateTestBattle();
#endif
                return;
            }

            Debug.Log("[BattleScene] Context initialized");
        }

#if UNITY_EDITOR
        void CreateTestBattle()
        {
            Debug.LogWarning("[BattleScene] Creating test battle for development");

            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            if (contextSystem == null)
            {
                Debug.LogError("[BattleScene] ContextSystem not found");
                return;
            }

            // 创建测试 Session
            if (sessionContext == null)
            {
                sessionContext = contextSystem.Root.CreateChild<SessionContext>();
                sessionContext.StartNewSession("Dev Player");
            }

            // 创建测试战斗数据
            var initData = new BattleInitData
            {
                MapWidth = 15,
                MapHeight = 11,
                Terrain = BattleTerrainType.Grass,
                AttackerArmy = CreateTestArmy("pikeman", 10, "archer", 5),
                DefenderArmy = CreateTestArmy("skeleton", 15, "zombie", 8)
            };

            battleContext = sessionContext.CreateChild<BattleContext>(ctx => ctx.Setup(initData));
        }

        UnitStack[] CreateTestArmy(string unit1, int count1, string unit2, int count2)
        {
            return new UnitStack[]
            {
                new UnitStack { UnitId = unit1, Count = count1 },
                new UnitStack { UnitId = unit2, Count = count2 }
            };
        }
#endif

        /// <summary>
        /// 动态创建场景所需的所有对象
        /// </summary>
        void CreateSceneObjects()
        {
            // 1. 创建相机
            CreateCamera();

            // 2. 创建战斗网格和瓦片地图
            CreateBattleMap();

            // 3. 创建单位容器
            CreateUnitContainer();

            // 4. 创建回合管理器
            CreateTurnManager();

            // 5. 创建输入控制器
            CreateInputController();

            // 6. 创建 HUD
            CreateHUD();

            Debug.Log("[BattleScene] All scene objects created");
        }

        void CreateCamera()
        {
            int mapWidth = battleContext?.InitData?.MapWidth ?? 15;
            int mapHeight = battleContext?.InitData?.MapHeight ?? 11;
            Vector3 centerPosition = new Vector3(mapWidth / 2f, mapHeight / 2f, 0);

            // 检查是否已有 CinemachineBrain（从 World 场景 Additive 加载时会有）
            var existingBrain = FindFirstObjectByType<CinemachineBrain>();

            if (existingBrain != null)
            {
                // 正常流程：从 World 进入 Battle
                // 只创建 CinemachineCamera，由 World 的 CinemachineBrain 自动接管
                CreateBattleVirtualCamera(centerPosition);
                battleCamera = existingBrain.GetComponent<Camera>();
            }
            else
            {
                // 开发模式：直接打开 BattleScene
                // 需要创建完整的相机系统
                CreateDevModeCamera(centerPosition);
            }

            Debug.Log("[BattleScene] Camera created");
        }

        /// <summary>
        /// 创建战斗虚拟相机（正常流程使用）
        /// </summary>
        void CreateBattleVirtualCamera(Vector3 position)
        {
            var vcamGo = new GameObject("BattleVirtualCamera");
            vcamGo.transform.position = new Vector3(position.x, position.y, -10f);

            battleVirtualCamera = vcamGo.AddComponent<CinemachineCamera>();
            battleVirtualCamera.Lens.OrthographicSize = 6f;
            battleVirtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;

            // 设置高优先级，自动接管 CinemachineBrain
            // World 虚拟相机默认优先级是 0-10，我们用 100 确保接管
            battleVirtualCamera.Priority = 100;

            Debug.Log("[BattleScene] CinemachineCamera created with priority 100");
        }

        /// <summary>
        /// 创建开发模式相机（直接打开 BattleScene 时使用）
        /// </summary>
        void CreateDevModeCamera(Vector3 position)
        {
            var cameraGo = new GameObject("BattleCamera_DevMode");
            cameraGo.tag = "MainCamera";

            battleCamera = cameraGo.AddComponent<Camera>();
            battleCamera.orthographic = true;
            battleCamera.orthographicSize = 6f;
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = new Color(0.1f, 0.15f, 0.2f);
            cameraGo.transform.position = new Vector3(position.x, position.y, -10f);

            // 添加 CinemachineBrain 以支持虚拟相机
            cameraGo.AddComponent<CinemachineBrain>();

            // 添加 AudioListener
            cameraGo.AddComponent<AudioListener>();

            // 同时创建虚拟相机
            CreateBattleVirtualCamera(position);

            Debug.Log("[BattleScene] Dev mode camera created");
        }

        void CreateBattleMap()
        {
            if (battleContext?.Map == null) return;

            int width = battleContext.Map.Width;
            int height = battleContext.Map.Height;

            // 创建 Grid
            var gridGo = new GameObject("BattleGrid");
            battleGrid = gridGo.AddComponent<Grid>();
            battleGrid.cellSize = Vector3.one;

            // 创建 Tilemap
            var tilemapGo = new GameObject("Tilemap");
            tilemapGo.transform.SetParent(gridGo.transform);
            battleTilemap = tilemapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            tilemapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

            // 如果有瓦片，填充地图
            if (grassTile != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var tile = battleContext.Map.GetTile(new Vector2Int(x, y));
                        if (tile != null && tile.IsWalkable)
                        {
                            battleTilemap.SetTile(new Vector3Int(x, y, 0), grassTile);
                        }
                        else if (obstacleTile != null && tile != null && !tile.IsWalkable)
                        {
                            battleTilemap.SetTile(new Vector3Int(x, y, 0), obstacleTile);
                        }
                    }
                }
            }
            else
            {
                // 没有瓦片时，创建简单的网格线指示
                CreateSimpleGridVisual(width, height);
            }

            Debug.Log($"[BattleScene] Battle map created: {width}x{height}");
        }

        /// <summary>
        /// 创建简单的网格视觉效果（当没有 Tile 资源时）
        /// </summary>
        void CreateSimpleGridVisual(int width, int height)
        {
            var gridVisualGo = new GameObject("GridVisual");

            // 使用 LineRenderer 绘制网格线
            for (int x = 0; x <= width; x++)
            {
                var lineGo = new GameObject($"VLine_{x}");
                lineGo.transform.SetParent(gridVisualGo.transform);
                var line = lineGo.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(x, 0, 0));
                line.SetPosition(1, new Vector3(x, height, 0));
                line.startWidth = 0.02f;
                line.endWidth = 0.02f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                line.endColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            for (int y = 0; y <= height; y++)
            {
                var lineGo = new GameObject($"HLine_{y}");
                lineGo.transform.SetParent(gridVisualGo.transform);
                var line = lineGo.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.SetPosition(0, new Vector3(0, y, 0));
                line.SetPosition(1, new Vector3(width, y, 0));
                line.startWidth = 0.02f;
                line.endWidth = 0.02f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                line.endColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }

        void CreateUnitContainer()
        {
            var containerGo = new GameObject("UnitContainer");
            unitContainer = containerGo.transform;
        }

        void CreateTurnManager()
        {
            var turnManagerGo = new GameObject("BattleTurnManager");
            turnManager = turnManagerGo.AddComponent<BattleTurnManager>();
        }

        void CreateInputController()
        {
            var inputGo = new GameObject("BattleInputController");
            inputController = inputGo.AddComponent<BattleInputController>();

            // 设置相机引用
            inputController.SetCamera(battleCamera);
        }

        void CreateHUD()
        {
            if (hudPrefab != null)
            {
                var hudGo = Instantiate(hudPrefab);
                battleHUD = hudGo.GetComponent<BattleHUD>();
            }
            else
            {
                // 创建最小化的 HUD
                CreateMinimalHUD();
            }
        }

        void CreateMinimalHUD()
        {
            // 创建 Canvas
            var canvasGo = new GameObject("BattleCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 添加 HUD 组件
            battleHUD = canvasGo.AddComponent<BattleHUD>();

            Debug.Log("[BattleScene] Minimal HUD created");
        }

        void InitializeBattle()
        {
            if (battleContext == null) return;

            // 创建单位工厂
            unitFactory = new BattleUnitFactory(unitConfigDatabase, unitPrefab, unitContainer);

            // 生成单位
            SpawnUnits();

            // 创建行动执行器
            actionExecutor = new BattleActionExecutor(battleContext, this);

            // 设置坐标转换
            if (battleTilemap != null)
            {
                actionExecutor.CellToWorldConverter = cell =>
                    battleTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            }
            else
            {
                // 无 Tilemap 时使用简单的 1:1 映射
                actionExecutor.CellToWorldConverter = cell =>
                    new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0);
            }

            // 创建玩家行动提供者
            playerProvider = new BattleActionProvider(battleContext, battleCamera);

            // 创建 AI 行动提供者
            aiProvider = new BattleAIProvider(battleContext);

            // 初始化回合管理器
            if (turnManager != null)
            {
                turnManager.Initialize(battleContext, actionExecutor);

                // 注册行动提供者：玩家控制攻击方，AI 控制防守方
                turnManager.RegisterProvider(BattleSide.Attacker, playerProvider);
                turnManager.RegisterProvider(BattleSide.Defender, aiProvider);
            }

            // 绑定输入控制器
            if (inputController != null)
            {
                inputController.BindActionProvider(playerProvider);
            }

            // 绑定 HUD
            if (battleHUD != null)
            {
                battleHUD.Bind(battleContext, turnManager);
            }

            Debug.Log("[BattleScene] Battle initialized");
        }

        void SpawnUnits()
        {
            if (battleContext?.InitData == null || unitFactory == null)
            {
                Debug.LogError("[BattleScene] Cannot spawn units: missing data or factory");
                return;
            }

            var attackers = new List<BattleUnit>();
            var defenders = new List<BattleUnit>();

            // 生成攻击方单位（左侧）
            if (battleContext.InitData.AttackerArmy != null)
            {
                int index = 0;
                foreach (var stack in battleContext.InitData.AttackerArmy)
                {
                    if (stack == null || stack.Count <= 0) continue;

                    var position = battleContext.Map.GetAttackerStartPosition(index);
                    var unit = unitFactory.Create(stack, BattleSide.Attacker, position);

                    if (unit != null)
                    {
                        battleContext.Map.PlaceUnit(unit, position);
                        attackers.Add(unit);
                        allUnits.Add(unit);
                        UpdateUnitWorldPosition(unit);
                        index++;
                    }
                }
            }

            // 生成防守方单位（右侧）
            if (battleContext.InitData.DefenderArmy != null)
            {
                int index = 0;
                foreach (var stack in battleContext.InitData.DefenderArmy)
                {
                    if (stack == null || stack.Count <= 0) continue;

                    var position = battleContext.Map.GetDefenderStartPosition(index);
                    var unit = unitFactory.Create(stack, BattleSide.Defender, position);

                    if (unit != null)
                    {
                        battleContext.Map.PlaceUnit(unit, position);
                        defenders.Add(unit);
                        allUnits.Add(unit);
                        UpdateUnitWorldPosition(unit);
                        unit.SetFacing(Vector2Int.left); // 防守方朝左
                        index++;
                    }
                }
            }

            // 注册单位到上下文
            battleContext.RegisterUnits(attackers, defenders);

            Debug.Log($"[BattleScene] Spawned {attackers.Count} attackers, {defenders.Count} defenders");
        }

        void UpdateUnitWorldPosition(BattleUnit unit)
        {
            if (unit == null) return;

            Vector3 worldPos;
            if (battleTilemap != null)
            {
                worldPos = battleTilemap.GetCellCenterWorld(new Vector3Int(unit.CellPosition.Value.x, unit.CellPosition.Value.y, 0));
            }
            else
            {
                // 简单的 1:1 映射，格子中心偏移 0.5
                worldPos = new Vector3(unit.CellPosition.Value.x + 0.5f, unit.CellPosition.Value.y + 0.5f, 0);
            }

            unit.transform.position = worldPos;
        }

        void StartBattle()
        {
            if (turnManager == null) return;

            // 启用输入
            inputController?.EnableInput();
            playerProvider?.SetEnabled(true);

            // 开始战斗
            turnManager.StartBattle();

            Debug.Log("[BattleScene] Battle started");
        }

        #region Event Handlers

        [AutoSubscribe]
        void OnBattleEnded(BattleEndedEvent e)
        {
            Debug.Log($"[BattleScene] Battle ended: {e.Result}");

            // 禁用输入
            inputController?.DisableInput();
            playerProvider?.SetEnabled(false);

            // 显示战斗结果 UI
            ShowBattleResult(e.Result, e.Rewards);
        }

        void ShowBattleResult(BattleResult result, BattleRewards rewards)
        {
            // TODO: 显示战斗结果 UI
            Debug.Log($"[BattleScene] Result: {result}, Experience: {rewards?.Experience ?? 0}");

            // 临时：延迟后自动退出
            Invoke(nameof(ExitBattle), 2f);
        }

        /// <summary>
        /// 退出战斗，返回世界地图
        /// </summary>
        public void ExitBattle()
        {
            Debug.Log("[BattleScene] Exiting battle");

            // 清理提供者
            playerProvider?.Dispose();

            // 销毁战斗上下文（会自动恢复 WorldContext）
            sessionContext?.DisposeChild<BattleContext>();

            // 卸载战斗场景
            SceneManager.UnloadSceneAsync("BattleScene");
        }

        /// <summary>
        /// 撤退（供 UI 按钮调用）
        /// </summary>
        public void OnRetreatClicked()
        {
            battleContext?.Retreat();
        }

        #endregion

        protected override void OnDestroy()
        {
            playerProvider?.Dispose();
            base.OnDestroy();
        }
    }
}
