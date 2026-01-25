using System;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 战斗初始化数据
    /// </summary>
    public class BattleInitData
    {
        /// <summary>
        /// 攻击方英雄
        /// </summary>
        public Hero AttackerHero;

        /// <summary>
        /// 防守方英雄（可为空，表示野怪战斗）
        /// </summary>
        public Hero DefenderHero;

        /// <summary>
        /// 攻击方军队
        /// </summary>
        public UnitStack[] AttackerArmy;

        /// <summary>
        /// 防守方军队
        /// </summary>
        public UnitStack[] DefenderArmy;

        /// <summary>
        /// 地图宽度（标准 15）
        /// </summary>
        public int MapWidth = 15;

        /// <summary>
        /// 地图高度（标准 11）
        /// </summary>
        public int MapHeight = 11;

        /// <summary>
        /// 地形类型
        /// </summary>
        public BattleTerrainType Terrain = BattleTerrainType.Grass;

        /// <summary>
        /// 是否为攻城战
        /// </summary>
        public bool IsSiege;

        /// <summary>
        /// 攻城战时的城堡等级
        /// </summary>
        public int CastleLevel;
    }

    /// <summary>
    /// 战斗奖励
    /// </summary>
    public class BattleRewards
    {
        /// <summary>
        /// 获得的经验值
        /// </summary>
        public int Experience;

        /// <summary>
        /// 获得的资源
        /// </summary>
        public ResourceBundle Resources = new();

        /// <summary>
        /// 获得的物品 ID 列表
        /// </summary>
        public string[] ItemIds;

        /// <summary>
        /// 俘获的单位
        /// </summary>
        public UnitStack[] CapturedUnits;
    }

    /// <summary>
    /// 伤害计算结果
    /// </summary>
    public struct DamageResult
    {
        /// <summary>
        /// 最终伤害值
        /// </summary>
        public int FinalDamage;

        /// <summary>
        /// 击杀的单位数量
        /// </summary>
        public int UnitsKilled;

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCritical;

        /// <summary>
        /// 目标剩余血量
        /// </summary>
        public int RemainingHealth;

        /// <summary>
        /// 目标剩余数量
        /// </summary>
        public int RemainingCount;
    }

    /// <summary>
    /// 战斗行动结果
    /// </summary>
    public struct BattleActionResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// 造成的伤害（如果有）
        /// </summary>
        public DamageResult? DamageDealt;

        /// <summary>
        /// 受到的反击伤害（如果有）
        /// </summary>
        public DamageResult? RetaliationDamage;

        public static BattleActionResult Success() => new() { IsSuccess = true };
        public static BattleActionResult Success(DamageResult damage) => new() { IsSuccess = true, DamageDealt = damage };
        public static BattleActionResult Failed(string error) => new() { IsSuccess = false, ErrorMessage = error };
    }

    /// <summary>
    /// 战斗阵营
    /// </summary>
    public enum BattleSide
    {
        Attacker,
        Defender
    }

    /// <summary>
    /// 战斗地形类型
    /// </summary>
    public enum BattleTerrainType
    {
        Grass,      // 草地
        Sand,       // 沙漠
        Snow,       // 雪地
        Lava,       // 熔岩
        Swamp,      // 沼泽
        Underground // 地下
    }

    /// <summary>
    /// 战斗格子类型
    /// </summary>
    public enum BattleTileType
    {
        Normal,     // 普通地形
        Difficult,  // 困难地形（移动消耗 x2）
        Obstacle    // 障碍物（不可通行）
    }

    /// <summary>
    /// 战斗单位状态
    /// </summary>
    public enum BattleUnitState
    {
        Idle,       // 待命
        Selected,   // 被选中
        Moving,     // 移动中
        Acting,     // 执行行动（攻击、施法）
        Waiting,    // 等待中
        Defending,  // 防御中
        Dead        // 死亡
    }

    /// <summary>
    /// 战斗回合状态
    /// </summary>
    public enum BattleTurnState
    {
        Idle,               // 战斗未开始
        RoundStart,         // 回合开始
        SelectUnit,         // 选择行动单位
        WaitingForAction,   // 等待行动指令
        ExecutingAction,    // 执行行动
        RoundEnd,           // 回合结束
        BattleEnd           // 战斗结束
    }

    /// <summary>
    /// 战斗行动类型
    /// </summary>
    public enum BattleActionType
    {
        None,
        Move,           // 移动
        Attack,         // 近战攻击
        RangedAttack,   // 远程攻击
        Ability,        // 使用技能
        Wait,           // 等待
        Defend          // 防御
    }
}
