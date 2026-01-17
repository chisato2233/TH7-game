using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 新游戏窗口 - 选择文明和英雄
    /// 点击阵营自动跳转到英雄选择，点击英雄自动开始游戏
    /// </summary>
    public class NewGameWindow : UIWindowBehaviour
    {
        [Header("Database")]
        [SerializeField] FactionConfigDatabase factionDatabase;

        [Header("Window Manager")]
        [SerializeField] WindowManager windowManager;

        [Header("Step 1: Faction Selection")]
        [SerializeField] FactionListUI factionList;

        [Header("Step 2: Hero Selection")]
        [SerializeField] HeroListUI heroList;

        // 状态
        FactionConfig selectedFaction;
        HeroConfig selectedHero;

        // 回调
        Action<NewGameSettings> onConfirm;
        Action onCancel;

        protected override void Awake()
        {
            base.Awake();

            // 绑定列表事件
            if (factionList != null)
                factionList.OnFactionSelected += SelectFaction;

            if (heroList != null)
                heroList.OnHeroSelected += SelectHero;
        }

        /// <summary>
        /// 显示新游戏窗口
        /// </summary>
        public void Show(Action<NewGameSettings> onConfirm, Action onCancel = null)
        {
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            selectedFaction = null;
            selectedHero = null;

            // 刷新阵营列表
            if (factionList != null && factionDatabase != null)
                factionList.Refresh(factionDatabase.Factions);

            // 使用 WindowManager 切换到第一步
            if (windowManager != null)
                windowManager.OpenWindowByIndex(0);

            Show();
        }

        void SelectFaction(FactionConfig faction)
        {
            selectedFaction = faction;
            selectedHero = null;

            // 刷新英雄列表并自动跳转到第二步
            if (heroList != null)
                heroList.Refresh(selectedFaction.AvailableHeroes);

            if (windowManager != null)
                windowManager.NextWindow();
        }

        void SelectHero(HeroConfig hero)
        {
            selectedHero = hero;

            // 自动开始游戏
            StartGame();
        }

        void StartGame()
        {
            if (selectedFaction == null || selectedHero == null)
                return;

            var settings = new NewGameSettings
            {
                PlayerName = "Player",
                SelectedFaction = selectedFaction,
                SelectedHero = selectedHero
            };

            Hide();
            onConfirm?.Invoke(settings);
        }

        /// <summary>
        /// 返回阵营选择（可由 WindowManager 的按钮调用）
        /// </summary>
        public void GoToFactionSelection()
        {
            selectedHero = null;
            if (windowManager != null)
                windowManager.OpenWindowByIndex(0);
        }

        /// <summary>
        /// 取消并关闭窗口
        /// </summary>
        public void Cancel()
        {
            Hide();
            onCancel?.Invoke();
        }

        protected override void OnDestroy()
        {
            // 解绑列表事件
            if (factionList != null)
                factionList.OnFactionSelected -= SelectFaction;

            if (heroList != null)
                heroList.OnHeroSelected -= SelectHero;

            base.OnDestroy();
        }
    }
}
