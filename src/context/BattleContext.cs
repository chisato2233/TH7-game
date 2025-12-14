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
        // 战斗状态
        public BattlePhase Phase { get; private set; } = BattlePhase.Init;
        public int CurrentRound { get; private set; } = 0;
        public BattleResult Result { get; private set; } = BattleResult.None;

        // 战斗数据（示例）
        // public BattleField Field { get; private set; }
        // public List<BattleUnit> AttackerUnits { get; private set; }
        // public List<BattleUnit> DefenderUnits { get; private set; }
        // public BattleUnit CurrentUnit { get; private set; }
        // public Queue<BattleUnit> ActionQueue { get; private set; }

        protected override void OnInitialize()
        {
            Debug.Log("[Battle] 战斗开始");
            Phase = BattlePhase.Init;

            // 初始化战场
            // SetupBattleField();
            // PlaceUnits();
            // CalculateInitiative();

            // 进入第一回合
            StartNewRound();
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 根据当前阶段处理战斗逻辑
            switch (Phase)
            {
                case BattlePhase.RoundStart:
                    // ProcessRoundStart();
                    break;
                case BattlePhase.UnitSelect:
                    // SelectNextUnit();
                    break;
                case BattlePhase.ActionSelect:
                    // 等待玩家输入或AI决策
                    break;
                case BattlePhase.ActionExecute:
                    // ExecuteAction(deltaTime);
                    break;
                case BattlePhase.RoundEnd:
                    // ProcessRoundEnd();
                    break;
                case BattlePhase.BattleEnd:
                    // 战斗已结束，等待退出
                    break;
            }
        }

        protected override void OnDispose()
        {
            Debug.Log($"[Battle] 战斗结束，结果: {Result}，回合数: {CurrentRound}");

            // 清理战斗资源
            // CleanupBattleField();

            // 恢复探索上下文
            var session = GetParent<SessionContext>();
            var world = session?.GetChild<WorldContext>();
            world?.Resume();
        }

        /// <summary>
        /// 开始新回合
        /// </summary>
        public void StartNewRound()
        {
            CurrentRound++;
            Phase = BattlePhase.RoundStart;
            Debug.Log($"[Battle] 回合 {CurrentRound} 开始");

            // 重置单位行动点、处理回合开始效果等
            // ResetUnitActions();
            // ProcessStartOfRoundEffects();

            Phase = BattlePhase.UnitSelect;
        }

        /// <summary>
        /// 设置当前阶段
        /// </summary>
        public void SetPhase(BattlePhase phase)
        {
            Debug.Log($"[Battle] 阶段切换: {Phase} -> {phase}");
            Phase = phase;
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndBattle(BattleResult result)
        {
            Result = result;
            Phase = BattlePhase.BattleEnd;
            Debug.Log($"[Battle] 战斗结算: {result}");

            // 计算战利品、经验等
            // CalculateRewards();

            // 通知上层销毁此上下文
            var session = GetParent<SessionContext>();
            session?.DisposeChild(this);
        }

        /// <summary>
        /// 撤退
        /// </summary>
        public void Retreat()
        {
            EndBattle(BattleResult.Retreat);
        }
    }
}
