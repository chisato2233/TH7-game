using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 兵种配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "UnitConfig", menuName = "TH7/Unit Config")]
    public class UnitConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string UnitId;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;
        public BiomeType Faction;

        [Header("Tier")]
        public UnitTier Tier = UnitTier.Tier1;
        public bool IsUpgraded;
        public string BaseUnitId;           // 升级前的兵种 ID

        [Header("Combat Stats")]
        public int Attack;
        public int Defense;
        public int MinDamage;
        public int MaxDamage;
        public int Health;
        public int Speed;

        [Header("Movement")]
        public MovementType MovementType = MovementType.Ground;
        public AttackType AttackType = AttackType.Melee;
        public int Shots;                   // 远程弹药，0 表示无限或近战

        [Header("Recruitment")]
        public ResourceBundle RecruitCost = new();
        public BuildingType RequiredBuilding;
        public int WeeklyGrowth = 7;

        [Header("Abilities")]
        public string[] AbilityIds;

        /// <summary>
        /// 计算伤害
        /// </summary>
        public int RollDamage() => Random.Range(MinDamage, MaxDamage + 1);

        /// <summary>
        /// 是否为远程单位
        /// </summary>
        public bool IsRanged => AttackType == AttackType.Ranged;

        /// <summary>
        /// 是否为飞行单位
        /// </summary>
        public bool IsFlying => MovementType == MovementType.Flying;
    }
}
