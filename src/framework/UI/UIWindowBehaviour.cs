using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace GameFramework
{
    /// <summary>
    /// 窗口 UI 基类，提供自定义显示/隐藏动画
    /// 可选择使用 DOTween 或 Animator 实现动画
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIWindowBehaviour : UIBehaviour
    {
        [Header("Window Settings")]
        [SerializeField] protected AnimationType animationType = AnimationType.DOTween;
        [SerializeField] protected float animationDuration = 0.3f;

        [Header("DOTween Settings")]
        [SerializeField] protected Ease showEase = Ease.OutBack;
        [SerializeField] protected Ease hideEase = Ease.InBack;
        [SerializeField] protected Vector3 hiddenScale = new Vector3(0.8f, 0.8f, 1f);

        [Header("Animator Settings")]
        [SerializeField] protected Animator windowAnimator;
        [SerializeField] protected string showStateName = "Show";
        [SerializeField] protected string hideStateName = "Hide";

        [Header("Events")]
        public Action OnShowStart;
        public Action OnShowComplete;
        public Action OnHideStart;
        public Action OnHideComplete;

        protected bool isShowing;
        protected Sequence currentSequence;

        public bool IsShowing => isShowing;

        public enum AnimationType
        {
            None,
            DOTween,
            Animator
        }

        protected override void Awake()
        {
            base.Awake();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // 初始状态隐藏
            if (!isShowing)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }

        public override void Show()
        {
            if (isShowing) return;
            isShowing = true;

            gameObject.SetActive(true);
            OnShowStart?.Invoke();

            switch (animationType)
            {
                case AnimationType.DOTween:
                    PlayDOTweenShow();
                    break;
                case AnimationType.Animator:
                    PlayAnimatorShow();
                    break;
                default:
                    ShowImmediate();
                    break;
            }
        }

        public override void Hide()
        {
            if (!isShowing) return;
            isShowing = false;

            OnHideStart?.Invoke();

            switch (animationType)
            {
                case AnimationType.DOTween:
                    PlayDOTweenHide();
                    break;
                case AnimationType.Animator:
                    PlayAnimatorHide();
                    break;
                default:
                    HideImmediate();
                    break;
            }
        }

        #region Immediate

        void ShowImmediate()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;
            OnShowComplete?.Invoke();
        }

        void HideImmediate()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            OnHideComplete?.Invoke();
        }

        #endregion

        #region DOTween

        void PlayDOTweenShow()
        {
            currentSequence?.Kill();

            transform.localScale = hiddenScale;
            canvasGroup.alpha = 0f;

            currentSequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, animationDuration))
                .Join(transform.DOScale(Vector3.one, animationDuration).SetEase(showEase))
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    OnShowComplete?.Invoke();
                });
        }

        void PlayDOTweenHide()
        {
            currentSequence?.Kill();

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            currentSequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(0f, animationDuration))
                .Join(transform.DOScale(hiddenScale, animationDuration).SetEase(hideEase))
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    OnHideComplete?.Invoke();
                });
        }

        #endregion

        #region Animator

        void PlayAnimatorShow()
        {
            if (windowAnimator == null)
            {
                ShowImmediate();
                return;
            }

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            windowAnimator.Play(showStateName);
            StartCoroutine(WaitForAnimationComplete(showStateName, () => OnShowComplete?.Invoke()));
        }

        void PlayAnimatorHide()
        {
            if (windowAnimator == null)
            {
                HideImmediate();
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            windowAnimator.Play(hideStateName);
            StartCoroutine(WaitForAnimationComplete(hideStateName, () =>
            {
                gameObject.SetActive(false);
                OnHideComplete?.Invoke();
            }));
        }

        IEnumerator WaitForAnimationComplete(string stateName, Action onComplete)
        {
            // 等待动画开始
            yield return null;

            // 获取当前动画状态信息
            var stateInfo = windowAnimator.GetCurrentAnimatorStateInfo(0);
            float length = stateInfo.length;

            yield return new WaitForSecondsRealtime(length);
            onComplete?.Invoke();
        }

        #endregion

        protected override void OnDestroy()
        {
            currentSequence?.Kill();
            base.OnDestroy();
        }
    }
}
