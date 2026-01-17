using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 文明配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "FactionConfigDatabase", menuName = "TH7/Faction Config Database")]
    public class FactionConfigDatabase : ScriptableObject
    {
        [Header("Faction Configs")]
        public List<FactionConfig> Factions = new();

        Dictionary<BiomeType, FactionConfig> factionLookup;

        void BuildCache()
        {
            if (factionLookup != null) return;

            factionLookup = new Dictionary<BiomeType, FactionConfig>();
            if (Factions == null) return;

            foreach (var faction in Factions)
            {
                if (faction != null)
                {
                    factionLookup[faction.FactionType] = faction;
                }
            }
        }

        /// <summary>
        /// 获取文明配置
        /// </summary>
        public FactionConfig GetFaction(BiomeType type)
        {
            BuildCache();
            return factionLookup.TryGetValue(type, out var config) ? config : null;
        }

        /// <summary>
        /// 获取所有文明
        /// </summary>
        public IReadOnlyList<FactionConfig> GetAllFactions()
        {
            return Factions;
        }

        /// <summary>
        /// 重建缓存
        /// </summary>
        public void RebuildCache()
        {
            factionLookup = null;
            BuildCache();
        }

        void OnValidate()
        {
            factionLookup = null;
        }
    }
}
