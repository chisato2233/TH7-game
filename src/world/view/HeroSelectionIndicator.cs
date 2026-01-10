using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄选择指示器
    /// </summary>
    public class HeroSelectionIndicator : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] SpriteRenderer selectionRing;
        [SerializeField] Color selectedColor = new(1f, 0.9f, 0.2f, 0.8f);
        [SerializeField] float pulseSpeed = 2f;
        [SerializeField] float pulseAmount = 0.1f;

        Hero currentHero;
        float baseScale = 1f;

        void Awake()
        {
            if (selectionRing == null)
            {
                var ringGo = new GameObject("SelectionRing");
                ringGo.transform.SetParent(transform);
                selectionRing = ringGo.AddComponent<SpriteRenderer>();
                selectionRing.sortingOrder = -1;
            }

            Hide();
        }

        void Update()
        {
            if (currentHero == null || selectionRing == null || !selectionRing.enabled) return;

            // 跟随英雄位置
            transform.position = currentHero.transform.position;

            // 脉冲动画
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            selectionRing.transform.localScale = Vector3.one * baseScale * pulse;
        }

        /// <summary>
        /// 选中英雄
        /// </summary>
        public void Select(Hero hero)
        {
            currentHero = hero;

            if (hero != null && selectionRing != null)
            {
                transform.position = hero.transform.position;
                selectionRing.color = selectedColor;
                selectionRing.enabled = true;
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void Hide()
        {
            currentHero = null;
            if (selectionRing != null)
                selectionRing.enabled = false;
        }
    }
}
