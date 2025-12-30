using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 城镇运行时数据（支持存档）
    /// </summary>
    [Serializable]
    public class TownData
    {
        public string TownId;
        public string TownName;
        public BiomeType Faction;
        public Vector2Int MapPosition;

        // 已建造的建筑
        public ReactiveList<BuildingInstance> Buildings = new();

        // 驻军
        public ReactiveList<UnitStack> Garrison = new();

        // 每周兵种产出累计
        public Dictionary<string, int> AvailableUnits = new();

        public TownData() { }

        public TownData(string id, string name, BiomeType faction, Vector2Int position)
        {
            TownId = id;
            TownName = name;
            Faction = faction;
            MapPosition = position;
        }

        /// <summary>
        /// 检查是否已建造某建筑
        /// </summary>
        public bool HasBuilding(BuildingType type, BuildingTier minTier = BuildingTier.Basic)
        {
            for (int i = 0; i < Buildings.Count; i++)
            {
                if (Buildings[i].Type == type && Buildings[i].Tier >= minTier)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取建筑实例
        /// </summary>
        public BuildingInstance GetBuilding(BuildingType type)
        {
            for (int i = 0; i < Buildings.Count; i++)
            {
                if (Buildings[i].Type == type)
                    return Buildings[i];
            }
            return null;
        }

        /// <summary>
        /// 添加建筑
        /// </summary>
        public void AddBuilding(BuildingType type, BuildingTier tier = BuildingTier.Basic)
        {
            var existing = GetBuilding(type);
            if (existing != null)
            {
                existing.Tier = tier;
            }
            else
            {
                Buildings.Add(new BuildingInstance(type, tier));
            }
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public bool UpgradeBuilding(BuildingType type)
        {
            var building = GetBuilding(type);
            if (building == null || building.Tier >= BuildingTier.Upgraded)
                return false;

            building.Tier = BuildingTier.Upgraded;
            return true;
        }

        /// <summary>
        /// 获取每日金币产出
        /// </summary>
        public int GetDailyGoldProduction()
        {
            // 基础产出
            int gold = 500;

            // 城镇大厅加成
            var townHall = GetBuilding(BuildingType.TownHall);
            if (townHall != null)
            {
                gold += townHall.Tier switch
                {
                    BuildingTier.Basic => 500,      // 城镇大厅: +500
                    BuildingTier.Upgraded => 1000,  // 城市大厅: +1000
                    _ => 0
                };
            }

            return gold;
        }

        /// <summary>
        /// 刷新每周招募数量
        /// </summary>
        public void RefreshWeeklyRecruits()
        {
            // TODO: 根据兵种建筑刷新可招募数量
            // 遍历所有 Dwelling 建筑，根据配置添加招募数量
        }

        /// <summary>
        /// 获取格子坐标位置
        /// </summary>
        public Vector3Int Position => new(MapPosition.x, MapPosition.y, 0);
    }

    /// <summary>
    /// 建筑实例
    /// </summary>
    [Serializable]
    public class BuildingInstance
    {
        public BuildingType Type;
        public BuildingTier Tier;

        public BuildingInstance() { }

        public BuildingInstance(BuildingType type, BuildingTier tier = BuildingTier.Basic)
        {
            Type = type;
            Tier = tier;
        }

        public bool IsUpgraded => Tier == BuildingTier.Upgraded;
    }

    /// <summary>
    /// 兵种堆叠
    /// </summary>
    [Serializable]
    public class UnitStack
    {
        public string UnitId;
        public int Count;

        public UnitStack() { }

        public UnitStack(string unitId, int count)
        {
            UnitId = unitId;
            Count = count;
        }

        public bool IsEmpty => Count <= 0;
    }
}
