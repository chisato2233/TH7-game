using System;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 城镇主界面（全屏窗口，覆盖在 WorldScene 上）
    /// </summary>
    public class TownPanelUI : UIWindowBehaviour
    {
        [Header("Header")]
        [SerializeField] TextMeshProUGUI townNameText;
        [SerializeField] TextMeshProUGUI factionText;

        [Header("Building Grid")]
        [SerializeField] BuildingGridUI buildingGrid;

        [Header("Info Panel")]
        [SerializeField] BuildingInfoPanelUI buildingInfoPanel;

        [Header("Buttons")]
        [SerializeField] ButtonManager recruitButton;
        [SerializeField] ButtonManager exitButton;

        [Header("Panels")]
        [SerializeField] RecruitPanelUI recruitPanel;

        TownContext townContext;

        public Action OnExitRequested;

        protected override void Start()
        {
            base.Start();

            if (recruitButton != null)
                recruitButton.onClick.AddListener(OnRecruitClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        /// <summary>
        /// 绑定并显示城镇界面
        /// </summary>
        public void Bind(TownContext context)
        {
            townContext = context;

            if (context?.Town == null) return;

            // 更新标题
            if (townNameText != null)
                townNameText.text = context.Town.TownName;

            if (factionText != null)
                factionText.text = context.Town.Faction.ToString();

            // 绑定建筑网格
            if (buildingGrid != null)
                buildingGrid.Bind(context);

            // 监听建筑点击
            if (buildingGrid != null)
                buildingGrid.OnBuildingSelected = OnBuildingSelected;

            // 播放显示动画
            Show();
        }

        /// <summary>
        /// 关闭城镇界面
        /// </summary>
        public void Close()
        {
            Hide();
            OnHideComplete = () =>
            {
                townContext = null;
                OnHideComplete = null;
            };
        }

        void OnBuildingSelected(BuildingType type, BuildingConfig config)
        {
            if (buildingInfoPanel != null)
                buildingInfoPanel.Show(type, config, townContext);
        }

        void OnRecruitClicked()
        {
            if (recruitPanel != null && townContext != null)
                recruitPanel.Show(townContext);
        }

        void OnExitClicked()
        {
            Close();
            OnExitRequested?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (recruitButton != null)
                recruitButton.onClick.RemoveListener(OnRecruitClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);

            base.OnDestroy();
        }
    }
}
