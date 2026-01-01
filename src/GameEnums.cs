namespace TH7
{
    // ============================================
    // 上下文相关
    // ============================================

    /// <summary>
    /// 上下文状态
    /// </summary>
    public enum ContextState
    {
        None,
        Initializing,
        Active,
        Paused,
        Disposing,
        Disposed
    }

    // ============================================
    // 战斗相关
    // ============================================

    /// <summary>
    /// 战斗阶段
    /// </summary>
    public enum BattlePhase
    {
        Init,           // 战斗初始化
        RoundStart,     // 回合开始
        UnitSelect,     // 选择行动单位
        ActionSelect,   // 选择行动
        ActionExecute,  // 执行行动
        RoundEnd,       // 回合结束
        BattleEnd       // 战斗结束
    }

    /// <summary>
    /// 战斗结果
    /// </summary>
    public enum BattleResult
    {
        None,
        Victory,
        Defeat,
        Retreat,
        Draw
    }

    // ============================================
    // 地图相关
    // ============================================

    // 基础地表类型
    public enum GroundType
    {
        Land,       // 陆地
        Water,      // 浅水
        DeepWater,  // 深水
        Void        // 虚空/边界
    }

    // 地表覆盖物
    public enum SurfaceType
    {
        None,
        Road,       // 道路
        Forest,     // 森林
        Mountain,   // 山脉
        Hill,       // 丘陵
        Swamp,      // 沼泽
        Sand,       // 沙地
        Snow,       // 雪地
        Lava        // 熔岩
    }

    // 地图物件类型
    public enum MapObjectType
    {
        None,
        Town,       // 城镇
        Mine,       // 矿场
        Artifact,   // 神器
        Monster,    // 怪物
        Resource,   // 资源堆
        Portal,     // 传送门
        Shrine      // 神殿
    }

    // 生态区域/文明风格
    public enum BiomeType
    {
        Neutral,    // 中立
        Arabian,    // 阿拉伯
        Egyptian,   // 埃及
        Indian,     // 印度
        Greek,      // 希腊
        Chinese,    // 汉唐
        Mongolian,  // 蒙古
        Islander    // 南岛
    }

    // ============================================
    // 资源相关
    // ============================================

    // 资源类型（英雄无敌风格）
    public enum ResourceType
    {
        Gold,       // 金币
        Wood,       // 木材
        Ore,        // 矿石
        Crystal,    // 水晶
        Gem,        // 宝石
        Sulfur,     // 硫磺
        Mercury     // 水银
    }

    // ============================================
    // 城镇相关
    // ============================================

    // 建筑类型
    public enum BuildingType
    {
        // 核心建筑
        TownHall,       // 城镇大厅（产金）
        Fort,           // 要塞（城防）
        Tavern,         // 酒馆（招募英雄）
        Marketplace,    // 市场（资源交易）
        Blacksmith,     // 铁匠铺（攻击加成）

        // 魔法建筑
        MageGuild,      // 法师公会（学习魔法）

        // 兵种建筑（按等级）
        Dwelling1,      // 1级兵种建筑
        Dwelling2,      // 2级兵种建筑
        Dwelling3,      // 3级兵种建筑
        Dwelling4,      // 4级兵种建筑
        Dwelling5,      // 5级兵种建筑
        Dwelling6,      // 6级兵种建筑
        Dwelling7,      // 7级兵种建筑

        // 特殊建筑
        Grail           // 圣杯建筑
    }

    // 建筑等级
    public enum BuildingTier
    {
        None,
        Basic,      // 基础
        Upgraded    // 升级
    }

    // ============================================
    // 兵种相关
    // ============================================

    // 兵种等级
    public enum UnitTier
    {
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3,
        Tier4 = 4,
        Tier5 = 5,
        Tier6 = 6,
        Tier7 = 7
    }

    // 兵种移动类型
    public enum MovementType
    {
        Ground,     // 地面
        Flying      // 飞行
    }

    // 攻击类型
    public enum AttackType
    {
        Melee,      // 近战
        Ranged      // 远程
    }

    // ============================================
    // 世界探索相关
    // ============================================

    /// <summary>
    /// 回合阶段
    /// </summary>
    public enum TurnPhase
    {
        Idle,           // 空闲
        PlayerTurn,     // 玩家回合
        AITurn,         // AI回合
        TurnEnd         // 回合结束
    }
}
