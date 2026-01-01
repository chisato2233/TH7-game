using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 效果持续时间类型
    /// </summary>
    public enum EffectDurationType
    {
        /// <summary>
        /// 即时效果 - 立即应用并结束
        /// </summary>
        Instant,

        /// <summary>
        /// 持续效果 - 持续一段时间
        /// </summary>
        Duration,

        /// <summary>
        /// 永久效果 - 直到被移除
        /// </summary>
        Infinite,

        /// <summary>
        /// 周期效果 - 周期性触发
        /// </summary>
        Periodic
    }

    /// <summary>
    /// 效果堆叠策略
    /// </summary>
    public enum EffectStackingPolicy
    {
        /// <summary>
        /// 不堆叠 - 忽略新效果
        /// </summary>
        None,

        /// <summary>
        /// 堆叠 - 增加堆叠层数
        /// </summary>
        Stack,

        /// <summary>
        /// 刷新 - 重置持续时间
        /// </summary>
        Refresh,

        /// <summary>
        /// 覆盖 - 用新效果替换旧效果
        /// </summary>
        Override
    }

    /// <summary>
    /// Gameplay 效果配置 (ScriptableObject)
    /// 定义效果的属性修改、标签要求、持续时间等
    /// 使用字符串标识属性，完全通用化
    /// </summary>
    [CreateAssetMenu(fileName = "NewEffect", menuName = "TH7/Ability/Gameplay Effect")]
    public class GameplayEffect : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("效果唯一ID")]
        public string EffectId;

        [Tooltip("效果显示名称")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("效果描述")]
        public string Description;

        [Header("持续时间")]
        [Tooltip("持续时间类型")]
        public EffectDurationType DurationType = EffectDurationType.Instant;

        [Tooltip("持续时间（秒），仅对 Duration 类型有效")]
        public float Duration = 5f;

        [Tooltip("周期间隔（秒），仅对 Periodic 类型有效")]
        public float Period = 1f;

        [Header("堆叠")]
        [Tooltip("堆叠策略")]
        public EffectStackingPolicy StackingPolicy = EffectStackingPolicy.None;

        [Tooltip("最大堆叠层数")]
        [Min(1)]
        public int MaxStacks = 1;

        [Tooltip("每层堆叠的效果倍率")]
        public float StackMultiplier = 1f;

        [Header("属性修改")]
        [Tooltip("应用的属性修改器列表")]
        public EffectModifierEntry[] Modifiers;

        [Header("标签")]
        [Tooltip("效果携带的标签（激活时添加到目标）")]
        public string[] GrantedTags;

        [Tooltip("目标必须拥有的标签")]
        public string[] RequiredTags;

        [Tooltip("目标不能拥有的标签（阻止应用）")]
        public string[] BlockedTags;

        [Tooltip("应用时移除带有这些标签的其他效果")]
        public string[] RemoveEffectsWithTags;

        [Header("视觉效果")]
        [Tooltip("效果图标")]
        public Sprite Icon;

        [Tooltip("粒子效果预制体")]
        public GameObject VfxPrefab;

        /// <summary>
        /// 获取授予的标签容器
        /// </summary>
        public GameplayTagContainer GetGrantedTagContainer()
        {
            return GameplayTagContainer.FromStrings(GrantedTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取要求的标签容器
        /// </summary>
        public GameplayTagContainer GetRequiredTagContainer()
        {
            return GameplayTagContainer.FromStrings(RequiredTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取阻止的标签容器
        /// </summary>
        public GameplayTagContainer GetBlockedTagContainer()
        {
            return GameplayTagContainer.FromStrings(BlockedTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 获取要移除的效果标签容器
        /// </summary>
        public GameplayTagContainer GetRemoveEffectsTagContainer()
        {
            return GameplayTagContainer.FromStrings(RemoveEffectsWithTags ?? Array.Empty<string>());
        }

        /// <summary>
        /// 检查是否可以应用到目标
        /// </summary>
        public bool CanApplyTo(GameplayTagContainer targetTags)
        {
            // 检查必需标签
            var required = GetRequiredTagContainer();
            if (!required.IsEmpty && !targetTags.HasAllWithHierarchy(required))
            {
                return false;
            }

            // 检查阻止标签
            var blocked = GetBlockedTagContainer();
            if (!blocked.IsEmpty && targetTags.HasAnyWithHierarchy(blocked))
            {
                return false;
            }

            return true;
        }

        void OnValidate()
        {
            if (string.IsNullOrEmpty(EffectId))
            {
                EffectId = name;
            }
        }
    }

    /// <summary>
    /// 效果修改器条目（用于 Inspector 序列化）
    /// 使用字符串标识属性，完全通用化
    /// </summary>
    [Serializable]
    public struct EffectModifierEntry
    {
        [Tooltip("要修改的属性名称")]
        public string Attribute;

        [Tooltip("运算类型")]
        public ModifierOp Operation;

        [Tooltip("修改值")]
        public float Value;

        /// <summary>
        /// 转换为 AttributeModifier
        /// </summary>
        public AttributeModifier ToModifier(object source)
        {
            return new AttributeModifier(Attribute, Operation, Value, source);
        }

        /// <summary>
        /// 检查是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Attribute);
    }
}
