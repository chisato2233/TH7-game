using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 技能激活结果
    /// </summary>
    public enum AbilityActivationResult
    {
        Success,
        OnCooldown,
        NotEnoughMana,
        OwnerTagsBlocked,
        OwnerTagsMissing,
        InvalidTarget,
        NoUsesRemaining,
        AbilityNotReady,
    }

    /// <summary>
    /// 技能实例 - 运行时技能状态
    /// </summary>
    public class AbilityInstance
    {
        /// <summary>
        /// 技能配置
        /// </summary>
        public GameplayAbility Ability { get; }

        /// <summary>
        /// 技能拥有者
        /// </summary>
        public AbilitySystemComponent Owner { get; }

        /// <summary>
        /// 当前冷却回合数
        /// </summary>
        public int CurrentCooldown { get; private set; }

        /// <summary>
        /// 本场战斗剩余使用次数
        /// </summary>
        public int RemainingUses { get; private set; }

        /// <summary>
        /// 技能是否激活中（用于持续性技能）
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 被动效果实例列表
        /// </summary>
        readonly List<EffectInstance> passiveEffectInstances = new();

        /// <summary>
        /// 激活时授予的标签
        /// </summary>
        readonly GameplayTagContainer activationTags = new();

        /// <summary>
        /// 技能激活事件
        /// </summary>
        public event Action<AbilityInstance, object> OnActivated;

        /// <summary>
        /// 技能结束事件
        /// </summary>
        public event Action<AbilityInstance> OnEnded;

        /// <summary>
        /// 冷却变化事件
        /// </summary>
        public event Action<AbilityInstance, int> OnCooldownChanged;

        public AbilityInstance(GameplayAbility ability, AbilitySystemComponent owner)
        {
            Ability = ability ?? throw new ArgumentNullException(nameof(ability));
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            CurrentCooldown = 0;
            RemainingUses = ability.UsesPerBattle > 0 ? ability.UsesPerBattle : int.MaxValue;
            IsActive = false;
        }

        /// <summary>
        /// 初始化技能（授予被动效果）
        /// </summary>
        public void Initialize()
        {
            if (Ability.Type == AbilityType.Passive)
            {
                ApplyPassiveEffects();
            }
        }

        /// <summary>
        /// 清理技能（移除被动效果）
        /// </summary>
        public void Cleanup()
        {
            RemovePassiveEffects();
            if (IsActive)
            {
                EndAbility();
            }
        }

        /// <summary>
        /// 检查技能是否可以激活
        /// </summary>
        public AbilityActivationResult CanActivate(AbilitySystemComponent target = null)
        {
            // 被动技能不需要激活
            if (Ability.Type == AbilityType.Passive)
            {
                return AbilityActivationResult.AbilityNotReady;
            }

            // 检查冷却
            if (CurrentCooldown > 0)
            {
                return AbilityActivationResult.OnCooldown;
            }

            // 检查使用次数
            if (RemainingUses <= 0)
            {
                return AbilityActivationResult.NoUsesRemaining;
            }

            // 检查魔法值（使用通用属性名 "Mana"）
            if (Ability.ManaCost > 0)
            {
                float currentMana = Owner.Attributes.GetCurrentValue(Ability.ManaAttribute);
                if (currentMana < Ability.ManaCost)
                {
                    return AbilityActivationResult.NotEnoughMana;
                }
            }

            // 检查拥有者标签
            if (!Ability.CanOwnerActivate(Owner.Tags))
            {
                var required = Ability.GetRequiredOwnerTagContainer();
                if (!required.IsEmpty && !Owner.Tags.HasAllWithHierarchy(required))
                {
                    return AbilityActivationResult.OwnerTagsMissing;
                }
                return AbilityActivationResult.OwnerTagsBlocked;
            }

            // 检查目标（如果需要）
            if (target != null && !Ability.IsValidTarget(target.Tags))
            {
                return AbilityActivationResult.InvalidTarget;
            }

            return AbilityActivationResult.Success;
        }

        /// <summary>
        /// 激活技能
        /// </summary>
        public AbilityActivationResult Activate(AbilitySystemComponent target = null)
        {
            var result = CanActivate(target);
            if (result != AbilityActivationResult.Success)
            {
                return result;
            }

            // 消耗魔法
            if (Ability.ManaCost > 0)
            {
                float currentMana = Owner.Attributes.GetBaseValue(Ability.ManaAttribute);
                Owner.Attributes.SetBaseValue(Ability.ManaAttribute, currentMana - Ability.ManaCost);
            }

            // 消耗使用次数
            if (Ability.UsesPerBattle > 0)
            {
                RemainingUses--;
            }

            // 进入冷却
            if (Ability.Cooldown > 0)
            {
                CurrentCooldown = Ability.Cooldown;
                OnCooldownChanged?.Invoke(this, CurrentCooldown);
            }

            // 添加激活标签
            ApplyActivationTags();

            IsActive = true;

            // 应用效果到目标
            ApplyEffectsToTarget(target);

            // 应用效果到自身
            ApplySelfEffects();

            OnActivated?.Invoke(this, target);

            // 如果没有持续效果，立即结束
            if (!HasDurationEffects())
            {
                EndAbility();
            }

            return AbilityActivationResult.Success;
        }

        /// <summary>
        /// 结束技能
        /// </summary>
        public void EndAbility()
        {
            if (!IsActive) return;

            IsActive = false;
            RemoveActivationTags();
            OnEnded?.Invoke(this);
        }

        /// <summary>
        /// 减少冷却（每回合调用）
        /// </summary>
        public void TickCooldown()
        {
            if (CurrentCooldown > 0)
            {
                CurrentCooldown--;
                OnCooldownChanged?.Invoke(this, CurrentCooldown);
            }
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            CurrentCooldown = 0;
            OnCooldownChanged?.Invoke(this, 0);
        }

        /// <summary>
        /// 重置使用次数（新战斗开始时调用）
        /// </summary>
        public void ResetUses()
        {
            RemainingUses = Ability.UsesPerBattle > 0 ? Ability.UsesPerBattle : int.MaxValue;
        }

        /// <summary>
        /// 应用被动效果
        /// </summary>
        void ApplyPassiveEffects()
        {
            if (Ability.PassiveEffects == null) return;

            foreach (var effect in Ability.PassiveEffects)
            {
                if (effect == null) continue;

                var instance = Owner.ApplyEffectToSelf(effect, this);
                if (instance != null)
                {
                    passiveEffectInstances.Add(instance);
                }
            }
        }

        /// <summary>
        /// 移除被动效果
        /// </summary>
        void RemovePassiveEffects()
        {
            foreach (var instance in passiveEffectInstances)
            {
                Owner.RemoveEffect(instance);
            }
            passiveEffectInstances.Clear();
        }

        /// <summary>
        /// 应用效果到目标
        /// </summary>
        void ApplyEffectsToTarget(AbilitySystemComponent target)
        {
            if (Ability.EffectsToApply == null || target == null) return;

            foreach (var effect in Ability.EffectsToApply)
            {
                if (effect == null) continue;
                target.ApplyEffect(effect, this);
            }
        }

        /// <summary>
        /// 应用效果到自身
        /// </summary>
        void ApplySelfEffects()
        {
            if (Ability.SelfEffects == null) return;

            foreach (var effect in Ability.SelfEffects)
            {
                if (effect == null) continue;
                Owner.ApplyEffectToSelf(effect, this);
            }
        }

        /// <summary>
        /// 应用激活标签
        /// </summary>
        void ApplyActivationTags()
        {
            if (Ability.ActivationGrantedTags == null) return;

            foreach (var tagStr in Ability.ActivationGrantedTags)
            {
                var tag = new GameplayTag(tagStr);
                Owner.Tags.AddTag(tag);
                activationTags.AddTag(tag);
            }
        }

        /// <summary>
        /// 移除激活标签
        /// </summary>
        void RemoveActivationTags()
        {
            foreach (var tag in activationTags)
            {
                Owner.Tags.RemoveTag(tag);
            }
            activationTags.Clear();
        }

        /// <summary>
        /// 检查是否有持续效果
        /// </summary>
        bool HasDurationEffects()
        {
            if (Ability.EffectsToApply != null)
            {
                foreach (var effect in Ability.EffectsToApply)
                {
                    if (effect != null && effect.DurationType != EffectDurationType.Instant)
                        return true;
                }
            }

            if (Ability.SelfEffects != null)
            {
                foreach (var effect in Ability.SelfEffects)
                {
                    if (effect != null && effect.DurationType != EffectDurationType.Instant)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查触发条件是否匹配
        /// </summary>
        public bool MatchesTrigger(AbilityTrigger trigger)
        {
            return Ability.Type == AbilityType.Triggered && Ability.Trigger == trigger;
        }

        /// <summary>
        /// 获取技能描述
        /// </summary>
        public string GetDescription()
        {
            string desc = Ability.Description ?? Ability.DisplayName ?? Ability.AbilityId;

            if (Ability.ManaCost > 0)
            {
                desc += $"\nMana: {Ability.ManaCost}";
            }

            if (CurrentCooldown > 0)
            {
                desc += $"\nCooldown: {CurrentCooldown}";
            }

            return desc;
        }

        public override string ToString()
        {
            return $"[AbilityInstance: {Ability?.AbilityId ?? "null"} CD:{CurrentCooldown}]";
        }
    }
}
