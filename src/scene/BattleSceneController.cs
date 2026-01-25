using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗场景控制器
    /// 管理战斗场景的生命周期，类似 WorldSceneController
    /// </summary>
    public class BattleSceneController : GameBehaviour
    {
        [Header("Turn Manager")]
        [SerializeField] BattleTurnManager turnManager;

        [Header("Map")]
        [SerializeField] Tilemap battleTilemap;
        [SerializeField] Transform unitContainer;

        [Header("Input")]
        [SerializeField] BattleInputController inputController;
        [SerializeField] Camera battleCamera;

        [Header("Config")]
        [SerializeField] UnitConfigDatabase unitConfigDatabase;

        [Header("Prefabs")]
        [SerializeField] GameObject unitPrefab;

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

            InitializeContext();

            if (battleContext != null)
            {
                InitializeBattle();
                StartBattle();
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

        void InitializeBattle()
        {
            if (battleContext == null) return;

            // 创建单位工厂
            unitFactory = new BattleUnitFactory(unitConfigDatabase, unitPrefab, unitContainer);

            // 生成单位
            SpawnUnits();

            // 创建行动执行器
            actionExecutor = new BattleActionExecutor(battleContext, this);

            // 设置坐标转换（如果有 Tilemap）
            if (battleTilemap != null)
            {
                actionExecutor.CellToWorldConverter = cell =>
                    battleTilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            }

            // 创建玩家行动提供者
            var camera = battleCamera != null ? battleCamera : Camera.main;
            playerProvider = new BattleActionProvider(battleContext, camera);

            // 创建 AI 行动提供者
            aiProvider = new BattleAIProvider(battleContext);

            // 初始化回合管理器
            if (turnManager != null)
            {
                turnManager.Initialize(battleContext, actionExecutor);

                // 注册行动提供者
                // 假设玩家控制攻击方，AI 控制防守方
                turnManager.RegisterProvider(BattleSide.Attacker, playerProvider);
                turnManager.RegisterProvider(BattleSide.Defender, aiProvider);
            }

            // 绑定输入控制器
            if (inputController != null)
            {
                inputController.BindActionProvider(playerProvider);
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

            // 生成攻击方单位
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

                        // 设置位置
                        UpdateUnitWorldPosition(unit);
                        index++;
                    }
                }
            }

            // 生成防守方单位
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

                        // 设置位置和朝向
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
                worldPos = new Vector3(unit.CellPosition.Value.x, unit.CellPosition.Value.y, 0);
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
