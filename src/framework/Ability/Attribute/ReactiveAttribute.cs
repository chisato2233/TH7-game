using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 响应式属性 - 支持修改器和监听的属性值
    /// 每次基础值或修改器变化时都会触发 Watch 回调
    /// </summary>
    [Serializable]
    public class ReactiveAttribute
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 基础值
        /// </summary>
        float baseValue;

        /// <summary>
        /// 当前生效的修改器列表
        /// </summary>
        readonly List<AttributeModifier> modifiers = new();

        /// <summary>
        /// 缓存的计算结果
        /// </summary>
        float cachedValue;

        /// <summary>
        /// 缓存是否有效
        /// </summary>
        bool cacheValid;

        /// <summary>
        /// 监听器
        /// </summary>
        Dictionary<int, Action<float>> listeners = new();
        int nextListenerId;

        /// <summary>
        /// 属性定义（可选，用于获取元数据）
        /// </summary>
        public AttributeDefinition Definition { get; set; }

        public ReactiveAttribute(string name, float initialValue = 0f)
        {
            Name = name;
            baseValue = initialValue;
            cachedValue = initialValue;
            cacheValid = true;
        }

        /// <summary>
        /// 基础值
        /// </summary>
        public float BaseValue
        {
            get => baseValue;
            set
            {
                if (Math.Abs(baseValue - value) < 0.0001f) return;
                baseValue = value;
                InvalidateAndNotify();
            }
        }

        /// <summary>
        /// 当前值（基础值 + 所有修改器）
        /// </summary>
        public float Value
        {
            get
            {
                if (!cacheValid)
                {
                    cachedValue = CalculateValue();
                    cacheValid = true;
                }
                return cachedValue;
            }
        }

        /// <summary>
        /// 当前值（整数）
        /// </summary>
        public int ValueInt => (int)Math.Round(Value);

        /// <summary>
        /// 修改器数量
        /// </summary>
        public int ModifierCount => modifiers.Count;

        /// <summary>
        /// 获取所有修改器（只读）
        /// </summary>
        public IReadOnlyList<AttributeModifier> Modifiers => modifiers;

        #region Modifier Management

        /// <summary>
        /// 添加修改器
        /// </summary>
        public void AddModifier(AttributeModifier modifier)
        {
            if (!modifier.IsValid || modifier.Attribute != Name) return;
            modifiers.Add(modifier);
            InvalidateAndNotify();
        }

        /// <summary>
        /// 移除修改器
        /// </summary>
        public bool RemoveModifier(AttributeModifier modifier)
        {
            bool removed = modifiers.Remove(modifier);
            if (removed)
            {
                InvalidateAndNotify();
            }
            return removed;
        }

        /// <summary>
        /// 移除来自指定源的所有修改器
        /// </summary>
        public int RemoveModifiersFromSource(object source)
        {
            if (source == null) return 0;

            int removed = modifiers.RemoveAll(m => ReferenceEquals(m.Source, source));
            if (removed > 0)
            {
                InvalidateAndNotify();
            }
            return removed;
        }

        /// <summary>
        /// 检查是否有来自指定源的修改器
        /// </summary>
        public bool HasModifierFromSource(object source)
        {
            if (source == null) return false;
            foreach (var mod in modifiers)
            {
                if (ReferenceEquals(mod.Source, source))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有修改器
        /// </summary>
        public void ClearModifiers()
        {
            if (modifiers.Count == 0) return;
            modifiers.Clear();
            InvalidateAndNotify();
        }

        #endregion

        #region Calculation

        /// <summary>
        /// 计算属性最终值
        /// 顺序: BaseValue -> Add -> Multiply -> Override
        /// </summary>
        float CalculateValue()
        {
            float addTotal = 0f;
            float multiplyTotal = 1f;
            AttributeModifier? overrideMod = null;
            int overridePriority = int.MinValue;

            foreach (var mod in modifiers)
            {
                switch (mod.Operation)
                {
                    case ModifierOp.Add:
                        addTotal += mod.Value;
                        break;
                    case ModifierOp.Multiply:
                        multiplyTotal *= mod.Value;
                        break;
                    case ModifierOp.Override:
                        if (mod.Priority >= overridePriority)
                        {
                            overrideMod = mod;
                            overridePriority = mod.Priority;
                        }
                        break;
                }
            }

            // 如果有覆盖修改器，直接返回覆盖值
            if (overrideMod.HasValue)
            {
                return ClampValue(overrideMod.Value.Value);
            }

            // 计算: (Base + Add) * Multiply
            float result = (baseValue + addTotal) * multiplyTotal;
            return ClampValue(result);
        }

        /// <summary>
        /// 根据定义钳制值
        /// </summary>
        float ClampValue(float value)
        {
            if (Definition != null)
            {
                return Definition.Clamp(value);
            }
            return value;
        }

        /// <summary>
        /// 使缓存失效并通知监听器
        /// </summary>
        void InvalidateAndNotify()
        {
            float oldValue = cachedValue;
            cacheValid = false;
            float newValue = Value; // 重新计算

            // 只在值实际变化时通知
            if (Math.Abs(oldValue - newValue) > 0.0001f)
            {
                NotifyListeners();
            }
        }

        #endregion

        #region Reactive API

        /// <summary>
        /// 监听属性变化
        /// </summary>
        public Subscription Watch(Action<float> callback)
        {
            if (callback == null) return default;

            int id = nextListenerId++;
            listeners[id] = callback;
            return new Subscription(() => listeners.Remove(id));
        }

        /// <summary>
        /// 监听属性变化（立即触发当前值）
        /// </summary>
        public Subscription WatchImmediate(Action<float> callback)
        {
            callback?.Invoke(Value);
            return Watch(callback);
        }

        /// <summary>
        /// 监听一次属性变化
        /// </summary>
        public Subscription WatchOnce(Action<float> callback)
        {
            if (callback == null) return default;

            int id = nextListenerId++;
            Action<float> wrapper = null;
            wrapper = value =>
            {
                callback(value);
                listeners.Remove(id);
            };
            listeners[id] = wrapper;
            return new Subscription(() => listeners.Remove(id));
        }

        /// <summary>
        /// 通知所有监听器
        /// </summary>
        void NotifyListeners()
        {
            float currentValue = Value;
            foreach (var listener in listeners.Values)
            {
                listener?.Invoke(currentValue);
            }
        }

        /// <summary>
        /// 强制通知监听器（即使值没有变化）
        /// </summary>
        public void ForceNotify()
        {
            NotifyListeners();
        }

        #endregion

        /// <summary>
        /// 隐式转换为 float
        /// </summary>
        public static implicit operator float(ReactiveAttribute attr) => attr.Value;

        public override string ToString()
        {
            return $"{Name}: {Value:F2} (Base: {baseValue:F2}, Mods: {modifiers.Count})";
        }
    }
}
