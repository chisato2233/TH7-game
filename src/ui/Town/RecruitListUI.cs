using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 可招募兵种列表
    /// </summary>
    public class RecruitListUI : UIBehaviour
    {
        [Header("List")]
        [SerializeField] Transform listContainer;
        [SerializeField] GameObject unitSlotPrefab;

        [Header("Database")]
        [SerializeField] UnitConfigDatabase unitDatabase;

        TownContext townContext;
        List<RecruitUnitSlotUI> slots = new();

        public Action<UnitConfig, int> OnUnitSelected;

        /// <summary>
        /// 绑定城镇上下文
        /// </summary>
        public void Bind(TownContext context)
        {
            townContext = context;
            Refresh();
        }

        /// <summary>
        /// 刷新列表
        /// </summary>
        public void Refresh()
        {
            ClearSlots();

            if (townContext?.Town == null || unitDatabase == null) return;

            var faction = townContext.Town.Faction;
            var units = unitDatabase.GetFactionUnits(faction);

            foreach (var unit in units)
            {
                if (unit == null) continue;

                // 检查是否有对应建筑
                if (!townContext.Town.HasBuilding(unit.RequiredBuilding))
                    continue;

                // 获取可用数量
                int available = 0;
                townContext.Town.AvailableUnits.TryGetValue(unit.UnitId, out available);

                if (available <= 0) continue;

                var slot = CreateSlot(unit, available);
                if (slot != null)
                    slots.Add(slot);
            }
        }

        RecruitUnitSlotUI CreateSlot(UnitConfig unit, int available)
        {
            if (unitSlotPrefab == null || listContainer == null)
                return null;

            var go = Instantiate(unitSlotPrefab, listContainer);
            var slot = go.GetComponent<RecruitUnitSlotUI>();

            if (slot != null)
            {
                slot.Setup(unit, available);
                slot.OnClicked = () => OnUnitSelected?.Invoke(unit, available);
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

    /// <summary>
    /// 招募单位槽位
    /// </summary>
    public class RecruitUnitSlotUI : UIBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI availableText;
        [SerializeField] ButtonManager button;

        UnitConfig unitConfig;

        public Action OnClicked;

        protected override void Awake()
        {
            base.Awake();

            if (button != null)
                button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void Setup(UnitConfig unit, int available)
        {
            unitConfig = unit;

            if (iconImage != null && unit.Icon != null)
                iconImage.sprite = unit.Icon;

            if (nameText != null)
                nameText.text = unit.DisplayName;

            if (availableText != null)
                availableText.text = available.ToString();
        }

        protected override void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();

            base.OnDestroy();
        }
    }
}
