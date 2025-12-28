using System;
using UnityEngine;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 存档选择弹窗，使用可拖动窗口
    /// </summary>
    public class SaveSlotWindow : UIWindowBehaviour
    {
        [Header("ScrollView")]
        [SerializeField] SaveSlotListUI slotListUI;

        [Header("Buttons")]
        [SerializeField] ButtonManager closeButton;

        Action<SaveSlotInfo> onSlotSelected;
        Action onCanceled;

        protected override void Awake()
        {
            base.Awake();

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        /// <summary>
        /// 显示存档选择面板（绑定 ReactiveList）
        /// </summary>
        public void Show(ReactiveList<SaveSlotInfo> slots, Action<SaveSlotInfo> onSelect, Action onCancel = null)
        {
            onSlotSelected = onSelect;
            onCanceled = onCancel;

            if (slotListUI != null)
            {
                slotListUI.OnSlotSelected = OnSlotClicked;
                slotListUI.Bind(slots);
            }

            Show();
        }

        /// <summary>
        /// 显示存档选择面板（手动传入数组）
        /// </summary>
        public void Show(SaveSlotInfo[] slots, Action<SaveSlotInfo> onSelect, Action onCancel = null)
        {
            onSlotSelected = onSelect;
            onCanceled = onCancel;

            if (slotListUI != null)
            {
                slotListUI.OnSlotSelected = OnSlotClicked;
                slotListUI.Refresh(slots);
            }

            Show();
        }

        void OnSlotClicked(SaveSlotInfo slot)
        {
            Hide();
            onSlotSelected?.Invoke(slot);
        }

        void OnCloseClicked()
        {
            Hide();
            onCanceled?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);

            base.OnDestroy();
        }
    }

}
