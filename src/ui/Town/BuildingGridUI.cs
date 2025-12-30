using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 建筑网格 - 显示所有可建造建筑
    /// </summary>
    public class BuildingGridUI : UIBehaviour
    {
        [Header("Grid")]
        [SerializeField] Transform gridContainer;
        [SerializeField] GameObject buildingSlotPrefab;

        TownContext townContext;
        List<BuildingSlotUI> slots = new();

        public Action<BuildingType, BuildingConfig> OnBuildingSelected;

        /// <summary>
        /// 绑定城镇上下文
        /// </summary>
        public void Bind(TownContext context)
        {
            townContext = context;
            RefreshGrid();

            // 监听建筑变化
            if (context?.Town?.Buildings != null)
                Listen(context.Town.Buildings, _ => RefreshGrid());
        }

        void RefreshGrid()
        {
            ClearSlots();

            if (townContext?.Config == null) return;

            var buildings = townContext.Config.GetAllBuildings();
            foreach (var config in buildings)
            {
                if (config == null) continue;

                var slot = CreateSlot(config);
                if (slot != null)
                {
                    slot.OnClicked = () => OnBuildingSelected?.Invoke(config.Type, config);
                    slots.Add(slot);
                }
            }
        }

        BuildingSlotUI CreateSlot(BuildingConfig config)
        {
            if (buildingSlotPrefab == null || gridContainer == null)
                return null;

            var go = Instantiate(buildingSlotPrefab, gridContainer);
            var slot = go.GetComponent<BuildingSlotUI>();

            if (slot != null)
            {
                var buildingInstance = townContext.Town.GetBuilding(config.Type);
                slot.Setup(config, buildingInstance, townContext);
            }

            return slot;
        }

        void ClearSlots()
        {
            foreach (var slot in slots)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            slots.Clear();
        }

        protected override void OnDestroy()
        {
            ClearSlots();
            base.OnDestroy();
        }
    }
}
