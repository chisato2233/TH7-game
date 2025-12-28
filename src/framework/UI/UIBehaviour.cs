using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// UI 组件基类，继承自 GameBehaviour
    /// 提供 UI 相关的便捷方法
    /// </summary>
    public class UIBehaviour : GameBehaviour
    {
        [Header("UI Base")]
        [SerializeField] protected CanvasGroup canvasGroup;

        protected RectTransform RectTransform => (RectTransform)transform;

        public virtual bool IsVisible => gameObject.activeSelf;

        public virtual void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public virtual void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
                canvasGroup.interactable = interactable;
        }

        #region Listen - ReactiveList

        /// <summary>
        /// 监听响应式列表变化
        /// </summary>
        protected Subscription Listen<T>(ReactiveList<T> list, System.Action<ReactiveList<T>.ListChangeEvent> callback)
        {
            var sub = list.Watch(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        /// <summary>
        /// 监听响应式列表数量变化
        /// </summary>
        protected Subscription ListenCount<T>(ReactiveList<T> list, System.Action<int> callback)
        {
            var sub = list.WatchCount(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        /// <summary>
        /// 监听响应式列表数量变化（立即触发）
        /// </summary>
        protected Subscription ListenCountImmediate<T>(ReactiveList<T> list, System.Action<int> callback)
        {
            var sub = list.WatchCountImmediate(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        #endregion
    }
}
