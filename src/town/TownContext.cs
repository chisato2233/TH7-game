using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 城镇上下文 - 进入城镇时创建，离开时销毁
    /// </summary>
    public class TownContext : GameContext
    {
        public TownData Town { get; private set; }
        public TownConfigDatabase Config { get; private set; }

        // 便捷访问 Session
        SessionContext Session => GetParent<SessionContext>();
        public PlayerResources Resources => Session?.Resources;

        public void Setup(TownData town, TownConfigDatabase config)
        {
            Town = town;
            Config = config;
        }

        protected override void OnInitialize()
        {
            Debug.Log($"[Town] 进入城镇: {Town?.TownName}");
        }

        protected override void OnDispose()
        {
            Debug.Log($"[Town] 离开城镇: {Town?.TownName}");
        }

        #region Building Operations

        /// <summary>
        /// 检查是否可以建造
        /// </summary>
        public bool CanBuild(BuildingType type, BuildingTier tier = BuildingTier.Basic)
        {
            var config = Config?.GetBuilding(type);
            if (config == null) return false;
            return config.CanBuild(Town, Resources, tier);
        }

        /// <summary>
        /// 建造建筑
        /// </summary>
        public bool Build(BuildingType type, BuildingTier tier = BuildingTier.Basic)
        {
            var config = Config?.GetBuilding(type);
            if (config == null) return false;

            if (!config.CanBuild(Town, Resources, tier))
                return false;

            // 扣除资源
            var cost = tier == BuildingTier.Basic ? config.BasicCost : config.UpgradeCost;
            Resources.Subtract(cost);

            // 添加建筑
            Town.AddBuilding(type, tier);

            Debug.Log($"[Town] 建造完成: {type} ({tier})");

            // 发送事件
            GameEntry.Instance?.GetSystem<EventSystem>()?.Publish(new BuildingConstructedEvent
            {
                TownId = Town.TownId,
                BuildingType = type,
                Tier = tier
            });

            return true;
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public bool Upgrade(BuildingType type)
        {
            if (!Town.HasBuilding(type, BuildingTier.Basic))
                return false;

            if (Town.HasBuilding(type, BuildingTier.Upgraded))
                return false;

            return Build(type, BuildingTier.Upgraded);
        }

        #endregion

        #region Recruitment

        /// <summary>
        /// 检查是否可以招募
        /// </summary>
        public bool CanRecruit(string unitId, int count, UnitConfig unitConfig)
        {
            if (unitConfig == null) return false;

            // 检查资源
            var cost = unitConfig.RecruitCost * count;
            if (!Resources.HasEnough(cost))
                return false;

            // 检查可用数量
            if (!Town.AvailableUnits.TryGetValue(unitId, out int available))
                return false;

            if (available < count)
                return false;

            return true;
        }

        /// <summary>
        /// 招募兵种
        /// </summary>
        public bool Recruit(string unitId, int count, UnitConfig unitConfig)
        {
            if (!CanRecruit(unitId, count, unitConfig))
                return false;

            // 扣除资源
            var cost = unitConfig.RecruitCost * count;
            Resources.Subtract(cost);

            // 减少可用数量
            Town.AvailableUnits[unitId] -= count;

            // 添加到驻军
            AddToGarrison(unitId, count);

            Debug.Log($"[Town] 招募: {unitId} x{count}");

            return true;
        }

        void AddToGarrison(string unitId, int count)
        {
            for (int i = 0; i < Town.Garrison.Count; i++)
            {
                if (Town.Garrison[i].UnitId == unitId)
                {
                    Town.Garrison[i].Count += count;
                    return;
                }
            }

            Town.Garrison.Add(new UnitStack(unitId, count));
        }

        #endregion
    }

    // 事件定义
    public struct BuildingConstructedEvent
    {
        public string TownId;
        public BuildingType BuildingType;
        public BuildingTier Tier;
    }
}
