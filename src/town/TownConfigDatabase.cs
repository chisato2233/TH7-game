using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 城镇配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "TownConfigDatabase", menuName = "TH7/Town Config Database")]
    public class TownConfigDatabase : ScriptableObject
    {
        [Header("Buildings")]
        public List<BuildingConfig> Buildings = new();

        [Header("Faction Defaults")]
        public List<FactionTownConfig> FactionConfigs = new();

        // 缓存
        Dictionary<BuildingType, BuildingConfig> buildingLookup;

        void OnEnable() => RebuildCache();

        public void RebuildCache()
        {
            buildingLookup = new Dictionary<BuildingType, BuildingConfig>();
            foreach (var config in Buildings)
            {
                if (config != null)
                    buildingLookup[config.Type] = config;
            }
        }

        /// <summary>
        /// 获取建筑配置
        /// </summary>
        public BuildingConfig GetBuilding(BuildingType type)
        {
            if (buildingLookup == null) RebuildCache();
            return buildingLookup.TryGetValue(type, out var config) ? config : null;
        }

        /// <summary>
        /// 获取阵营城镇配置
        /// </summary>
        public FactionTownConfig GetFactionConfig(BiomeType faction)
        {
            foreach (var config in FactionConfigs)
            {
                if (config.Faction == faction)
                    return config;
            }
            return null;
        }

        /// <summary>
        /// 获取所有可建造的建筑（按类型排序）
        /// </summary>
        public List<BuildingConfig> GetAllBuildings() => Buildings;
    }

    /// <summary>
    /// 阵营城镇配置
    /// </summary>
    [System.Serializable]
    public class FactionTownConfig
    {
        public BiomeType Faction;
        public string DisplayName;
        public Sprite TownIcon;
        public List<BuildingType> DefaultBuildings = new();
    }
}
