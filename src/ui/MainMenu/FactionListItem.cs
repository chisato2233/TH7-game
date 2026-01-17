using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 阵营列表项 - 带点击动画
    /// </summary>
    public class FactionListItem : ScrollViewItem<FactionConfig>, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI References")]
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Image iconImage;

        [Header("Click Animation")]
        [SerializeField] float clickScale = 0.9f;
        [SerializeField] float clickDuration = 0.1f;
        [SerializeField] Ease clickEase = Ease.OutQuad;

        [Header("Hover Animation")]
        [SerializeField] float hoverScale = 1.05f;
        [SerializeField] float hoverDuration = 0.15f;
        [SerializeField] Ease hoverEase = Ease.OutQuad;

        public event Action<FactionConfig> OnClicked;

        RectTransform rectTransform;
        Vector3 originalScale;
        Tween currentTween;
        bool isPointerDown;
        bool isPointerInside;

        protected override void Awake()
        {
            base.Awake();
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
                originalScale = rectTransform.localScale;
        }

        protected override void OnSetup()
        {
            if (nameText != null)
                nameText.text = data?.DisplayName ?? "";

            if (iconImage != null)
            {
                if (data?.Icon != null)
                {
                    iconImage.sprite = data.Icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                }
            }
        }

        #region Pointer Events

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Debug.Log($"[FactionListItem] Clicked: {data?.DisplayName}, Subscribers: {OnClicked?.GetInvocationList()?.Length ?? 0}");
            OnClicked?.Invoke(data);
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

        #endregion

        void AnimateScale(Vector3 targetScale, float duration, Ease ease)
        {
            if (rectTransform == null)
                return;

            currentTween?.Kill();
            currentTween = rectTransform
                .DOScale(targetScale, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        protected override void OnDestroy()
        {
            currentTween?.Kill();
            base.OnDestroy();
        }
    }
}
