using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameFramework
{
    public class GameBehaviour : MonoBehaviour
    {
        private List<AutoSubscribeInfo> autoSubscribes = new List<AutoSubscribeInfo>();
        private bool isSubscribed = false;

        protected virtual void Awake()
        {
            RegisterAutoSubscribes(AutoSubscribeTime.Awake);
        }

        protected virtual void Start()
        {
            RegisterAutoSubscribes(AutoSubscribeTime.Start);
        }

        protected virtual void OnDestroy()
        {
            UnregisterAutoSubscribes();
        }

        private void RegisterAutoSubscribes(AutoSubscribeTime time)
        {
            if (isSubscribed) return;

            EventSystem eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            if (eventSystem == null) return;

            Type type = GetType();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<AutoSubscribeAttribute>();
                if (attribute == null || attribute.Time != time) continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;

                Type eventType = parameters[0].ParameterType;
                if (!eventType.IsValueType) continue;

                Type delegateType = typeof(Action<>).MakeGenericType(eventType);
                Delegate handler = Delegate.CreateDelegate(delegateType, this, method);

                MethodInfo subscribeMethod = typeof(EventSystem).GetMethod("Subscribe").MakeGenericMethod(eventType);
                subscribeMethod.Invoke(eventSystem, new object[] { handler });

                autoSubscribes.Add(new AutoSubscribeInfo
                {
                    EventType = eventType,
                    Handler = handler
                });
            }

            if (time == AutoSubscribeTime.Start)
                isSubscribed = true;
        }

        private void UnregisterAutoSubscribes()
        {
            EventSystem eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            if (eventSystem == null) return;

            foreach (var info in autoSubscribes)
            {
                MethodInfo unsubscribeMethod = typeof(EventSystem).GetMethod("Unsubscribe").MakeGenericMethod(info.EventType);
                unsubscribeMethod.Invoke(eventSystem, new object[] { info.Handler });
            }

            autoSubscribes.Clear();
        }

        private class AutoSubscribeInfo
        {
            public Type EventType;
            public Delegate Handler;
        }
    }

    public enum AutoSubscribeTime
    {
        Awake,
        Start
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AutoSubscribeAttribute : Attribute
    {
        public AutoSubscribeTime Time { get; private set; }

        public AutoSubscribeAttribute(AutoSubscribeTime time = AutoSubscribeTime.Awake)
        {
            Time = time;
        }
    }
}

