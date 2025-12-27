using System;
using System.Collections.Generic;
using System.Reflection;
using TH7;

namespace GameFramework
{
    /// <summary>
    /// 自动订阅处理器，缓存反射结果以提升性能
    /// </summary>
    public static class AutoSubscribeProcessor
    {
        private static readonly Dictionary<Type, List<AutoSubscribeMethodInfo>> cachedMethods = new();
        private static readonly MethodInfo subscribeMethod;
        private static readonly MethodInfo unsubscribeMethod;

        static AutoSubscribeProcessor()
        {
            subscribeMethod = typeof(EventSystem).GetMethod("Subscribe");
            unsubscribeMethod = typeof(EventSystem).GetMethod("Unsubscribe");
        }

        /// <summary>
        /// 获取某个类型的所有自动订阅方法信息（带缓存）
        /// </summary>
        public static List<AutoSubscribeMethodInfo> GetMethodsForType(Type type)
        {
            if (cachedMethods.TryGetValue(type, out var methods))
                return methods;

            methods = new List<AutoSubscribeMethodInfo>();
            var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in allMethods)
            {
                var attribute = method.GetCustomAttribute<AutoSubscribeAttribute>();
                if (attribute == null) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;

                Type eventType = attribute.TargetType ?? parameters[0].ParameterType;
                if (!eventType.IsValueType) continue;

                methods.Add(new AutoSubscribeMethodInfo
                {
                    Method = method,
                    EventType = eventType,
                    Time = attribute.Time,
                    SubscribeMethod = subscribeMethod.MakeGenericMethod(eventType),
                    UnsubscribeMethod = unsubscribeMethod.MakeGenericMethod(eventType)
                });
            }

            cachedMethods[type] = methods;
            return methods;
        }

        /// <summary>
        /// 为实例注册自动订阅（指定时机）
        /// </summary>
        public static List<ActiveSubscription> Register(object instance, AutoSubscribeTime time, EventSystem eventSystem)
        {
            if (eventSystem == null) return null;

            var result = new List<ActiveSubscription>();
            var methods = GetMethodsForType(instance.GetType());

            foreach (var info in methods)
            {
                if (info.Time != time) continue;

                Type delegateType = typeof(Action<>).MakeGenericType(info.EventType);
                Delegate handler = Delegate.CreateDelegate(delegateType, instance, info.Method);

                info.SubscribeMethod.Invoke(eventSystem, new object[] { handler });

                result.Add(new ActiveSubscription
                {
                    EventType = info.EventType,
                    Handler = handler,
                    UnsubscribeMethod = info.UnsubscribeMethod
                });
            }

            return result;
        }

        /// <summary>
        /// 注销活动订阅列表
        /// </summary>
        public static void Unregister(List<ActiveSubscription> subscriptions, EventSystem eventSystem)
        {
            if (eventSystem == null || subscriptions == null) return;

            foreach (var sub in subscriptions)
                sub.UnsubscribeMethod.Invoke(eventSystem, new object[] { sub.Handler });

            subscriptions.Clear();
        }
    }

    /// <summary>
    /// 自动订阅方法的缓存信息
    /// </summary>
    public class AutoSubscribeMethodInfo
    {
        public MethodInfo Method;
        public Type EventType;
        public AutoSubscribeTime Time;
        public MethodInfo SubscribeMethod;
        public MethodInfo UnsubscribeMethod;
    }

    /// <summary>
    /// 活动订阅记录，用于注销
    /// </summary>
    public class ActiveSubscription
    {
        public Type EventType;
        public Delegate Handler;
        public MethodInfo UnsubscribeMethod;
    }
}
