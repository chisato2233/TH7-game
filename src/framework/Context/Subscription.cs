using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 统一订阅句柄，管理 Event 和 Reactive 的订阅生命周期
    /// </summary>
    public struct Subscription : IDisposable
    {
        private Action disposeAction;
        private bool disposed;

        public Subscription(Action disposeAction)
        {
            this.disposeAction = disposeAction;
            this.disposed = false;
        }

        public bool IsValid => !disposed && disposeAction != null;

        public void Dispose()
        {
            if (!disposed && disposeAction != null)
            {
                disposeAction.Invoke();
                disposeAction = null;
                disposed = true;
            }
        }
    }

    /// <summary>
    /// 订阅管理器，批量管理订阅生命周期
    /// </summary>
    public sealed class SubscriptionList : IDisposable
    {
        private List<Subscription> subscriptions = new();

        public void Add(Subscription subscription)
        {
            if (subscription.IsValid)
                subscriptions.Add(subscription);
        }

        public void Dispose()
        {
            foreach (var sub in subscriptions)
                sub.Dispose();
            subscriptions.Clear();
        }

        public static SubscriptionList operator +(SubscriptionList list, Subscription sub)
        {
            list.Add(sub);
            return list;
        }
    }
}
