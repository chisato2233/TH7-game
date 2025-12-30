using System;
using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 建筑配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "TH7/Building Config")]
    public class BuildingConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public BuildingType Type;
        public string DisplayName;
        [TextArea] public string Description;
        public Sprite Icon;

        [Header("Cost - Basic")]
        public ResourceBundle BasicCost = new();

        [Header("Cost - Upgrade")]
        public ResourceBundle UpgradeCost = new();

        [Header("Requirements")]
        public List<BuildingRequirement> Requirements = new();

        [Header("Production")]
        public int GoldPerDay;              // 每日产金
        public string ProducedUnitId;       // 产出的兵种 ID
        public int WeeklyGrowth;            // 每周产出数量

        /// <summary>
        /// 检查是否满足建造条件
        /// </summary>
        public bool CanBuild(TownData town, PlayerResources resources, BuildingTier targetTier = BuildingTier.Basic)
        {
            // 检查资源
            var cost = targetTier == BuildingTier.Basic ? BasicCost : UpgradeCost;
            if (!resources.HasEnough(cost))
                return false;

            // 检查前置建筑
            foreach (var req in Requirements)
            {
                if (!town.HasBuilding(req.RequiredBuilding, req.RequiredTier))
                    return false;
            }

            // 如果是升级，检查是否已有基础版
            if (targetTier == BuildingTier.Upgraded)
            {
                if (!town.HasBuilding(Type, BuildingTier.Basic))
                    return false;
            }
            else
            {
                // 基础版不能重复建造
                if (town.HasBuilding(Type))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取不满足的条件列表
        /// </summary>
        public List<string> GetUnmetRequirements(TownData town, PlayerResources resources, BuildingTier targetTier = BuildingTier.Basic)
        {
            var unmet = new List<string>();

            var cost = targetTier == BuildingTier.Basic ? BasicCost : UpgradeCost;
            if (!resources.HasEnough(cost))
                unmet.Add("Insufficient resources");

            foreach (var req in Requirements)
            {
                if (!town.HasBuilding(req.RequiredBuilding, req.RequiredTier))
                    unmet.Add($"Requires: {req.RequiredBuilding}");
            }

            if (targetTier == BuildingTier.Upgraded && !town.HasBuilding(Type, BuildingTier.Basic))
                unmet.Add("Build basic version first");

            if (targetTier == BuildingTier.Basic && town.HasBuilding(Type))
                unmet.Add("Already built");

            return unmet;
        }
    }

    /// <summary>
    /// 建筑前置条件
    /// </summary>
    [Serializable]
    public class BuildingRequirement
    {
        public BuildingType RequiredBuilding;
        public BuildingTier RequiredTier = BuildingTier.Basic;
    }
}
