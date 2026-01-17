using System;
using UnityEngine;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 阵营列表 UI
    /// </summary>
    public class FactionListUI : ScrollViewUI<FactionConfig, FactionListItem>
    {
        public event Action<FactionConfig> OnFactionSelected;

        protected override void OnItemCreated(FactionListItem item)
        {
            item.OnClicked += HandleItemClicked;
        }

        protected override void OnItemDestroyed(FactionListItem item)
        {
            item.OnClicked -= HandleItemClicked;
        }

        void HandleItemClicked(FactionConfig faction)
        {
            Debug.Log($"[FactionListUI] HandleItemClicked: {faction?.DisplayName}");
            OnFactionSelected?.Invoke(faction);
        }
    }
}
