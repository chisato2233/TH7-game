using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 选中英雄信息显示 UI
    /// 绑定到 PlayerActionProvider.SelectedHero
    /// </summary>
    public class SelectedHeroInfoUI : UIBehaviour
    {
        [Header("Hero Info")]
        [SerializeField] TextMeshProUGUI heroNameText;
        [SerializeField] Image heroPortrait;
        [SerializeField] TextMeshProUGUI movementText;

        [Header("Format")]
        [SerializeField] string movementFormat = "MP: {0}/{1}";

        [Header("Container")]
        [SerializeField] GameObject infoContainer;

        Hero boundHero;
        Subscription movementSubscription;

        /// <summary>
        /// 绑定到 PlayerActionProvider
        /// </summary>
        public void Bind(PlayerActionProvider provider)
        {
            if (provider?.SelectedHero != null)
                ListenImmediate(provider.SelectedHero, OnSelectedHeroChanged);
        }

        void OnSelectedHeroChanged(Hero hero)
        {
            // 清除之前的移动力监听
            movementSubscription.Dispose();
            boundHero = hero;

            if (hero != null)
            {
                ShowHeroInfo(hero);

                // 监听移动力变化
                if (hero.MovementPoints != null)
                    movementSubscription = ListenImmediate(hero.MovementPoints, OnMovementChanged);
            }
            else
            {
                HideHeroInfo();
            }
        }

        void ShowHeroInfo(Hero hero)
        {
            if (infoContainer != null)
                infoContainer.SetActive(true);

            if (heroNameText != null)
                heroNameText.text = hero.HeroName;

            // 从 HeroConfig 获取头像
            if (heroPortrait != null && hero.Config?.Portrait != null)
                heroPortrait.sprite = hero.Config.Portrait;

            UpdateMovementDisplay(hero.MovementPoints?.Value ?? 0);
        }

        void HideHeroInfo()
        {
            if (infoContainer != null)
                infoContainer.SetActive(false);
        }

        void OnMovementChanged(int currentMovement)
        {
            UpdateMovementDisplay(currentMovement);
        }

        void UpdateMovementDisplay(int current)
        {
            if (movementText == null || boundHero == null) return;

            int max = boundHero.MaxMovementPoints;
            movementText.text = string.Format(movementFormat, current, max);
        }
    }
}
