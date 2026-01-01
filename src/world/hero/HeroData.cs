using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 英雄属性名称常量
    /// 项目可自行定义属性名，这里是示例
    /// </summary>
    public static class HeroAttributes
    {
        public const string MovementPoints = "MovementPoints";
        public const string MaxMovementPoints = "MaxMovementPoints";
        public const string Attack = "Attack";
        public const string Defense = "Defense";
        public const string SpellPower = "SpellPower";
        public const string Knowledge = "Knowledge";
        public const string Morale = "Morale";
        public const string Luck = "Luck";
    }

    /// <summary>
    /// 英雄数据（可序列化，存档用）
    /// </summary>
    [Serializable]
    public class HeroData
    {
        public string HeroId;
        public string HeroName;

        // 位置
        public Vector3Int CellPosition;

        // 移动力
        public Reactive<int> MovementPoints = new(20);
        public int MaxMovementPoints = 20;

        // 所属
        public int OwnerPlayerId;
        public bool IsPlayerControlled => OwnerPlayerId == 0;

        // 军队（7个槽位）
        public UnitStack[] Army = new UnitStack[7];

        // 技能系统组件
        [NonSerialized]
        AbilitySystemComponent abilitySystem;

        /// <summary>
        /// 获取技能系统组件（延迟初始化）
        /// </summary>
        public AbilitySystemComponent AbilitySystem
        {
            get
            {
                if (abilitySystem == null)
                {
                    InitializeAbilitySystem();
                }
                return abilitySystem;
            }
        }

        public HeroData() { }

        public HeroData(string id, string name, Vector3Int position, int ownerId = 0)
        {
            HeroId = id;
            HeroName = name;
            CellPosition = position;
            OwnerPlayerId = ownerId;
        }

        /// <summary>
        /// 初始化技能系统
        /// </summary>
        public void InitializeAbilitySystem()
        {
            abilitySystem = new AbilitySystemComponent();

            // 初始化基础属性（使用字符串属性名）
            abilitySystem.InitializeFromConfig(
                (HeroAttributes.MaxMovementPoints, MaxMovementPoints),
                (HeroAttributes.MovementPoints, MovementPoints.Value)
            );

            // 添加英雄标签
            abilitySystem.AddTag("Unit.Hero");
            abilitySystem.AddTag(IsPlayerControlled ? "Team.Player" : "Team.Enemy");

            // 监听属性变化，同步到本地字段
            abilitySystem.WatchAttribute(HeroAttributes.MovementPoints, v => MovementPoints.Value = Mathf.RoundToInt(v));
            abilitySystem.WatchAttribute(HeroAttributes.MaxMovementPoints, v => MaxMovementPoints = Mathf.RoundToInt(v));
        }

        /// <summary>
        /// 消耗移动力
        /// </summary>
        public bool ConsumeMovement(int cost)
        {
            if (MovementPoints.Value < cost) return false;
            MovementPoints.Value -= cost;

            // 同步到技能系统
            if (abilitySystem != null)
            {
                abilitySystem.Attributes.SetBaseValue(HeroAttributes.MovementPoints, MovementPoints.Value);
            }

            return true;
        }

        /// <summary>
        /// 重置移动力（新回合）
        /// </summary>
        public void ResetMovement()
        {
            // 从技能系统获取最大移动力（可能被效果修改）
            if (abilitySystem != null)
            {
                int maxMp = abilitySystem.Attributes.GetCurrentValueInt(HeroAttributes.MaxMovementPoints);
                MovementPoints.Value = maxMp;
                abilitySystem.Attributes.SetBaseValue(HeroAttributes.MovementPoints, maxMp);
            }
            else
            {
                MovementPoints.Value = MaxMovementPoints;
            }
        }

        /// <summary>
        /// 检查是否还能行动
        /// </summary>
        public bool CanAct => MovementPoints.Value > 0;

        /// <summary>
        /// 回合开始处理
        /// </summary>
        public void OnTurnStart()
        {
            ResetMovement();
            abilitySystem?.OnTurnStart();
        }

        /// <summary>
        /// 回合结束处理
        /// </summary>
        public void OnTurnEnd()
        {
            abilitySystem?.OnTurnEnd();
        }

        /// <summary>
        /// 授予技能
        /// </summary>
        public AbilityInstance GrantAbility(GameplayAbility ability)
        {
            return AbilitySystem.GrantAbility(ability);
        }

        /// <summary>
        /// 应用效果
        /// </summary>
        public EffectInstance ApplyEffect(GameplayEffect effect, object source = null)
        {
            return AbilitySystem.ApplyEffectToSelf(effect, source);
        }
    }
}
