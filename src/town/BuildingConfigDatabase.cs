using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 建筑配置数据库
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfigDatabase", menuName = "TH7/Database/Building Config Database")]
    public class BuildingConfigDatabase : ScriptableObject
    {
        public List<BuildingConfig> Buildings = new();

        public BuildingConfig GetByType(BuildingType type)
        {
            return Buildings.Find(b => b.Type == type);
        }

        public List<BuildingConfig> GetByTier(BuildingTier tier)
        {
            // 这个方法可以根据需求实现
            return Buildings;
        }
    }
}
