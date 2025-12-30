using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 建筑槽位 - 显示单个建筑状态
    /// </summary>
    public class BuildingSlotUI : UIBehaviour
    {
        [Header("Display")]
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Image tierIndicator;
        [SerializeField] GameObject lockedOverlay;
        [SerializeField] GameObject builtIndicator;

        [Header("Interaction")]
        [SerializeField] ButtonManager button;

        [Header("Colors")]
        [SerializeField] Color unbuiltColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] Color builtColor = Color.white;
        [SerializeField] Color upgradedColor = new Color(1f, 0.85f, 0.4f, 1f);

        BuildingConfig config;
        BuildingInstance instance;
        TownContext context;

        public Action OnClicked;

        protected override void Awake()
        {
            base.Awake();

            if (button != null)
                button.onClick.AddListener(() => OnClicked?.Invoke());
        }

        /// <summary>
        /// 设置建筑槽位
        /// </summary>
        public void Setup(BuildingConfig buildingConfig, BuildingInstance buildingInstance, TownContext townContext)
        {
            config = buildingConfig;
            instance = buildingInstance;
            context = townContext;

            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (config == null) return;

            // 图标
            if (iconImage != null && config.Icon != null)
                iconImage.sprite = config.Icon;

            // 名称
            if (nameText != null)
                nameText.text = config.DisplayName;

            // 状态
            bool isBuilt = instance != null;
            bool isUpgraded = instance?.IsUpgraded ?? false;
            bool canBuild = context?.CanBuild(config.Type) ?? false;

            // 图标颜色
            if (iconImage != null)
            {
                if (isUpgraded)
                    iconImage.color = upgradedColor;
                else if (isBuilt)
                    iconImage.color = builtColor;
                else
                    iconImage.color = unbuiltColor;
            }

            // 等级指示器
            if (tierIndicator != null)
            {
                tierIndicator.gameObject.SetActive(isBuilt);
                if (isBuilt)
                    tierIndicator.color = isUpgraded ? upgradedColor : builtColor;
            }

            // 已建造指示器
            if (builtIndicator != null)
                builtIndicator.SetActive(isBuilt);

            // 锁定遮罩（未建造且无法建造）
            if (lockedOverlay != null)
                lockedOverlay.SetActive(!isBuilt && !canBuild);

            // 按钮可交互性
            if (button != null)
                button.Interactable(true); // 总是可点击，详情面板显示原因
        }

        protected override void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();

            base.OnDestroy();
        }
    }
}
