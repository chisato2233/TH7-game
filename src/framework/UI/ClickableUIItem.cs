using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace GameFramework
{
    /// <summary>
    /// 可点击的 UI 项基类，带 DOTween 动画效果
    /// </summary>
    public class ClickableUIItem : UIBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Click Animation")]
        [SerializeField] protected float clickScale = 0.9f;
        [SerializeField] protected float clickDuration = 0.1f;
        [SerializeField] protected Ease clickEase = Ease.OutQuad;

        [Header("Hover Animation")]
        [SerializeField] protected float hoverScale = 1.05f;
        [SerializeField] protected float hoverDuration = 0.15f;
        [SerializeField] protected Ease hoverEase = Ease.OutQuad;

        public event Action OnClicked;

        protected RectTransform rectTransform;
        protected Vector3 originalScale;
        protected Tween currentTween;
        protected bool isPointerDown;
        protected bool isPointerInside;

        protected override void Awake()
        {
            base.Awake();
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
                originalScale = rectTransform.localScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OnClicked?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = true;
            AnimateScale(originalScale * clickScale, clickDuration, clickEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            isPointerDown = false;

            // 恢复到 hover 或原始状态
            if (isPointerInside)
                AnimateScale(originalScale * hoverScale, clickDuration, clickEase);
            else
                AnimateScale(originalScale, clickDuration, clickEase);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;

            if (!isPointerDown)
                AnimateScale(originalScale * hoverScale, hoverDuration, hoverEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;

            if (!isPointerDown)
                AnimateScale(originalScale, hoverDuration, hoverEase);
        }

        protected void AnimateScale(Vector3 targetScale, float duration, Ease ease)
        {
            if (rectTransform == null)
                return;

            currentTween?.Kill();
            currentTween = rectTransform
                .DOScale(targetScale, duration)
                .SetEase(ease)
                .SetUpdate(true); // 忽略 timeScale
        }

        protected override void OnDestroy()
        {
            currentTween?.Kill();
            base.OnDestroy();
        }
    }
}
