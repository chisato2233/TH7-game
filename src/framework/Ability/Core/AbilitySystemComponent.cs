using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 技能系统组件 - 管理实体的属性、效果和技能
    /// 这是 GAS 的核心组件，每个需要技能系统的实体都应该拥有一个
    /// 完全通用化，使用字符串标识属性
    /// </summary>
    [Serializable]
    public class AbilitySystemComponent
    {
        /// <summary>
        /// 属性集合
        /// </summary>
        public AttributeSet Attributes { get; }

        /// <summary>
        /// 标签容器
        /// </summary>
        public GameplayTagContainer Tags { get; }

        /// <summary>
        /// 活跃的效果实例
        /// </summary>
        readonly List<EffectInstance> activeEffects = new();

        /// <summary>
        /// 已授予的技能实例
        /// </summary>
        readonly List<AbilityInstance> grantedAbilities = new();

        /// <summary>
        /// 效果应用事件
        /// </summary>
        public event Action<EffectInstance> OnEffectApplied;

        /// <summary>
        /// 效果移除事件
        /// </summary>
        public event Action<EffectInstance> OnEffectRemoved;

        /// <summary>
        /// 技能激活事件
        /// </summary>
        public event Action<AbilityInstance, object> OnAbilityActivated;

        /// <summary>
        /// 技能结束事件
        /// </summary>
        public event Action<AbilityInstance> OnAbilityEnded;

        /// <summary>
        /// 标签变化事件
        /// </summary>
        public event Action<GameplayTag, bool> OnTagChanged;

        public AbilitySystemComponent()
        {
            Attributes = new AttributeSet();
            Tags = new GameplayTagContainer();
        }

        #region Effect Management

        /// <summary>
        /// 应用效果到自身
        /// </summary>
        public EffectInstance ApplyEffectToSelf(GameplayEffect effect, object source)
        {
            return ApplyEffect(effect, source, this);
        }

        /// <summary>
        /// 应用效果
        /// </summary>
        public EffectInstance ApplyEffect(GameplayEffect effect, object source, AbilitySystemComponent target = null)
        {
            target ??= this;

            if (effect == null) return null;

            // 检查标签条件
            if (!effect.CanApplyTo(target.Tags))
            {
                return null;
            }

            // 处理移除带有特定标签的效果
            var removeTagContainer = effect.GetRemoveEffectsTagContainer();
            if (!removeTagContainer.IsEmpty)
            {
                target.RemoveEffectsWithTags(removeTagContainer);
            }

            // 检查堆叠
            var existingInstance = target.FindExistingEffect(effect);
            if (existingInstance != null)
            {
                return HandleStacking(existingInstance, effect, source);
            }

            // 创建新实例
            var instance = new EffectInstance(effect, source, target);

            if (effect.DurationType == EffectDurationType.Instant)
            {
                // 即时效果立即应用并结束
                instance.Activate();
                OnEffectApplied?.Invoke(instance);
                return instance;
            }

            // 持续效果添加到活跃列表
            target.activeEffects.Add(instance);
            instance.Activate();
            OnEffectApplied?.Invoke(instance);

            return instance;
        }

        /// <summary>
        /// 查找已存在的同类效果
        /// </summary>
        EffectInstance FindExistingEffect(GameplayEffect effect)
        {
            foreach (var instance in activeEffects)
            {
                if (instance.Effect == effect || instance.Effect.EffectId == effect.EffectId)
                {
                    return instance;
                }
            }
            return null;
        }

        /// <summary>
        /// 处理效果堆叠
        /// </summary>
        EffectInstance HandleStacking(EffectInstance existing, GameplayEffect effect, object source)
        {
            switch (effect.StackingPolicy)
            {
                case EffectStackingPolicy.None:
                    // 不堆叠，忽略新效果
                    return existing;

                case EffectStackingPolicy.Stack:
                    // 增加堆叠层数
                    existing.AddStack();
                    return existing;

                case EffectStackingPolicy.Refresh:
                    // 刷新持续时间
                    existing.RefreshDuration();
                    return existing;

                case EffectStackingPolicy.Override:
                    // 移除旧效果，应用新效果
                    RemoveEffect(existing);
                    return ApplyEffect(effect, source);

                default:
                    return existing;
            }
        }

        /// <summary>
        /// 移除效果
        /// </summary>
        public void RemoveEffect(EffectInstance instance)
        {
            if (instance == null) return;

            instance.Deactivate();
            activeEffects.Remove(instance);
            OnEffectRemoved?.Invoke(instance);
        }

        /// <summary>
        /// 移除所有来自指定源的效果
        /// </summary>
        public int RemoveEffectsFromSource(object source)
        {
            if (source == null) return 0;

            var toRemove = new List<EffectInstance>();
            foreach (var instance in activeEffects)
            {
                if (ReferenceEquals(instance.Source, source))
                {
                    toRemove.Add(instance);
                }
            }

            foreach (var instance in toRemove)
            {
                RemoveEffect(instance);
            }

            return toRemove.Count;
        }

        /// <summary>
        /// 移除带有指定标签的效果
        /// </summary>
        public int RemoveEffectsWithTag(GameplayTag tag)
        {
            var container = new GameplayTagContainer();
            container.AddTag(tag);
            return RemoveEffectsWithTags(container);
        }

        /// <summary>
        /// 移除带有任一指定标签的效果
        /// </summary>
        public int RemoveEffectsWithTags(GameplayTagContainer tags)
        {
            if (tags == null || tags.IsEmpty) return 0;

            var toRemove = new List<EffectInstance>();
            foreach (var instance in activeEffects)
            {
                var effectTags = instance.Effect.GetGrantedTagContainer();
                if (effectTags.HasAny(tags))
                {
                    toRemove.Add(instance);
                }
            }

            foreach (var instance in toRemove)
            {
                RemoveEffect(instance);
            }

            return toRemove.Count;
        }

        /// <summary>
        /// 检查是否有带指定标签的效果
        /// </summary>
        public bool HasEffectWithTag(GameplayTag tag)
        {
            foreach (var instance in activeEffects)
            {
                var effectTags = instance.Effect.GetGrantedTagContainer();
                if (effectTags.HasTag(tag))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有活跃效果
        /// </summary>
        public IReadOnlyList<EffectInstance> GetActiveEffects() => activeEffects;

        /// <summary>
        /// 清除所有效果
        /// </summary>
        public void ClearAllEffects()
        {
            var allEffects = new List<EffectInstance>(activeEffects);
            foreach (var instance in allEffects)
            {
                RemoveEffect(instance);
            }
        }

        #endregion

        #region Ability Management

        /// <summary>
        /// 授予技能
        /// </summary>
        public AbilityInstance GrantAbility(GameplayAbility ability)
        {
            if (ability == null) return null;

            // 检查是否已拥有
            if (HasAbility(ability.AbilityId))
            {
                return GetAbilityInstance(ability.AbilityId);
            }

            var instance = new AbilityInstance(ability, this);
            instance.OnActivated += HandleAbilityActivated;
            instance.OnEnded += HandleAbilityEnded;

            grantedAbilities.Add(instance);
            instance.Initialize();

            return instance;
        }

        /// <summary>
        /// 移除技能
        /// </summary>
        public bool RemoveAbility(string abilityId)
        {
            var instance = GetAbilityInstance(abilityId);
            if (instance == null) return false;

            instance.Cleanup();
            instance.OnActivated -= HandleAbilityActivated;
            instance.OnEnded -= HandleAbilityEnded;
            grantedAbilities.Remove(instance);

            return true;
        }

        /// <summary>
        /// 检查是否拥有技能
        /// </summary>
        public bool HasAbility(string abilityId)
        {
            return GetAbilityInstance(abilityId) != null;
        }

        /// <summary>
        /// 获取技能实例
        /// </summary>
        public AbilityInstance GetAbilityInstance(string abilityId)
        {
            foreach (var instance in grantedAbilities)
            {
                if (instance.Ability.AbilityId == abilityId)
                {
                    return instance;
                }
            }
            return null;
        }

        /// <summary>
        /// 尝试激活技能
        /// </summary>
        public AbilityActivationResult TryActivateAbility(string abilityId, AbilitySystemComponent target = null)
        {
            var instance = GetAbilityInstance(abilityId);
            if (instance == null)
            {
                return AbilityActivationResult.AbilityNotReady;
            }

            return instance.Activate(target);
        }

        /// <summary>
        /// 检查技能是否可以激活
        /// </summary>
        public AbilityActivationResult CanActivateAbility(string abilityId, AbilitySystemComponent target = null)
        {
            var instance = GetAbilityInstance(abilityId);
            if (instance == null)
            {
                return AbilityActivationResult.AbilityNotReady;
            }

            return instance.CanActivate(target);
        }

        /// <summary>
        /// 获取所有已授予的技能
        /// </summary>
        public IReadOnlyList<AbilityInstance> GetGrantedAbilities() => grantedAbilities;

        /// <summary>
        /// 获取指定类型的技能
        /// </summary>
        public List<AbilityInstance> GetAbilitiesByType(AbilityType type)
        {
            var result = new List<AbilityInstance>();
            foreach (var instance in grantedAbilities)
            {
                if (instance.Ability.Type == type)
                {
                    result.Add(instance);
                }
            }
            return result;
        }

        /// <summary>
        /// 触发指定类型的触发技能
        /// </summary>
        public void TriggerAbilities(AbilityTrigger trigger, AbilitySystemComponent target = null)
        {
            foreach (var instance in grantedAbilities)
            {
                if (instance.MatchesTrigger(trigger))
                {
                    instance.Activate(target);
                }
            }
        }

        #endregion

        #region Attribute Watch API

        /// <summary>
        /// 获取响应式属性
        /// </summary>
        public ReactiveAttribute GetAttribute(string name)
        {
            return Attributes.GetAttribute(name);
        }

        /// <summary>
        /// 监听属性变化
        /// </summary>
        public Subscription WatchAttribute(string name, Action<float> callback)
        {
            return Attributes.Watch(name, callback);
        }

        /// <summary>
        /// 监听属性变化（立即触发当前值）
        /// </summary>
        public Subscription WatchAttributeImmediate(string name, Action<float> callback)
        {
            return Attributes.WatchImmediate(name, callback);
        }

        /// <summary>
        /// 监听一次属性变化
        /// </summary>
        public Subscription WatchAttributeOnce(string name, Action<float> callback)
        {
            return Attributes.WatchOnce(name, callback);
        }

        #endregion

        #region Tag Convenience Methods

        /// <summary>
        /// 检查是否有标签
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            return Tags.HasTag(tag);
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(GameplayTag tag)
        {
            if (!Tags.HasTag(tag))
            {
                Tags.AddTag(tag);
                OnTagChanged?.Invoke(tag, true);
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public void RemoveTag(GameplayTag tag)
        {
            if (Tags.RemoveTag(tag))
            {
                OnTagChanged?.Invoke(tag, false);
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 更新效果
            var expiredEffects = new List<EffectInstance>();
            foreach (var instance in activeEffects)
            {
                if (!instance.Tick(deltaTime))
                {
                    expiredEffects.Add(instance);
                }
            }

            // 移除过期效果
            foreach (var instance in expiredEffects)
            {
                RemoveEffect(instance);
            }
        }

        /// <summary>
        /// 回合开始
        /// </summary>
        public void OnTurnStart()
        {
            // 触发回合开始技能
            TriggerAbilities(AbilityTrigger.OnTurnStart);
        }

        /// <summary>
        /// 回合结束
        /// </summary>
        public void OnTurnEnd()
        {
            // 触发回合结束技能
            TriggerAbilities(AbilityTrigger.OnTurnEnd);

            // 减少技能冷却
            foreach (var instance in grantedAbilities)
            {
                instance.TickCooldown();
            }
        }

        /// <summary>
        /// 战斗开始
        /// </summary>
        public void OnBattleStart()
        {
            // 重置技能使用次数
            foreach (var instance in grantedAbilities)
            {
                instance.ResetUses();
            }

            TriggerAbilities(AbilityTrigger.OnBattleStart);
        }

        /// <summary>
        /// 战斗结束
        /// </summary>
        public void OnBattleEnd()
        {
            TriggerAbilities(AbilityTrigger.OnBattleEnd);

            // 清除战斗临时效果
            ClearBattleEffects();
        }

        /// <summary>
        /// 清除战斗相关的临时效果
        /// </summary>
        void ClearBattleEffects()
        {
            var battleTag = new GameplayTag("Effect.Battle");
            RemoveEffectsWithTag(battleTag);
        }

        #endregion

        #region Event Handlers

        void HandleAbilityActivated(AbilityInstance ability, object target)
        {
            OnAbilityActivated?.Invoke(ability, target);
        }

        void HandleAbilityEnded(AbilityInstance ability)
        {
            OnAbilityEnded?.Invoke(ability);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 从配置初始化属性（字符串版本）
        /// </summary>
        public void InitializeFromConfig(params (string attribute, float value)[] attributes)
        {
            Attributes.InitializeAttributes(attributes);
        }

        /// <summary>
        /// 从字典初始化属性
        /// </summary>
        public void InitializeFromDictionary(Dictionary<string, float> attributes)
        {
            Attributes.InitializeFromDictionary(attributes);
        }

        /// <summary>
        /// 设置属性定义数据库
        /// </summary>
        public void SetAttributeDefinitions(AttributeDefinitionDatabase database)
        {
            Attributes.DefinitionDatabase = database;
        }

        /// <summary>
        /// 清理所有状态
        /// </summary>
        public void Cleanup()
        {
            // 清理技能
            foreach (var ability in grantedAbilities)
            {
                ability.Cleanup();
            }
            grantedAbilities.Clear();

            // 清理效果
            ClearAllEffects();

            // 清理属性修改器
            Attributes.ClearModifiers();

            // 清理标签
            Tags.Clear();
        }

        #endregion

        public override string ToString()
        {
            return $"[ASC: {grantedAbilities.Count} abilities, {activeEffects.Count} effects]";
        }
    }
}
