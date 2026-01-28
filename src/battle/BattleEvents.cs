using UnityEngine;

namespace TH7
{
    // ============================================
    // 战斗流程事件
    // ============================================

    /// <summary>
    /// 战斗开始事件
    /// </summary>
    public struct BattleStartedEvent
    {
        public BattleContext Context;
    }

    /// <summary>
    /// 战斗结束事件
    /// </summary>
    public struct BattleEndedEvent
    {
        public BattleResult Result;
        public BattleRewards Rewards;
        public BattleInitData InitData; // 原始战斗数据，用于处理战斗结果
    }

    /// <summary>
    /// 战斗回合开始事件
    /// </summary>
    public struct BattleRoundStartedEvent
    {
        public int Round;
    }

    /// <summary>
    /// 战斗回合结束事件
    /// </summary>
    public struct BattleRoundEndedEvent
    {
        public int Round;
    }

    // ============================================
    // 单位事件
    // ============================================

    /// <summary>
    /// 单位回合开始事件
    /// </summary>
    public struct UnitTurnStartedEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 单位回合结束事件
    /// </summary>
    public struct UnitTurnEndedEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 单位死亡事件
    /// </summary>
    public struct UnitDiedEvent
    {
        public BattleUnit Unit;
        public BattleUnit Killer;
    }

    /// <summary>
    /// 单位被选中事件
    /// </summary>
    public struct BattleUnitSelectedEvent
    {
        public BattleUnit Unit;
    }

    /// <summary>
    /// 单位取消选中事件
    /// </summary>
    public struct BattleUnitDeselectedEvent { }

    // ============================================
    // 行动事件
    // ============================================

    /// <summary>
    /// 战斗行动开始事件
    /// </summary>
    public struct BattleActionStartedEvent
    {
        public BattleAction Action;
    }

    /// <summary>
    /// 战斗行动完成事件
    /// </summary>
    public struct BattleActionCompletedEvent
    {
        public BattleAction Action;
        public BattleActionResult Result;
    }

    // ============================================
    // 战斗效果事件
    // ============================================

    /// <summary>
    /// 造成伤害事件
    /// </summary>
    public struct DamageDealtEvent
    {
        public BattleUnit Source;
        public BattleUnit Target;
        public int Damage;
        public int UnitsKilled;
        public bool IsCritical;
        public bool IsRanged;
        public bool IsRetaliation;
    }

    /// <summary>
    /// 治疗事件
    /// </summary>
    public struct HealingDoneEvent
    {
        public BattleUnit Source;
        public BattleUnit Target;
        public int Amount;
        public int UnitsResurrected;
    }

    // ============================================
    // 输入事件
    // ============================================

    /// <summary>
    /// 战斗格子点击事件
    /// </summary>
    public struct BattleTileClickedEvent
    {
        public Vector2Int Position;
        public bool IsRightClick;
    }

    /// <summary>
    /// 战斗单位点击事件
    /// </summary>
    public struct BattleUnitClickedEvent
    {
        public BattleUnit Unit;
        public bool IsRightClick;
    }

    /// <summary>
    /// 可移动范围更新事件
    /// </summary>
    public struct MovableRangeUpdatedEvent
    {
        public BattleUnit Unit;
        public Vector2Int[] ReachableTiles;
    }

    /// <summary>
    /// 可攻击目标更新事件
    /// </summary>
    public struct AttackableTargetsUpdatedEvent
    {
        public BattleUnit Unit;
        public BattleUnit[] Targets;
    }
}
