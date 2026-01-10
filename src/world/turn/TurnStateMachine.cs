using GameFramework;

namespace TH7
{
    /// <summary>
    /// 回合状态枚举
    /// </summary>
    public enum TurnState
    {
        Idle,               // 空闲（游戏未开始）
        DayStart,           // 日开始处理
        WaitingForAction,   // 等待玩家行动
        ExecutingAction,    // 正在执行行动
        Interacting,        // 交互中（城镇、战斗）
        DayEnd              // 日结束处理
    }

    /// <summary>
    /// 回合空闲状态
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.Idle)]
    public class TurnIdleState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            // 游戏尚未开始或已暂停
        }
    }

    /// <summary>
    /// 日开始状态
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.DayStart)]
    public class TurnDayStartState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            manager.ProcessDayStart();
        }
    }

    /// <summary>
    /// 等待行动状态
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.WaitingForAction)]
    public class TurnWaitingState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            manager.RequestNextAction();
        }
    }

    /// <summary>
    /// 执行行动状态
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.ExecutingAction)]
    public class TurnExecutingState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            // 行动正在执行中，等待完成回调
        }
    }

    /// <summary>
    /// 交互状态（城镇、战斗等）
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.Interacting)]
    public class TurnInteractingState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            // 等待交互完成，外部调用 Resume() 继续
        }
    }

    /// <summary>
    /// 日结束状态
    /// </summary>
    [StateBinding(typeof(TurnState), TurnState.DayEnd)]
    public class TurnDayEndState : StateBase<WorldTurnManager>
    {
        public override void OnEnter(WorldTurnManager manager)
        {
            manager.ProcessDayEnd();
        }
    }
}
