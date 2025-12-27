using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 响应式数据容器
    /// </summary>
    [Serializable]
    public class Reactive<T> : ISerializationCallbackReceiver
    {
        [SerializeField] private T serializedValue;

        private T currentValue;
        private Dictionary<int, Action<T>> listeners = new();
        private int nextId;

        public Reactive() => currentValue = default;

        public Reactive(T initialValue)
        {
            currentValue = initialValue;
            serializedValue = initialValue;
        }

        public T Value
        {
            get => currentValue;
            set
            {
                if (EqualityComparer<T>.Default.Equals(currentValue, value)) return;
                currentValue = value;
                NotifyListeners();
            }
        }

        public void SetSilent(T value) => currentValue = value;

        public void NotifyListeners()
        {
            foreach (var listener in listeners.Values)
                listener?.Invoke(currentValue);
        }

        public Subscription Watch(Action<T> callback)
        {
            if (callback == null) return default;

            int id = nextId++;
            listeners[id] = callback;
            return new Subscription(() => listeners.Remove(id));
        }

        public Subscription WatchImmediate(Action<T> callback)
        {
            callback?.Invoke(currentValue);
            return Watch(callback);
        }

        public Subscription WatchOnce(Action<T> callback)
        {
            if (callback == null) return default;

            int id = nextId++;
            Action<T> wrapper = null;
            wrapper = value =>
            {
                callback(value);
                listeners.Remove(id);
            };
            listeners[id] = wrapper;
            return new Subscription(() => listeners.Remove(id));
        }

        public static implicit operator T(Reactive<T> reactive) => reactive.Value;

        public void OnBeforeSerialize() => serializedValue = currentValue;

        public void OnAfterDeserialize() => currentValue = serializedValue;

        public override string ToString() => currentValue?.ToString() ?? "null";
    }

    [Serializable] public class ReactiveInt : Reactive<int>
    {
        public ReactiveInt() : base() { }
        public ReactiveInt(int value) : base(value) { }
    }

    [Serializable] public class ReactiveFloat : Reactive<float>
    {
        public ReactiveFloat() : base() { }
        public ReactiveFloat(float value) : base(value) { }
    }

    [Serializable] public class ReactiveString : Reactive<string>
    {
        public ReactiveString() : base() { }
        public ReactiveString(string value) : base(value) { }
    }

    [Serializable] public class ReactiveBool : Reactive<bool>
    {
        public ReactiveBool() : base() { }
        public ReactiveBool(bool value) : base(value) { }
    }
}
