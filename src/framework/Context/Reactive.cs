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
        // ES3 序列化用的属性（ES3 只存这个）
        [ES3Serializable]
        public T SavedValue
        {
            get => currentValue;
            set => currentValue = value;
        }

        // Unity 序列化用（ES3 会忽略）
        [SerializeField, ES3NonSerializable] private T serializedValue;

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

    /// <summary>
    /// 响应式列表容器，支持 Add/Remove/Clear 等操作的监听
    /// </summary>
    [Serializable]
    public class ReactiveList<T>
    {
        public enum ChangeType { Add, Remove, Replace, Clear, Reset }

        public readonly struct ListChangeEvent
        {
            public readonly ChangeType Type;
            public readonly int Index;
            public readonly T OldValue;
            public readonly T NewValue;

            public ListChangeEvent(ChangeType type, int index = -1, T oldValue = default, T newValue = default)
            {
                Type = type;
                Index = index;
                OldValue = oldValue;
                NewValue = newValue;
            }
        }

        // ES3 序列化用（只存储列表数据）
        [ES3Serializable]
        public List<T> SavedItems
        {
            get => items;
            set => items = value ?? new List<T>();
        }

        // Unity 序列化用（ES3 会忽略）
        [SerializeField, ES3NonSerializable] private List<T> items = new();

        // 运行时状态（不存储，延迟初始化以支持反序列化）
        [NonSerialized] private Dictionary<int, Action<ListChangeEvent>> listeners;
        [NonSerialized] private Dictionary<int, Action<int>> countListeners;
        [NonSerialized] private int nextId;

        private Dictionary<int, Action<ListChangeEvent>> Listeners =>
            listeners ??= new Dictionary<int, Action<ListChangeEvent>>();
        private Dictionary<int, Action<int>> CountListeners =>
            countListeners ??= new Dictionary<int, Action<int>>();

        public ReactiveList() { }

        public ReactiveList(IEnumerable<T> collection)
        {
            items = new List<T>(collection);
        }

        // 基础属性
        public int Count => items.Count;
        public T this[int index]
        {
            get => items[index];
            set
            {
                T old = items[index];
                items[index] = value;
                NotifyChange(new ListChangeEvent(ChangeType.Replace, index, old, value));
            }
        }

        // 列表操作
        public void Add(T item)
        {
            items.Add(item);
            NotifyChange(new ListChangeEvent(ChangeType.Add, items.Count - 1, newValue: item));
            NotifyCountChanged();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            int startIndex = items.Count;
            items.AddRange(collection);
            // 批量添加时只触发一次 Reset
            NotifyChange(new ListChangeEvent(ChangeType.Reset));
            NotifyCountChanged();
        }

        public bool Remove(T item)
        {
            int index = items.IndexOf(item);
            if (index < 0) return false;

            items.RemoveAt(index);
            NotifyChange(new ListChangeEvent(ChangeType.Remove, index, oldValue: item));
            NotifyCountChanged();
            return true;
        }

        public void RemoveAt(int index)
        {
            T item = items[index];
            items.RemoveAt(index);
            NotifyChange(new ListChangeEvent(ChangeType.Remove, index, oldValue: item));
            NotifyCountChanged();
        }

        public void Insert(int index, T item)
        {
            items.Insert(index, item);
            NotifyChange(new ListChangeEvent(ChangeType.Add, index, newValue: item));
            NotifyCountChanged();
        }

        public void Clear()
        {
            items.Clear();
            NotifyChange(new ListChangeEvent(ChangeType.Clear));
            NotifyCountChanged();
        }

        public bool Contains(T item) => items.Contains(item);
        public int IndexOf(T item) => items.IndexOf(item);
        public T Find(Predicate<T> match) => items.Find(match);
        public List<T> FindAll(Predicate<T> match) => items.FindAll(match);
        public void ForEach(Action<T> action) => items.ForEach(action);

        // 获取底层列表的只读快照
        public IReadOnlyList<T> AsReadOnly() => items.AsReadOnly();
        public List<T> ToList() => new List<T>(items);

        // 整体替换（触发 Reset）
        public void Reset(IEnumerable<T> newItems)
        {
            items.Clear();
            if (newItems != null)
                items.AddRange(newItems);
            NotifyChange(new ListChangeEvent(ChangeType.Reset));
            NotifyCountChanged();
        }

        // 监听任意变化
        public Subscription Watch(Action<ListChangeEvent> callback)
        {
            if (callback == null) return default;
            int id = nextId++;
            Listeners[id] = callback;
            return new Subscription(() => Listeners.Remove(id));
        }

        // 监听数量变化
        public Subscription WatchCount(Action<int> callback)
        {
            if (callback == null) return default;
            int id = nextId++;
            CountListeners[id] = callback;
            return new Subscription(() => CountListeners.Remove(id));
        }

        // 监听数量变化（立即触发当前值）
        public Subscription WatchCountImmediate(Action<int> callback)
        {
            callback?.Invoke(items.Count);
            return WatchCount(callback);
        }

        private void NotifyChange(ListChangeEvent evt)
        {
            if (listeners == null) return;
            foreach (var listener in listeners.Values)
                listener?.Invoke(evt);
        }

        private void NotifyCountChanged()
        {
            if (countListeners == null) return;
            int count = items.Count;
            foreach (var listener in countListeners.Values)
                listener?.Invoke(count);
        }

        // 支持 foreach
        public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();
    }
}
