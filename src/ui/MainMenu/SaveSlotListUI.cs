using UnityEngine;
using TMPro;
using Michsky.MUIP;
using GameFramework;
using System;

namespace TH7.UI{
    
    /// <summary>
    /// 存档列表 ScrollView
    /// </summary>
    public class SaveSlotListUI : ScrollViewUI<SaveSlotInfo, SaveSlotItemUI>
    {
        public Action<SaveSlotInfo> OnSlotSelected;
        public Action<SaveSlotInfo> OnSlotDeleted;

        protected override void OnItemCreated(SaveSlotItemUI item)
        {
            item.OnSelected = OnSlotSelected;
            item.OnDeleted = OnSlotDeleted;
        }
    }

    /// <summary>
    /// 单个存档项 UI
    /// </summary>
    public class SaveSlotItemUI : ScrollViewItem<SaveSlotInfo>
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI infoText;
        [SerializeField] TextMeshProUGUI timeText;
        [SerializeField] ButtonManager selectButton;
        [SerializeField] ButtonManager deleteButton;

        public Action<SaveSlotInfo> OnSelected;
        public Action<SaveSlotInfo> OnDeleted;

        protected override void OnSetup()
        {
            if (nameText != null)
                nameText.text = data.PlayerName;

            if (infoText != null)
                infoText.text = $"Day {data.Day}";

            if (timeText != null)
                timeText.text = data.TimeAgo;

            BindButtons();
        }

        void BindButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => OnSelected?.Invoke(data));
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => OnDeleted?.Invoke(data));
            }
        }

        protected override void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveAllListeners();
            if (deleteButton != null)
                deleteButton.onClick.RemoveAllListeners();

            base.OnDestroy();
        }
    }
}