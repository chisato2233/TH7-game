using System;
using UnityEngine;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 英雄列表 UI
    /// </summary>
    public class HeroListUI : ScrollViewUI<HeroConfig, HeroListItem>
    {
        public event Action<HeroConfig> OnHeroSelected;

        protected override void OnItemCreated(HeroListItem item)
        {
            item.OnClicked += HandleItemClicked;
        }

        protected override void OnItemDestroyed(HeroListItem item)
        {
            item.OnClicked -= HandleItemClicked;
        }

        void HandleItemClicked(HeroConfig hero)
        {
            OnHeroSelected?.Invoke(hero);
        }
    }
}
