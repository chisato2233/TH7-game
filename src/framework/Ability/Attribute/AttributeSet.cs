using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 属性集合 - 管理实体的所有响应式属性
    /// 每个属性都是 ReactiveAttribute，支持 Watch 监听
    /// </summary>
    [Serializable]
    public class AttributeSet
    {
        /// <summary>
        /// 响应式属性字典
        /// </summary>
        readonly Dictionary<string, ReactiveAttribute> attributes = new();

        /// <summary>
        /// 属性定义数据库（可选，用于获取默认值和元数据）
        /// </summary>
        public AttributeDefinitionDatabase DefinitionDatabase { get; set; }

        #region Attribute Access

        /// <summary>
        /// 获取或创建响应式属性
        /// </summary>
        public ReactiveAttribute GetAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (!attributes.TryGetValue(name, out var attr))
            {
                float defaultValue = DefinitionDatabase?.GetDefaultValue(name) ?? 0f;
                attr = new ReactiveAttribute(name, defaultValue);

                if (DefinitionDatabase != null)
                {
                    attr.Definition = DefinitionDatabase.GetDefinition(name);
                }

                attributes[name] = attr;
            }

            return attr;
        }

        /// <summary>
        /// 尝试获取属性（不创建）
        /// </summary>
        public bool TryGetAttribute(string name, out ReactiveAttribute attr)
        {
            if (string.IsNullOrEmpty(name))
            {
                attr = null;
                return false;
            }
            return attributes.TryGetValue(name, out attr);
        }

        /// <summary>
        /// 检查是否存在属性
        /// </summary>
        public bool HasAttribute(string name)
        {
            return !string.IsNullOrEmpty(name) && attributes.ContainsKey(name);
        }

        /// <summary>
        /// 获取所有属性名
        /// </summary>
        public IEnumerable<string> GetAllAttributeNames() => attributes.Keys;

        /// <summary>
        /// 获取所有属性
        /// </summary>
        public IEnumerable<ReactiveAttribute> GetAllAttributes() => attributes.Values;

        #endregion

        #region Value Access (便捷方法)

        /// <summary>
        /// 获取基础值
        /// </summary>
        public float GetBaseValue(string name)
        {
            return GetAttribute(name)?.BaseValue ?? 0f;
        }

        /// <summary>
        /// 设置基础值
        /// </summary>
        public void SetBaseValue(string name, float value)
        {
            if (string.IsNullOrEmpty(name)) return;
            GetAttribute(name).BaseValue = value;
        }

        /// <summary>
        /// 获取当前值（基础值 + 所有修改器）
        /// </summary>
        public float GetCurrentValue(string name)
        {
            return GetAttribute(name)?.Value ?? 0f;
        }

        /// <summary>
        /// 获取当前值（整数）
        /// </summary>
        public int GetCurrentValueInt(string name)
        {
            return GetAttribute(name)?.ValueInt ?? 0;
        }

        #endregion

        #region Modifier Management

        /// <summary>
        /// 添加修改器
        /// </summary>
        public void AddModifier(AttributeModifier modifier)
        {
            if (!modifier.IsValid) return;
            GetAttribute(modifier.Attribute).AddModifier(modifier);
        }

        /// <summary>
        /// 移除修改器
        /// </summary>
        public bool RemoveModifier(AttributeModifier modifier)
        {
            if (!modifier.IsValid) return false;
            return attributes.TryGetValue(modifier.Attribute, out var attr) && attr.RemoveModifier(modifier);
        }

        /// <summary>
        /// 移除来自指定源的所有修改器
        /// </summary>
        public int RemoveModifiersFromSource(object source)
        {
            if (source == null) return 0;

            int totalRemoved = 0;
            foreach (var attr in attributes.Values)
            {
                totalRemoved += attr.RemoveModifiersFromSource(source);
            }
            return totalRemoved;
        }

        /// <summary>
        /// 检查是否有来自指定源的修改器
        /// </summary>
        public bool HasModifierFromSource(object source)
        {
            if (source == null) return false;

            foreach (var attr in attributes.Values)
            {
                if (attr.HasModifierFromSource(source))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定属性的所有修改器
        /// </summary>
        public IReadOnlyList<AttributeModifier> GetModifiers(string name)
        {
            if (attributes.TryGetValue(name, out var attr))
            {
                return attr.Modifiers;
            }
            return Array.Empty<AttributeModifier>();
        }

        /// <summary>
        /// 获取所有修改器
        /// </summary>
        public List<AttributeModifier> GetAllModifiers()
        {
            var result = new List<AttributeModifier>();
            foreach (var attr in attributes.Values)
            {
                result.AddRange(attr.Modifiers);
            }
            return result;
        }

        /// <summary>
        /// 清空所有修改器
        /// </summary>
        public void ClearModifiers()
        {
            foreach (var attr in attributes.Values)
            {
                attr.ClearModifiers();
            }
        }

        #endregion

        #region Watch API

        /// <summary>
        /// 监听指定属性变化
        /// </summary>
        public Subscription Watch(string name, Action<float> callback)
        {
            return GetAttribute(name)?.Watch(callback) ?? default;
        }

        /// <summary>
        /// 监听指定属性变化（立即触发当前值）
        /// </summary>
        public Subscription WatchImmediate(string name, Action<float> callback)
        {
            return GetAttribute(name)?.WatchImmediate(callback) ?? default;
        }

        /// <summary>
        /// 监听一次属性变化
        /// </summary>
        public Subscription WatchOnce(string name, Action<float> callback)
        {
            return GetAttribute(name)?.WatchOnce(callback) ?? default;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化一组属性
        /// </summary>
        public void InitializeAttributes(params (string attribute, float value)[] attrs)
        {
            foreach (var (attribute, value) in attrs)
            {
                if (!string.IsNullOrEmpty(attribute))
                {
                    GetAttribute(attribute).BaseValue = value;
                }
            }
        }

        /// <summary>
        /// 从字典初始化属性
        /// </summary>
        public void InitializeFromDictionary(Dictionary<string, float> attrs)
        {
            foreach (var kvp in attrs)
            {
                if (!string.IsNullOrEmpty(kvp.Key))
                {
                    GetAttribute(kvp.Key).BaseValue = kvp.Value;
                }
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 使所有缓存失效
        /// </summary>
        public void InvalidateCache()
        {
            foreach (var attr in attributes.Values)
            {
                attr.ForceNotify();
            }
        }

        /// <summary>
        /// 复制属性集（只复制基础值，不复制修改器）
        /// </summary>
        public AttributeSet Clone()
        {
            var clone = new AttributeSet();
            clone.DefinitionDatabase = DefinitionDatabase;
            foreach (var kvp in attributes)
            {
                clone.GetAttribute(kvp.Key).BaseValue = kvp.Value.BaseValue;
            }
            return clone;
        }

        /// <summary>
        /// 深度复制属性集（包括修改器）
        /// </summary>
        public AttributeSet DeepClone()
        {
            var clone = Clone();
            foreach (var kvp in attributes)
            {
                var cloneAttr = clone.GetAttribute(kvp.Key);
                foreach (var mod in kvp.Value.Modifiers)
                {
                    cloneAttr.AddModifier(mod);
                }
            }
            return clone;
        }

        /// <summary>
        /// 导出所有基础值为字典
        /// </summary>
        public Dictionary<string, float> ExportBaseValues()
        {
            var result = new Dictionary<string, float>();
            foreach (var kvp in attributes)
            {
                result[kvp.Key] = kvp.Value.BaseValue;
            }
            return result;
        }

        /// <summary>
        /// 导出所有当前值为字典
        /// </summary>
        public Dictionary<string, float> ExportCurrentValues()
        {
            var result = new Dictionary<string, float>();
            foreach (var kvp in attributes)
            {
                result[kvp.Key] = kvp.Value.Value;
            }
            return result;
        }

        #endregion
    }
}
