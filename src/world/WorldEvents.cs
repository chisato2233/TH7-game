using UnityEngine;

namespace TH7
{
    // ============================================
    // 回合相关事件
    // ============================================

    /// <summary>
    /// 回合阶段变化事件
    /// </summary>
    public struct TurnPhaseChangedEvent
    {
        public TurnPhase Phase;
        public TurnPhase PreviousPhase;
    }

    /// <summary>
    /// 英雄回合开始事件
    /// </summary>
    public struct HeroTurnStartedEvent
    {
        public Hero Hero;
    }

    /// <summary>
    /// 英雄回合结束事件
    /// </summary>
    public struct HeroTurnEndedEvent
    {
        public Hero Hero;
    }

    /// <summary>
    /// 一天结束事件
    /// </summary>
    public struct DayEndedEvent
    {
        public int Day;
        public int Week;
        public int Month;
    }

    /// <summary>
    /// 一周开始事件
    /// </summary>
    public struct WeekStartedEvent
    {
        public int Week;
        public int Month;
    }

    // ============================================
    // 英雄行动事件
    // ============================================

    /// <summary>
    /// 行动开始事件
    /// </summary>
    public struct ActionStartedEvent
    {
        public HeroAction Action;
    }

    /// <summary>
    /// 行动完成事件
    /// </summary>
    public struct ActionCompletedEvent
    {
        public HeroAction Action;
        public ActionResult Result;
    }

    /// <summary>
    /// 英雄移动事件
    /// </summary>
    public struct HeroMovedEvent
    {
        public Hero Hero;
        public Vector3Int FromCell;
        public Vector3Int ToCell;
    }

    /// <summary>
    /// 请求进入城镇事件
    /// </summary>
    public struct EnterTownRequestedEvent
    {
        public Hero Hero;
        public TownData Town;
    }

    /// <summary>
    /// 请求战斗事件
    /// </summary>
    public struct BattleRequestedEvent
    {
        public Hero Hero;
        public object Enemy;
    }

    // ============================================
    // 输入相关事件
    // ============================================

    /// <summary>
    /// 地图点击事件
    /// </summary>
    public struct MapClickedEvent
    {
        public Vector3Int CellPosition;
        public Vector3 WorldPosition;
        public bool IsRightClick;
    }

    /// <summary>
    /// 请求结束回合事件
    /// </summary>
    public struct EndTurnRequestedEvent { }
}
