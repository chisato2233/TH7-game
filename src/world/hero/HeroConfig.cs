using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "TH7/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string HeroId;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Portrait;
        public Sprite MapIcon;

        [Header("Faction")]
        public BiomeType Faction;
        public HeroClass Class;

        [Header("Base Stats")]
        [Tooltip("攻击力")]
        public int Attack = 1;
        [Tooltip("防御力")]
        public int Defense = 1;
        [Tooltip("魔法力")]
        public int SpellPower = 1;
        [Tooltip("知识（法力上限）")]
        public int Knowledge = 1;

        [Header("Movement")]
        [Tooltip("基础移动力")]
        public int BaseMovementPoints = 20;

        [Header("Specialization")]
        [Tooltip("专精技能ID")]
        public string SpecializationAbilityId;
        [Tooltip("初始技能ID列表")]
        public string[] StartingAbilityIds;

        [Header("Starting Army")]
        [Tooltip("初始军队配置")]
        public StartingUnit[] StartingArmy;

        [Header("Visuals")]
        public RuntimeAnimatorController AnimatorController;
        public GameObject Prefab;
    }

    /// <summary>
    /// 初始军队单位配置
    /// </summary>
    [System.Serializable]
    public struct StartingUnit
    {
        public string UnitId;
        [Range(1, 9999)]
        public int Count;
    }
}
