using System;
using System.Collections.Generic;
using UnityEngine;
using TH7;

namespace GameFramework
{
    /// <summary>
    /// MonoBehaviour 扩展基类，提供自动事件订阅和响应式数据监听
    /// </summary>
    public class GameBehaviour : MonoBehaviour
    {
        private List<ActiveSubscription> autoSubscriptions;
        private SubscriptionList subscriptions;
        private bool isSubscribed;

        protected SubscriptionList Subscriptions => subscriptions ??= new SubscriptionList();

        protected virtual void Awake() => ProcessAutoSubscribe(AutoSubscribeTime.Awake);

        protected virtual void Start() => ProcessAutoSubscribe(AutoSubscribeTime.Start);

        protected virtual void OnDestroy()
        {
            UnregisterAutoSubscribes();
            subscriptions?.Dispose();
        }

        #region Listen - Event

        /// <summary>
        /// 订阅事件
        /// </summary>
        protected Subscription Listen<T>(Action<T> callback) where T : struct
        {
            var eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            if (eventSystem == null) return default;

            eventSystem.Subscribe(callback);
            var sub = new Subscription(() => eventSystem.Unsubscribe(callback));
            Subscriptions.Add(sub);
            return sub;
        }

        #endregion

        #region Listen - Reactive

        /// <summary>
        /// 监听响应式数据
        /// </summary>
        protected Subscription Listen<T>(Reactive<T> reactive, Action<T> callback)
        {
            var sub = reactive.Watch(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        /// <summary>
        /// 监听响应式数据并立即执行一次
        /// </summary>
        protected Subscription ListenImmediate<T>(Reactive<T> reactive, Action<T> callback)
        {
            var sub = reactive.WatchImmediate(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        /// <summary>
        /// 只监听一次响应式数据变化
        /// </summary>
        protected Subscription ListenOnce<T>(Reactive<T> reactive, Action<T> callback)
        {
            var sub = reactive.WatchOnce(callback);
            Subscriptions.Add(sub);
            return sub;
        }

        #endregion

        #region AutoSubscribe

        private void ProcessAutoSubscribe(AutoSubscribeTime time)
        {
            if (isSubscribed) return;

            var eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            if (eventSystem == null) return;

            var subs = AutoSubscribeProcessor.Register(this, time, eventSystem);
            if (subs is { Count: > 0 })
            {
                autoSubscriptions ??= new List<ActiveSubscription>();
                autoSubscriptions.AddRange(subs);
            }

            if (time == AutoSubscribeTime.Start)
                isSubscribed = true;
        }

        private void UnregisterAutoSubscribes()
        {
            var eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            AutoSubscribeProcessor.Unregister(autoSubscriptions, eventSystem);
            autoSubscriptions = null;
        }

        #endregion
    }
}
