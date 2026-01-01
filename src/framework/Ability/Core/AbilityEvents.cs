namespace GameFramework
{
    /// <summary>
    /// 效果应用事件数据
    /// </summary>
    public struct EffectAppliedEvent
    {
        /// <summary>
        /// 效果目标
        /// </summary>
        public AbilitySystemComponent Target;

        /// <summary>
        /// 效果实例
        /// </summary>
        public EffectInstance Effect;

        /// <summary>
        /// 效果来源
        /// </summary>
        public object Source;

        public EffectAppliedEvent(AbilitySystemComponent target, EffectInstance effect, object source = null)
        {
            Target = target;
            Effect = effect;
            Source = source;
        }
    }

    /// <summary>
    /// 效果移除事件数据
    /// </summary>
    public struct EffectRemovedEvent
    {
        /// <summary>
        /// 效果目标
        /// </summary>
        public AbilitySystemComponent Target;

        /// <summary>
        /// 效果实例
        /// </summary>
        public EffectInstance Effect;

        /// <summary>
        /// 是否过期移除
        /// </summary>
        public bool Expired;

        public EffectRemovedEvent(AbilitySystemComponent target, EffectInstance effect, bool expired)
        {
            Target = target;
            Effect = effect;
            Expired = expired;
        }
    }

    /// <summary>
    /// 技能激活事件数据
    /// </summary>
    public struct AbilityActivatedEvent
    {
        /// <summary>
        /// 技能拥有者
        /// </summary>
        public AbilitySystemComponent Owner;

        /// <summary>
        /// 技能实例
        /// </summary>
        public AbilityInstance Ability;

        /// <summary>
        /// 技能目标
        /// </summary>
        public AbilitySystemComponent Target;

        public AbilityActivatedEvent(AbilitySystemComponent owner, AbilityInstance ability, AbilitySystemComponent target = null)
        {
            Owner = owner;
            Ability = ability;
            Target = target;
        }
    }

    /// <summary>
    /// 技能结束事件数据
    /// </summary>
    public struct AbilityEndedEvent
    {
        /// <summary>
        /// 技能拥有者
        /// </summary>
        public AbilitySystemComponent Owner;

        /// <summary>
        /// 技能实例
        /// </summary>
        public AbilityInstance Ability;

        public AbilityEndedEvent(AbilitySystemComponent owner, AbilityInstance ability)
        {
            Owner = owner;
            Ability = ability;
        }
    }

    /// <summary>
    /// 属性变化事件数据
    /// 使用字符串标识属性，完全通用化
    /// </summary>
    public struct AttributeChangedEvent
    {
        /// <summary>
        /// 属性拥有者
        /// </summary>
        public AbilitySystemComponent Owner;

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Attribute;

        /// <summary>
        /// 旧值
        /// </summary>
        public float OldValue;

        /// <summary>
        /// 新值
        /// </summary>
        public float NewValue;

        /// <summary>
        /// 变化量
        /// </summary>
        public float Delta => NewValue - OldValue;

        public AttributeChangedEvent(AbilitySystemComponent owner, string attribute, float oldValue, float newValue)
        {
            Owner = owner;
            Attribute = attribute;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 标签变化事件数据
    /// </summary>
    public struct TagChangedEvent
    {
        /// <summary>
        /// 标签拥有者
        /// </summary>
        public AbilitySystemComponent Owner;

        /// <summary>
        /// 变化的标签
        /// </summary>
        public GameplayTag Tag;

        /// <summary>
        /// 是否为添加（false 表示移除）
        /// </summary>
        public bool Added;

        public TagChangedEvent(AbilitySystemComponent owner, GameplayTag tag, bool added)
        {
            Owner = owner;
            Tag = tag;
            Added = added;
        }
    }

    /// <summary>
    /// 伤害事件数据
    /// </summary>
    public struct DamageEvent
    {
        /// <summary>
        /// 伤害来源
        /// </summary>
        public AbilitySystemComponent Source;

        /// <summary>
        /// 伤害目标
        /// </summary>
        public AbilitySystemComponent Target;

        /// <summary>
        /// 伤害值
        /// </summary>
        public float Damage;

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCritical;

        /// <summary>
        /// 伤害类型标签（可用于分类火焰/冰霜/物理等）
        /// </summary>
        public GameplayTag DamageType;

        /// <summary>
        /// 造成伤害的技能（可空）
        /// </summary>
        public AbilityInstance SourceAbility;

        /// <summary>
        /// 造成伤害的效果（可空）
        /// </summary>
        public EffectInstance SourceEffect;

        public DamageEvent(AbilitySystemComponent source, AbilitySystemComponent target, float damage,
            bool isCritical = false, GameplayTag damageType = default,
            AbilityInstance sourceAbility = null, EffectInstance sourceEffect = null)
        {
            Source = source;
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
            DamageType = damageType;
            SourceAbility = sourceAbility;
            SourceEffect = sourceEffect;
        }
    }

    /// <summary>
    /// 治疗事件数据
    /// </summary>
    public struct HealEvent
    {
        /// <summary>
        /// 治疗来源
        /// </summary>
        public AbilitySystemComponent Source;

        /// <summary>
        /// 治疗目标
        /// </summary>
        public AbilitySystemComponent Target;

        /// <summary>
        /// 治疗量
        /// </summary>
        public float Amount;

        /// <summary>
        /// 造成治疗的技能（可空）
        /// </summary>
        public AbilityInstance SourceAbility;

        public HealEvent(AbilitySystemComponent source, AbilitySystemComponent target, float amount,
            AbilityInstance sourceAbility = null)
        {
            Source = source;
            Target = target;
            Amount = amount;
            SourceAbility = sourceAbility;
        }
    }

    /// <summary>
    /// 死亡事件数据
    /// </summary>
    public struct DeathEvent
    {
        /// <summary>
        /// 死亡的实体
        /// </summary>
        public AbilitySystemComponent Victim;

        /// <summary>
        /// 击杀者（可空）
        /// </summary>
        public AbilitySystemComponent Killer;

        /// <summary>
        /// 最后一击的伤害事件
        /// </summary>
        public DamageEvent? FinalBlow;

        public DeathEvent(AbilitySystemComponent victim, AbilitySystemComponent killer = null,
            DamageEvent? finalBlow = null)
        {
            Victim = victim;
            Killer = killer;
            FinalBlow = finalBlow;
        }
    }
}
