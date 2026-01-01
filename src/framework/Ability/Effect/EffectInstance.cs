using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 效果实例 - 运行时效果状态
    /// </summary>
    public class EffectInstance
    {
        /// <summary>
        /// 效果配置
        /// </summary>
        public GameplayEffect Effect { get; }

        /// <summary>
        /// 效果来源（施法者/物品等）
        /// </summary>
        public object Source { get; }

        /// <summary>
        /// 效果目标
        /// </summary>
        public AbilitySystemComponent Target { get; }

        /// <summary>
        /// 剩余持续时间（秒）
        /// </summary>
        public float RemainingDuration { get; private set; }

        /// <summary>
        /// 周期计时器（秒）
        /// </summary>
        public float PeriodTimer { get; private set; }

        /// <summary>
        /// 当前堆叠层数
        /// </summary>
        public int CurrentStacks { get; private set; }

        /// <summary>
        /// 效果是否已过期
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (Effect.DurationType == EffectDurationType.Instant)
                    return true;
                if (Effect.DurationType == EffectDurationType.Infinite)
                    return false;
                return RemainingDuration <= 0;
            }
        }

        /// <summary>
        /// 效果是否激活
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 已应用的修改器列表（用于移除时清理）
        /// </summary>
        readonly List<AttributeModifier> appliedModifiers = new();

        /// <summary>
        /// 已授予的标签
        /// </summary>
        readonly GameplayTagContainer grantedTags = new();

        /// <summary>
        /// 周期触发事件
        /// </summary>
        public event Action<EffectInstance> OnPeriodTrigger;

        /// <summary>
        /// 堆叠变化事件
        /// </summary>
        public event Action<EffectInstance, int, int> OnStackChanged;

        public EffectInstance(GameplayEffect effect, object source, AbilitySystemComponent target)
        {
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
            Source = source;
            Target = target;

            RemainingDuration = effect.Duration;
            PeriodTimer = effect.Period;
            CurrentStacks = 1;
            IsActive = false;
        }

        /// <summary>
        /// 激活效果 - 应用修改器和标签
        /// </summary>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;

            ApplyModifiers();
            ApplyGrantedTags();
        }

        /// <summary>
        /// 停用效果 - 移除修改器和标签
        /// </summary>
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;

            RemoveModifiers();
            RemoveGrantedTags();
        }

        /// <summary>
        /// 更新效果（每帧调用）
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>效果是否仍然有效</returns>
        public bool Tick(float deltaTime)
        {
            if (!IsActive || Effect.DurationType == EffectDurationType.Instant)
                return false;

            // 更新持续时间
            if (Effect.DurationType == EffectDurationType.Duration ||
                Effect.DurationType == EffectDurationType.Periodic)
            {
                RemainingDuration -= deltaTime;
            }

            // 处理周期效果
            if (Effect.DurationType == EffectDurationType.Periodic)
            {
                PeriodTimer -= deltaTime;
                if (PeriodTimer <= 0)
                {
                    TriggerPeriodic();
                    PeriodTimer = Effect.Period;
                }
            }

            return !IsExpired;
        }

        /// <summary>
        /// 触发周期效果
        /// </summary>
        void TriggerPeriodic()
        {
            OnPeriodTrigger?.Invoke(this);

            // 重新应用即时效果的修改（如周期性伤害/治疗）
            if (Target != null && Effect.Modifiers != null)
            {
                foreach (var modEntry in Effect.Modifiers)
                {
                    if (modEntry.Operation == ModifierOp.Add)
                    {
                        // 对于加法类型，直接修改基础值
                        float currentBase = Target.Attributes.GetBaseValue(modEntry.Attribute);
                        float delta = modEntry.Value * CurrentStacks;
                        Target.Attributes.SetBaseValue(modEntry.Attribute, currentBase + delta);
                    }
                }
            }
        }

        /// <summary>
        /// 增加堆叠层数
        /// </summary>
        public bool AddStack()
        {
            if (CurrentStacks >= Effect.MaxStacks)
                return false;

            int oldStacks = CurrentStacks;
            CurrentStacks++;

            // 重新计算修改器
            RefreshModifiers();

            OnStackChanged?.Invoke(this, oldStacks, CurrentStacks);
            return true;
        }

        /// <summary>
        /// 减少堆叠层数
        /// </summary>
        public void RemoveStack()
        {
            if (CurrentStacks <= 0) return;

            int oldStacks = CurrentStacks;
            CurrentStacks--;

            if (CurrentStacks > 0)
            {
                RefreshModifiers();
            }

            OnStackChanged?.Invoke(this, oldStacks, CurrentStacks);
        }

        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void RefreshDuration()
        {
            RemainingDuration = Effect.Duration;
        }

        /// <summary>
        /// 应用属性修改器
        /// </summary>
        void ApplyModifiers()
        {
            if (Target == null || Effect.Modifiers == null) return;

            float stackMultiplier = 1f + (CurrentStacks - 1) * Effect.StackMultiplier;

            foreach (var modEntry in Effect.Modifiers)
            {
                // 即时效果直接修改基础值
                if (Effect.DurationType == EffectDurationType.Instant)
                {
                    if (modEntry.Operation == ModifierOp.Add)
                    {
                        float currentBase = Target.Attributes.GetBaseValue(modEntry.Attribute);
                        Target.Attributes.SetBaseValue(modEntry.Attribute, currentBase + modEntry.Value * stackMultiplier);
                    }
                    else if (modEntry.Operation == ModifierOp.Override)
                    {
                        Target.Attributes.SetBaseValue(modEntry.Attribute, modEntry.Value);
                    }
                    // 乘法对于即时效果意义不大，跳过
                }
                else
                {
                    // 持续效果添加修改器
                    var modifier = new AttributeModifier(
                        modEntry.Attribute,
                        modEntry.Operation,
                        modEntry.Operation == ModifierOp.Add
                            ? modEntry.Value * stackMultiplier
                            : modEntry.Value,
                        this
                    );
                    Target.Attributes.AddModifier(modifier);
                    appliedModifiers.Add(modifier);
                }
            }
        }

        /// <summary>
        /// 移除属性修改器
        /// </summary>
        void RemoveModifiers()
        {
            if (Target == null) return;

            foreach (var modifier in appliedModifiers)
            {
                Target.Attributes.RemoveModifier(modifier);
            }
            appliedModifiers.Clear();
        }

        /// <summary>
        /// 刷新修改器（堆叠变化时）
        /// </summary>
        void RefreshModifiers()
        {
            RemoveModifiers();
            ApplyModifiers();
        }

        /// <summary>
        /// 应用授予的标签
        /// </summary>
        void ApplyGrantedTags()
        {
            if (Target == null || Effect.GrantedTags == null) return;

            foreach (var tagStr in Effect.GrantedTags)
            {
                var tag = new GameplayTag(tagStr);
                Target.Tags.AddTag(tag);
                grantedTags.AddTag(tag);
            }
        }

        /// <summary>
        /// 移除授予的标签
        /// </summary>
        void RemoveGrantedTags()
        {
            if (Target == null) return;

            foreach (var tag in grantedTags)
            {
                Target.Tags.RemoveTag(tag);
            }
            grantedTags.Clear();
        }

        /// <summary>
        /// 获取效果描述
        /// </summary>
        public string GetDescription()
        {
            string desc = Effect.Description ?? Effect.DisplayName ?? Effect.EffectId;

            if (Effect.MaxStacks > 1)
            {
                desc += $" (x{CurrentStacks}/{Effect.MaxStacks})";
            }

            if (Effect.DurationType == EffectDurationType.Duration ||
                Effect.DurationType == EffectDurationType.Periodic)
            {
                desc += $" [{RemainingDuration:F1}s]";
            }

            return desc;
        }

        public override string ToString()
        {
            return $"[EffectInstance: {Effect?.EffectId ?? "null"} x{CurrentStacks}]";
        }
    }
}
