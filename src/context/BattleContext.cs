using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗上下文
    /// 生命周期：进入战斗 -> 战斗结束
    /// 包含：战场数据、单位列表、回合状态、行动队列等
    /// </summary>
    public class BattleContext : GameContext
    {
        #region Init Data

        /// <summary>
        /// 战斗初始化数据
        /// </summary>
        public BattleInitData InitData { get; private set; }

        #endregion

        #region Runtime State

        /// <summary>
        /// 战斗阶段
        /// </summary>
        public BattlePhase Phase { get; private set; } = BattlePhase.Init;

        /// <summary>
        /// 当前回合数
        /// </summary>
        public int CurrentRound { get; private set; } = 0;

        /// <summary>
        /// 战斗结果
        /// </summary>
        public BattleResult Result { get; private set; } = BattleResult.None;

        #endregion

        #region Reactive Data (for UI binding)

        /// <summary>
        /// 当前阶段（响应式）
        /// </summary>
        public Reactive<BattlePhase> CurrentPhase { get; } = new(BattlePhase.Init);

        /// <summary>
        /// 回合数（响应式）
        /// </summary>
        public Reactive<int> Round { get; } = new(0);

        /// <summary>
        /// 当前行动单位（响应式）
        /// </summary>
        public Reactive<BattleUnit> ActiveUnit { get; } = new();

        #endregion

        #region Core Components

        /// <summary>
        /// 战斗地图
        /// </summary>
        public BattleMap Map { get; private set; }

        /// <summary>
        /// 行动顺序队列
        /// </summary>
        public InitiativeQueue Initiative { get; private set; }

        #endregion

        #region Units

        readonly List<BattleUnit> attackerUnits = new();
        readonly List<BattleUnit> defenderUnits = new();

        /// <summary>
        /// 攻击方单位列表
        /// </summary>
        public IReadOnlyList<BattleUnit> AttackerUnits => attackerUnits;

        /// <summary>
        /// 防守方单位列表
        /// </summary>
        public IReadOnlyList<BattleUnit> DefenderUnits => defenderUnits;

        /// <summary>
        /// 当前行动单位
        /// </summary>
        public BattleUnit CurrentUnit { get; private set; }

        #endregion

        #region Setup

        /// <summary>
        /// 设置战斗初始化数据
        /// </summary>
        public void Setup(BattleInitData initData)
        {
            InitData = initData;
            Debug.Log($"[BattleContext] Setup with attacker army: {initData.AttackerArmy?.Length ?? 0}, defender army: {initData.DefenderArmy?.Length ?? 0}");
        }

        #endregion

        #region Lifecycle

        protected override void OnInitialize()
        {
            Debug.Log("[BattleContext] Battle starting");

            if (InitData == null)
            {
                Debug.LogError("[BattleContext] InitData is null!");
                return;
            }

            // 初始化地图
            Map = new BattleMap();
            Map.Initialize(InitData.MapWidth, InitData.MapHeight, InitData.Terrain);

            // 初始化行动队列
            Initiative = new InitiativeQueue();

            Phase = BattlePhase.Init;
            CurrentPhase.Value = Phase;

            Debug.Log("[BattleContext] Battle initialized, waiting for units");
        }

        /// <summary>
        /// 注册单位（由 BattleSceneController 调用）
        /// </summary>
        public void RegisterUnits(IEnumerable<BattleUnit> attackers, IEnumerable<BattleUnit> defenders)
        {
            attackerUnits.Clear();
            defenderUnits.Clear();

            if (attackers != null)
            {
                attackerUnits.AddRange(attackers);
            }
            if (defenders != null)
            {
                defenderUnits.AddRange(defenders);
            }

            // 计算行动顺序
            Initiative.CalculateOrder(attackerUnits, defenderUnits);

            Debug.Log($"[BattleContext] Registered {attackerUnits.Count} attackers, {defenderUnits.Count} defenders");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 战斗逻辑由 BattleTurnManager 驱动
        }

        protected override void OnDispose()
        {
            Debug.Log($"[BattleContext] Battle ended, result: {Result}, rounds: {CurrentRound}");

            // 清理单位
            foreach (var unit in attackerUnits)
            {
                if (unit != null)
                {
                    unit.Cleanup();
                    Object.Destroy(unit.gameObject);
                }
            }
            foreach (var unit in defenderUnits)
            {
                if (unit != null)
                {
                    unit.Cleanup();
                    Object.Destroy(unit.gameObject);
                }
            }
            attackerUnits.Clear();
            defenderUnits.Clear();

            // 清理地图
            Map?.Dispose();

            // 恢复探索上下文
            var session = GetParent<SessionContext>();
            var world = session?.GetChild<WorldContext>();
            world?.Resume();
        }

        #endregion

        #region Round Management

        /// <summary>
        /// 开始新回合
        /// </summary>
        public void StartNewRound()
        {
            CurrentRound++;
            Round.Value = CurrentRound;
            Phase = BattlePhase.RoundStart;
            CurrentPhase.Value = Phase;

            Debug.Log($"[BattleContext] Round {CurrentRound} started");

            // 重置所有单位的行动状态
            foreach (var unit in GetAllUnits())
            {
                if (unit != null && unit.IsAlive)
                {
                    unit.OnRoundStart();
                }
            }

            // 刷新行动顺序
            Initiative.RefreshForNewRound();

            Phase = BattlePhase.UnitSelect;
            CurrentPhase.Value = Phase;
        }

        /// <summary>
        /// 设置当前阶段
        /// </summary>
        public void SetPhase(BattlePhase phase)
        {
            Debug.Log($"[BattleContext] Phase: {Phase} -> {phase}");
            Phase = phase;
            CurrentPhase.Value = phase;
        }

        #endregion

        #region Battle End

        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        public void CheckBattleEnd()
        {
            bool attackerAlive = attackerUnits.Any(u => u != null && u.IsAlive);
            bool defenderAlive = defenderUnits.Any(u => u != null && u.IsAlive);

            if (!attackerAlive)
            {
                EndBattle(BattleResult.Defeat);
            }
            else if (!defenderAlive)
            {
                EndBattle(BattleResult.Victory);
            }
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndBattle(BattleResult result)
        {
            Result = result;
            Phase = BattlePhase.BattleEnd;
            CurrentPhase.Value = Phase;

            Debug.Log($"[BattleContext] Battle ended: {result}");

            // 计算奖励
            var rewards = CalculateRewards();

            // 发布战斗结束事件
            GameEntry.Instance?.GetSystem<EventSystem>()?.Publish(
                new BattleEndedEvent
                {
                    Result = result,
                    Rewards = rewards,
                    InitData = InitData // 传递原始数据，用于处理战斗后果
                }
            );
        }

        /// <summary>
        /// 撤退
        /// </summary>
        public void Retreat()
        {
            EndBattle(BattleResult.Retreat);
        }

        /// <summary>
        /// 计算战斗奖励
        /// </summary>
        BattleRewards CalculateRewards()
        {
            var rewards = new BattleRewards();

            if (Result == BattleResult.Victory)
            {
                // 计算经验值
                int exp = 0;
                foreach (var unit in defenderUnits)
                {
                    if (unit?.Config != null)
                    {
                        // 经验 = 单位等级 * 数量
                        exp += ((int)unit.Config.Tier + 1) * 10;
                    }
                }
                rewards.Experience = exp;

                // TODO: 计算战利品
            }

            return rewards;
        }

        #endregion

        #region Unit Queries

        /// <summary>
        /// 获取所有单位
        /// </summary>
        public IEnumerable<BattleUnit> GetAllUnits()
        {
            foreach (var u in attackerUnits)
            {
                if (u != null) yield return u;
            }
            foreach (var u in defenderUnits)
            {
                if (u != null) yield return u;
            }
        }

        /// <summary>
        /// 获取指定位置的单位
        /// </summary>
        public BattleUnit GetUnitAt(Vector2Int cell)
        {
            foreach (var unit in GetAllUnits())
            {
                if (unit.IsAlive && unit.CellPosition.Value == cell)
                    return unit;
            }
            return null;
        }

        /// <summary>
        /// 获取敌方单位列表
        /// </summary>
        public IReadOnlyList<BattleUnit> GetEnemies(BattleSide side)
        {
            return side == BattleSide.Attacker ? defenderUnits : attackerUnits;
        }

        /// <summary>
        /// 获取友方单位列表
        /// </summary>
        public IReadOnlyList<BattleUnit> GetAllies(BattleSide side)
        {
            return side == BattleSide.Attacker ? attackerUnits : defenderUnits;
        }

        /// <summary>
        /// 获取存活的攻击方单位数
        /// </summary>
        public int GetAliveAttackerCount()
        {
            return attackerUnits.Count(u => u != null && u.IsAlive);
        }

        /// <summary>
        /// 获取存活的防守方单位数
        /// </summary>
        public int GetAliveDefenderCount()
        {
            return defenderUnits.Count(u => u != null && u.IsAlive);
        }

        #endregion
    }
}
