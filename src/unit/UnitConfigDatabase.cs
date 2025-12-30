using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 兵种配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "UnitConfigDatabase", menuName = "TH7/Unit Config Database")]
    public class UnitConfigDatabase : ScriptableObject
    {
        public List<UnitConfig> Units = new();

        // 缓存
        Dictionary<string, UnitConfig> unitLookup;
        Dictionary<BiomeType, List<UnitConfig>> factionUnits;

        void OnEnable() => RebuildCache();

        public void RebuildCache()
        {
            unitLookup = new Dictionary<string, UnitConfig>();
            factionUnits = new Dictionary<BiomeType, List<UnitConfig>>();

            foreach (var unit in Units)
            {
                if (unit == null) continue;

                unitLookup[unit.UnitId] = unit;

                if (!factionUnits.ContainsKey(unit.Faction))
                    factionUnits[unit.Faction] = new List<UnitConfig>();

                factionUnits[unit.Faction].Add(unit);
            }

            // 按等级排序
            foreach (var list in factionUnits.Values)
                list.Sort((a, b) => a.Tier.CompareTo(b.Tier));
        }

        /// <summary>
        /// 获取兵种配置
        /// </summary>
        public UnitConfig GetUnit(string unitId)
        {
            if (unitLookup == null) RebuildCache();
            return unitLookup.TryGetValue(unitId, out var config) ? config : null;
        }

        /// <summary>
        /// 获取阵营所有兵种
        /// </summary>
        public List<UnitConfig> GetFactionUnits(BiomeType faction)
        {
            if (factionUnits == null) RebuildCache();
            return factionUnits.TryGetValue(faction, out var list) ? list : new List<UnitConfig>();
        }

        /// <summary>
        /// 获取指定等级的兵种
        /// </summary>
        public UnitConfig GetFactionUnitByTier(BiomeType faction, UnitTier tier, bool upgraded = false)
        {
            var units = GetFactionUnits(faction);
            foreach (var unit in units)
            {
                if (unit.Tier == tier && unit.IsUpgraded == upgraded)
                    return unit;
            }
            return null;
        }
    }
}
