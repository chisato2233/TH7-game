using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 建筑详情面板
    /// </summary>
    public class BuildingInfoPanelUI : UIWindowBehaviour
    {
        [Header("Info")]
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] TextMeshProUGUI statusText;

        [Header("Cost")]
        [SerializeField] Transform costContainer;
        [SerializeField] GameObject costItemPrefab;

        [Header("Requirements")]
        [SerializeField] TextMeshProUGUI requirementsText;

        [Header("Buttons")]
        [SerializeField] ButtonManager buildButton;
        [SerializeField] ButtonManager upgradeButton;
        [SerializeField] ButtonManager closeButton;

        BuildingType currentType;
        BuildingConfig currentConfig;
        TownContext townContext;

        protected override void Awake()
        {
            base.Awake();

            if (buildButton != null)
                buildButton.onClick.AddListener(OnBuildClicked);

            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        /// <summary>
        /// 显示建筑详情
        /// </summary>
        public void Show(BuildingType type, BuildingConfig config, TownContext context)
        {
            currentType = type;
            currentConfig = config;
            townContext = context;

            UpdateDisplay();
            Show();
        }

        void UpdateDisplay()
        {
            if (currentConfig == null) return;

            // 基本信息
            if (iconImage != null && currentConfig.Icon != null)
                iconImage.sprite = currentConfig.Icon;

            if (nameText != null)
                nameText.text = currentConfig.DisplayName;

            if (descriptionText != null)
                descriptionText.text = currentConfig.Description;

            // 状态检查
            var building = townContext?.Town?.GetBuilding(currentType);
            bool isBuilt = building != null;
            bool isUpgraded = building?.IsUpgraded ?? false;
            bool canBuild = townContext?.CanBuild(currentType, BuildingTier.Basic) ?? false;
            bool canUpgrade = isBuilt && !isUpgraded && (townContext?.CanBuild(currentType, BuildingTier.Upgraded) ?? false);

            // 状态文本
            if (statusText != null)
            {
                if (isUpgraded)
                    statusText.text = "Upgraded";
                else if (isBuilt)
                    statusText.text = "Built";
                else if (canBuild)
                    statusText.text = "Available";
                else
                    statusText.text = "Locked";
            }

            // 成本显示
            UpdateCostDisplay(isBuilt ? currentConfig.UpgradeCost : currentConfig.BasicCost);

            // 前置条件
            UpdateRequirementsDisplay(isBuilt);

            // 按钮状态
            if (buildButton != null)
            {
                buildButton.gameObject.SetActive(!isBuilt);
                buildButton.Interactable(canBuild);
            }

            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(isBuilt && !isUpgraded);
                upgradeButton.Interactable(canUpgrade);
            }
        }

        void UpdateCostDisplay(ResourceBundle cost)
        {
            if (costContainer == null) return;

            // 清理旧的
            foreach (Transform child in costContainer)
                Destroy(child.gameObject);

            if (cost == null || cost.IsEmpty) return;

            // 创建成本项
            var resources = townContext?.Resources;
            CreateCostItem(ResourceType.Gold, cost.Gold, resources);
            CreateCostItem(ResourceType.Wood, cost.Wood, resources);
            CreateCostItem(ResourceType.Ore, cost.Ore, resources);
            CreateCostItem(ResourceType.Crystal, cost.Crystal, resources);
            CreateCostItem(ResourceType.Gem, cost.Gem, resources);
            CreateCostItem(ResourceType.Sulfur, cost.Sulfur, resources);
            CreateCostItem(ResourceType.Mercury, cost.Mercury, resources);
        }

        void CreateCostItem(ResourceType type, int amount, PlayerResources resources)
        {
            if (amount <= 0 || costItemPrefab == null) return;

            var go = Instantiate(costItemPrefab, costContainer);
            var costUI = go.GetComponent<ResourceCostUI>();

            if (costUI != null)
                costUI.Setup(type, amount, resources);
        }

        void UpdateRequirementsDisplay(bool isBuilt)
        {
            if (requirementsText == null) return;

            if (currentConfig?.Requirements == null || currentConfig.Requirements.Count == 0)
            {
                requirementsText.text = "No requirements";
                return;
            }

            var tier = isBuilt ? BuildingTier.Upgraded : BuildingTier.Basic;
            var unmet = currentConfig.GetUnmetRequirements(townContext?.Town, townContext?.Resources, tier);

            if (unmet.Count == 0)
                requirementsText.text = "All requirements met";
            else
                requirementsText.text = string.Join("\n", unmet);
        }

        void OnBuildClicked()
        {
            if (townContext?.Build(currentType) == true)
            {
                UpdateDisplay();
            }
        }

        void OnUpgradeClicked()
        {
            if (townContext?.Upgrade(currentType) == true)
            {
                UpdateDisplay();
            }
        }

        protected override void OnDestroy()
        {
            if (buildButton != null)
                buildButton.onClick.RemoveListener(OnBuildClicked);

            if (upgradeButton != null)
                upgradeButton.onClick.RemoveListener(OnUpgradeClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);

            base.OnDestroy();
        }
    }
}
