using System;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 招募面板
    /// </summary>
    public class RecruitPanelUI : UIWindowBehaviour
    {
        [Header("List")]
        [SerializeField] RecruitListUI recruitList;

        [Header("Selected Unit Info")]
        [SerializeField] TextMeshProUGUI unitNameText;
        [SerializeField] TextMeshProUGUI unitStatsText;
        [SerializeField] TextMeshProUGUI availableText;

        [Header("Recruit Controls")]
        [SerializeField] TMP_InputField countInput;
        [SerializeField] ButtonManager minButton;
        [SerializeField] ButtonManager maxButton;
        [SerializeField] ButtonManager recruitButton;
        [SerializeField] ButtonManager closeButton;

        [Header("Cost Preview")]
        [SerializeField] Transform costContainer;
        [SerializeField] GameObject costItemPrefab;

        TownContext townContext;
        UnitConfig selectedUnit;
        int selectedCount;
        int maxAvailable;

        protected override void Awake()
        {
            base.Awake();

            if (minButton != null)
                minButton.onClick.AddListener(() => SetCount(1));

            if (maxButton != null)
                maxButton.onClick.AddListener(() => SetCount(maxAvailable));

            if (recruitButton != null)
                recruitButton.onClick.AddListener(OnRecruitClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            if (countInput != null)
                countInput.onEndEdit.AddListener(OnCountInputChanged);
        }

        /// <summary>
        /// 显示招募面板
        /// </summary>
        public void Show(TownContext context)
        {
            townContext = context;

            if (recruitList != null)
            {
                recruitList.Bind(context);
                recruitList.OnUnitSelected = OnUnitSelected;
            }

            selectedUnit = null;
            selectedCount = 0;
            UpdateSelectedUnitDisplay();

            Show();
        }

        void OnUnitSelected(UnitConfig unit, int available)
        {
            selectedUnit = unit;
            maxAvailable = available;
            SetCount(1);
            UpdateSelectedUnitDisplay();
        }

        void SetCount(int count)
        {
            selectedCount = Mathf.Clamp(count, 0, maxAvailable);

            if (countInput != null)
                countInput.text = selectedCount.ToString();

            UpdateCostPreview();
            UpdateRecruitButton();
        }

        void OnCountInputChanged(string value)
        {
            if (int.TryParse(value, out int count))
                SetCount(count);
            else
                SetCount(0);
        }

        void UpdateSelectedUnitDisplay()
        {
            if (selectedUnit == null)
            {
                if (unitNameText != null) unitNameText.text = "Select a unit";
                if (unitStatsText != null) unitStatsText.text = "";
                if (availableText != null) availableText.text = "";
                ClearCostPreview();
                return;
            }

            if (unitNameText != null)
                unitNameText.text = selectedUnit.DisplayName;

            if (unitStatsText != null)
            {
                unitStatsText.text = $"ATK: {selectedUnit.Attack}  DEF: {selectedUnit.Defense}\n" +
                                    $"DMG: {selectedUnit.MinDamage}-{selectedUnit.MaxDamage}  HP: {selectedUnit.Health}\n" +
                                    $"SPD: {selectedUnit.Speed}  {(selectedUnit.IsRanged ? "Ranged" : "Melee")}";
            }

            if (availableText != null)
                availableText.text = $"Available: {maxAvailable}";

            UpdateCostPreview();
            UpdateRecruitButton();
        }

        void UpdateCostPreview()
        {
            ClearCostPreview();

            if (selectedUnit == null || selectedCount <= 0) return;

            var cost = selectedUnit.RecruitCost * selectedCount;
            var resources = townContext?.Resources;

            CreateCostItem(ResourceType.Gold, cost.Gold, resources);
            CreateCostItem(ResourceType.Wood, cost.Wood, resources);
            CreateCostItem(ResourceType.Ore, cost.Ore, resources);
            CreateCostItem(ResourceType.Crystal, cost.Crystal, resources);
            CreateCostItem(ResourceType.Gem, cost.Gem, resources);
            CreateCostItem(ResourceType.Sulfur, cost.Sulfur, resources);
            CreateCostItem(ResourceType.Mercury, cost.Mercury, resources);
        }

        void ClearCostPreview()
        {
            if (costContainer == null) return;

            foreach (Transform child in costContainer)
                Destroy(child.gameObject);
        }

        void CreateCostItem(ResourceType type, int amount, PlayerResources resources)
        {
            if (amount <= 0 || costItemPrefab == null || costContainer == null) return;

            var go = Instantiate(costItemPrefab, costContainer);
            var costUI = go.GetComponent<ResourceCostUI>();

            if (costUI != null)
                costUI.Setup(type, amount, resources);
        }

        void UpdateRecruitButton()
        {
            if (recruitButton == null) return;

            bool canRecruit = selectedUnit != null &&
                              selectedCount > 0 &&
                              townContext?.CanRecruit(selectedUnit.UnitId, selectedCount, selectedUnit) == true;

            recruitButton.Interactable(canRecruit);
        }

        void OnRecruitClicked()
        {
            if (selectedUnit == null || selectedCount <= 0) return;

            if (townContext?.Recruit(selectedUnit.UnitId, selectedCount, selectedUnit) == true)
            {
                // 更新可用数量
                maxAvailable -= selectedCount;
                SetCount(Mathf.Min(selectedCount, maxAvailable));

                // 刷新列表
                recruitList?.Refresh();

                UpdateSelectedUnitDisplay();
            }
        }

        protected override void OnDestroy()
        {
            if (minButton != null)
                minButton.onClick.RemoveAllListeners();

            if (maxButton != null)
                maxButton.onClick.RemoveAllListeners();

            if (recruitButton != null)
                recruitButton.onClick.RemoveListener(OnRecruitClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);

            base.OnDestroy();
        }
    }
}
