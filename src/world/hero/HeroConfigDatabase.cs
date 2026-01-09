using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "HeroConfigDatabase", menuName = "TH7/Hero Config Database")]
    public class HeroConfigDatabase : ScriptableObject
    {
        [Header("Hero Configs")]
        public HeroConfig[] Heroes;

        Dictionary<string, HeroConfig> heroLookup;

        void BuildCache()
        {
            if (heroLookup != null) return;

            heroLookup = new Dictionary<string, HeroConfig>();
            if (Heroes == null) return;

            foreach (var hero in Heroes)
            {
                if (hero != null && !string.IsNullOrEmpty(hero.HeroId))
                {
                    heroLookup[hero.HeroId] = hero;
                }
            }
        }

        /// <summary>
        /// 获取英雄配置
        /// </summary>
        public HeroConfig GetHero(string heroId)
        {
            BuildCache();
            return heroLookup.TryGetValue(heroId, out var config) ? config : null;
        }

        /// <summary>
        /// 检查是否存在英雄
        /// </summary>
        public bool HasHero(string heroId)
        {
            BuildCache();
            return heroLookup.ContainsKey(heroId);
        }

        /// <summary>
        /// 获取所有英雄ID
        /// </summary>
        public IEnumerable<string> GetAllHeroIds()
        {
            BuildCache();
            return heroLookup.Keys;
        }

        /// <summary>
        /// 按阵营获取英雄
        /// </summary>
        public List<HeroConfig> GetHeroesByFaction(BiomeType faction)
        {
            var result = new List<HeroConfig>();
            if (Heroes == null) return result;

            foreach (var hero in Heroes)
            {
                if (hero != null && hero.Faction == faction)
                {
                    result.Add(hero);
                }
            }
            return result;
        }

        /// <summary>
        /// 按职业获取英雄
        /// </summary>
        public List<HeroConfig> GetHeroesByClass(HeroClass heroClass)
        {
            var result = new List<HeroConfig>();
            if (Heroes == null) return result;

            foreach (var hero in Heroes)
            {
                if (hero != null && hero.Class == heroClass)
                {
                    result.Add(hero);
                }
            }
            return result;
        }

        /// <summary>
        /// 重建缓存
        /// </summary>
        public void RebuildCache()
        {
            heroLookup = null;
            BuildCache();
        }

        void OnValidate()
        {
            heroLookup = null;
        }
    }
}
