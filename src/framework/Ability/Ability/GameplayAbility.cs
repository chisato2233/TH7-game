using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum AbilityType
    {
        /// <summary>
        /// 被动技能 - 始终生效
        /// </summary>
        Passive,

        /// <summary>
        /// 主动技能 - 需要手动激活
        /// </summary>
        Active,

        /// <summary>
        /// 触发技能 - 特定条件下自动触发
        /// </summary>
        Triggered
    }

    /// <summary>
    /// 技能触发时机
    /// </summary>
    public enum AbilityTrigger
    {
        None,
        OnTurnStart,        // 回合开始
        OnTurnEnd,          // 回合结束
        OnBattleStart,      // 战斗开始
        OnBattleEnd,        // 战斗结束
        OnAttack,           // 攻击时
        OnBeingAttacked,    // 被攻击时
        OnDamageDealt,      // 造成伤害后
        OnDamageTaken,      // 受到伤害后
        OnKill,             // 击杀目标后
        OnDeath,            // 死亡时
        OnHeal,             // 治疗时
        OnMove,             // 移动时
        OnSpellCast,        // 施法时
        OnStackDeath,       // 部队死亡时
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum AbilityTargetType
    {
        None,               // 无目标
        Self,               // 自身
        SingleEnemy,        // 单个敌人
        SingleAlly,         // 单个友军
        AllEnemies,         // 所有敌人
        AllAllies,          // 所有友军
        AreaOfEffect,       // 范围效果
    }

    /// <summary>
    /// Gameplay 技能配置 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbility", menuName = "TH7/Ability/Gameplay Ability")]
    public class GameplayAbility : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("技能唯一ID")]
        public string AbilityId;

        [Tooltip("技能显示名称")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("技能描述")]
        public string Description;

        [Header("类型")]
        [Tooltip("技能类型")]
        public AbilityType Type = AbilityType.Active;

        [Tooltip("触发时机（仅对 Triggered 类型有效）")]
        public AbilityTrigger Trigger = AbilityTrigger.None;

        [Tooltip("目标类型")]
        public AbilityTargetType TargetType = AbilityTargetType.None;

        [Header("消耗")]
        [Tooltip("消耗的属性名称（如 Mana, Energy, Stamina 等）")]
        public string ManaAttribute = "Mana";

        [Tooltip("消耗数值")]
        [Min(0)]
        public int ManaCost;

        [Tooltip("冷却回合数")]
        [Min(0)]
        public int Cooldown;

        [Tooltip("每场战斗使用次数限制（0=无限制）")]
        [Min(0)]
        public int UsesPerBattle;

        [Header("标签")]
        [Tooltip("技能自身的标签")]
        public string[] AbilityTags;

        [Tooltip("拥有者必须拥有的标签")]
        public string[] RequiredOwnerTags;

        [Tooltip("拥有者不能拥有的标签")]
        public string[] BlockedOwnerTags;

        [Tooltip("目标必须拥有的标签")]
        public string[] RequiredTargetTags;

        [Tooltip("目标不能拥有的标签")]
        public string[] BlockedTargetTags;

        [Tooltip("激活时临时添加到拥有者的标签")]
        public string[] ActivationGrantedTags;

        [Header("效果")]
        [Tooltip("激活时应用到目标的效果")]
        public GameplayEffect[] EffectsToApply;

        [Tooltip("激活时应用到自身的效果")]
        public GameplayEffect[] SelfEffects;

        [Tooltip("被动技能持续生效的效果")]
        public GameplayEffect[] PassiveEffects;

        [Header("视觉")]
        [Tooltip("技能图标")]
        public Sprite Icon;

        [Tooltip("技能动画触发器名称")]
        public string AnimationTrigger;

        [Tooltip("技能音效")]
        public AudioClip SoundEffect;

        [Tooltip("技能特效预制体")]
        public GameObject VfxPrefab;

        /// <summary>
        /// 获取技能标签容器
        /// </summary>
        public GameplayTagContainer GetAbilityTagContainer()
        {
            return GameplayTagContainer.FromStrings(AbilityTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取要求的拥有者标签容器
        /// </summary>
        public GameplayTagContainer GetRequiredOwnerTagContainer()
        {
            return GameplayTagContainer.FromStrings(RequiredOwnerTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取阻止的拥有者标签容器
        /// </summary>
        public GameplayTagContainer GetBlockedOwnerTagContainer()
        {
            return GameplayTagContainer.FromStrings(BlockedOwnerTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取要求的目标标签容器
        /// </summary>
        public GameplayTagContainer GetRequiredTargetTagContainer()
        {
            return GameplayTagContainer.FromStrings(RequiredTargetTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取阻止的目标标签容器
        /// </summary>
        public GameplayTagContainer GetBlockedTargetTagContainer()
        {
            return GameplayTagContainer.FromStrings(BlockedTargetTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取激活时授予的标签容器
        /// </summary>
        public GameplayTagContainer GetActivationGrantedTagContainer()
        {
            return GameplayTagContainer.FromStrings(ActivationGrantedTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 检查拥有者是否满足激活条件
        /// </summary>
        public bool CanOwnerActivate(GameplayTagContainer ownerTags)
        {
            // 检查必需标签
            var required = GetRequiredOwnerTagContainer();
            if (!required.IsEmpty && !ownerTags.HasAllWithHierarchy(required))
            {
                return false;
            }

            // 检查阻止标签
            var blocked = GetBlockedOwnerTagContainer();
            if (!blocked.IsEmpty && ownerTags.HasAnyWithHierarchy(blocked))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查目标是否有效
        /// </summary>
        public bool IsValidTarget(GameplayTagContainer targetTags)
        {
            if (TargetType == AbilityTargetType.None || TargetType == AbilityTargetType.Self)
            {
                return true;
            }

            // 检查必需标签
            var required = GetRequiredTargetTagContainer();
            if (!required.IsEmpty && !targetTags.HasAllWithHierarchy(required))
            {
                return false;
            }

            // 检查阻止标签
            var blocked = GetBlockedTargetTagContainer();
            if (!blocked.IsEmpty && targetTags.HasAnyWithHierarchy(blocked))
            {
                return false;
            }

            return true;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(AbilityId))
            {
                AbilityId = name;
            }
        }
    }
}
