using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 技能数据库 - 全局技能和效果注册表
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityDatabase", menuName = "TH7/Ability/Ability Database")]
    public class AbilityDatabase : ScriptableObject
    {
        [Header("技能配置")]
        [Tooltip("所有技能配置")]
        public GameplayAbility[] Abilities;

        [Header("效果配置")]
        [Tooltip("所有效果配置")]
        public GameplayEffect[] Effects;

        /// <summary>
        /// 技能ID到技能的映射（运行时缓存）
        /// </summary>
        Dictionary<string, GameplayAbility> abilityLookup;

        /// <summary>
        /// 效果ID到效果的映射（运行时缓存）
        /// </summary>
        Dictionary<string, GameplayEffect> effectLookup;

        /// <summary>
        /// 初始化缓存
        /// </summary>
        void BuildCache()
        {
            if (abilityLookup == null)
            {
                abilityLookup = new Dictionary<string, GameplayAbility>();
                if (Abilities != null)
                {
                    foreach (var ability in Abilities)
                    {
                        if (ability != null && !string.IsNullOrEmpty(ability.AbilityId))
                        {
                            abilityLookup[ability.AbilityId] = ability;
                        }
                    }
                }
            }

            if (effectLookup == null)
            {
                effectLookup = new Dictionary<string, GameplayEffect>();
                if (Effects != null)
                {
                    foreach (var effect in Effects)
                    {
                        if (effect != null && !string.IsNullOrEmpty(effect.EffectId))
                        {
                            effectLookup[effect.EffectId] = effect;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取技能
        /// </summary>
        public GameplayAbility GetAbility(string abilityId)
        {
            BuildCache();
            return abilityLookup.TryGetValue(abilityId, out var ability) ? ability : null;
        }

        /// <summary>
        /// 获取效果
        /// </summary>
        public GameplayEffect GetEffect(string effectId)
        {
            BuildCache();
            return effectLookup.TryGetValue(effectId, out var effect) ? effect : null;
        }

        /// <summary>
        /// 检查是否存在技能
        /// </summary>
        public bool HasAbility(string abilityId)
        {
            BuildCache();
            return abilityLookup.ContainsKey(abilityId);
        }

        /// <summary>
        /// 检查是否存在效果
        /// </summary>
        public bool HasEffect(string effectId)
        {
            BuildCache();
            return effectLookup.ContainsKey(effectId);
        }

        /// <summary>
        /// 获取所有技能ID
        /// </summary>
        public IEnumerable<string> GetAllAbilityIds()
        {
            BuildCache();
            return abilityLookup.Keys;
        }

        /// <summary>
        /// 获取所有效果ID
        /// </summary>
        public IEnumerable<string> GetAllEffectIds()
        {
            BuildCache();
            return effectLookup.Keys;
        }

        /// <summary>
        /// 按类型获取技能
        /// </summary>
        public List<GameplayAbility> GetAbilitiesByType(AbilityType type)
        {
            var result = new List<GameplayAbility>();
            if (Abilities == null) return result;

            foreach (var ability in Abilities)
            {
                if (ability != null && ability.Type == type)
                {
                    result.Add(ability);
                }
            }
            return result;
        }

        /// <summary>
        /// 按标签获取技能
        /// </summary>
        public List<GameplayAbility> GetAbilitiesWithTag(GameplayTag tag)
        {
            var result = new List<GameplayAbility>();
            if (Abilities == null) return result;

            foreach (var ability in Abilities)
            {
                if (ability != null)
                {
                    var tags = ability.GetAbilityTagContainer();
                    if (tags.HasTagOrParent(tag))
                    {
                        result.Add(ability);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 按持续时间类型获取效果
        /// </summary>
        public List<GameplayEffect> GetEffectsByDurationType(EffectDurationType durationType)
        {
            var result = new List<GameplayEffect>();
            if (Effects == null) return result;

            foreach (var effect in Effects)
            {
                if (effect != null && effect.DurationType == durationType)
                {
                    result.Add(effect);
                }
            }
            return result;
        }

        /// <summary>
        /// 重建缓存（编辑器中修改后调用）
        /// </summary>
        public void RebuildCache()
        {
            abilityLookup = null;
            effectLookup = null;
            BuildCache();
        }

        void OnValidate()
        {
            // 编辑器中修改时清空缓存
            abilityLookup = null;
            effectLookup = null;
        }
    }
}
