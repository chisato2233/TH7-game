using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗单位待命状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Idle)]
    public class BattleUnitIdleState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
        }
    }

    /// <summary>
    /// 战斗单位被选中状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Selected)]
    public class BattleUnitSelectedState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
            // 选中效果由 View 层处理
        }
    }

    /// <summary>
    /// 战斗单位移动状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Moving)]
    public class BattleUnitMovingState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(true);
        }

        public override void OnExit(BattleUnit unit)
        {
            unit.SetMoving(false);
        }
    }

    /// <summary>
    /// 战斗单位行动状态（攻击/施法）
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Acting)]
    public class BattleUnitActingState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
        }
    }

    /// <summary>
    /// 战斗单位等待状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Waiting)]
    public class BattleUnitWaitingState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
        }
    }

    /// <summary>
    /// 战斗单位防御状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Defending)]
    public class BattleUnitDefendingState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
            // 可以在这里添加防御姿态动画
        }
    }

    /// <summary>
    /// 战斗单位死亡状态
    /// </summary>
    [StateBinding(typeof(BattleUnitState), BattleUnitState.Dead)]
    public class BattleUnitDeadState : StateBase<BattleUnit>
    {
        public override void OnEnter(BattleUnit unit)
        {
            unit.SetMoving(false);
            unit.PlayDeathAnimation();
        }
    }
}
