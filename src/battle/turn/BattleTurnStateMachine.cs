using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗回合空闲状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.Idle)]
    public class BattleTurnIdleState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            // 战斗未开始
        }
    }

    /// <summary>
    /// 战斗回合开始状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.RoundStart)]
    public class BattleTurnRoundStartState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            manager.ProcessRoundStart();
        }
    }

    /// <summary>
    /// 选择单位状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.SelectUnit)]
    public class BattleTurnSelectUnitState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            manager.SelectNextUnit();
        }
    }

    /// <summary>
    /// 等待行动状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.WaitingForAction)]
    public class BattleTurnWaitingState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            manager.RequestAction();
        }
    }

    /// <summary>
    /// 执行行动状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.ExecutingAction)]
    public class BattleTurnExecutingState : StateBase<BattleTurnManager>
    {
        // 等待行动执行完成
    }

    /// <summary>
    /// 回合结束状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.RoundEnd)]
    public class BattleTurnRoundEndState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            manager.ProcessRoundEnd();
        }
    }

    /// <summary>
    /// 战斗结束状态
    /// </summary>
    [StateBinding(typeof(BattleTurnState), BattleTurnState.BattleEnd)]
    public class BattleTurnBattleEndState : StateBase<BattleTurnManager>
    {
        public override void OnEnter(BattleTurnManager manager)
        {
            manager.ProcessBattleEnd();
        }
    }
}
