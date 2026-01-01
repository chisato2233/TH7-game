using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 属性定义 - 描述一个属性的元数据
    /// 可用于编辑器预定义或运行时动态创建
    /// </summary>
    [Serializable]
    public class AttributeDefinition
    {
        /// <summary>
        /// 属性名称（唯一标识）
        /// </summary>
        public string Name;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// 默认值
        /// </summary>
        public float DefaultValue;

        /// <summary>
        /// 最小值
        /// </summary>
        public float MinValue = float.MinValue;

        /// <summary>
        /// 最大值
        /// </summary>
        public float MaxValue = float.MaxValue;

        /// <summary>
        /// 是否为整数类型
        /// </summary>
        public bool IsInteger;

        /// <summary>
        /// 属性分类标签（用于 UI 分组）
        /// </summary>
        public string Category;

        public AttributeDefinition() { }

        public AttributeDefinition(string name, float defaultValue = 0f, bool isInteger = false)
        {
            Name = name;
            DisplayName = name;
            DefaultValue = defaultValue;
            IsInteger = isInteger;
        }

        /// <summary>
        /// 钳制值到有效范围
        /// </summary>
        public float Clamp(float value)
        {
            value = Mathf.Clamp(value, MinValue, MaxValue);
            return IsInteger ? Mathf.Round(value) : value;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// 属性定义数据库 (ScriptableObject)
    /// 用于在编辑器中预定义属性列表
    /// </summary>
    [CreateAssetMenu(fileName = "AttributeDefinitions", menuName = "TH7/Ability/Attribute Definitions")]
    public class AttributeDefinitionDatabase : ScriptableObject
    {
        [SerializeField]
        AttributeDefinition[] definitions;

        public AttributeDefinition[] Definitions => definitions;

        /// <summary>
        /// 获取属性定义
        /// </summary>
        public AttributeDefinition GetDefinition(string name)
        {
            if (definitions == null) return null;

            foreach (var def in definitions)
            {
                if (def.Name == name) return def;
            }
            return null;
        }

        /// <summary>
        /// 检查属性是否存在
        /// </summary>
        public bool HasDefinition(string name)
        {
            return GetDefinition(name) != null;
        }

        /// <summary>
        /// 获取属性默认值
        /// </summary>
        public float GetDefaultValue(string name)
        {
            var def = GetDefinition(name);
            return def?.DefaultValue ?? 0f;
        }
    }

    /// <summary>
    /// 修改器运算类型
    /// </summary>
    public enum ModifierOp
    {
        /// <summary>
        /// 加法 (BaseValue + ModifierValue)
        /// </summary>
        Add = 0,

        /// <summary>
        /// 乘法 (BaseValue * ModifierValue)
        /// 注意: 值为 1.0 表示无修改, 1.5 表示增加 50%
        /// </summary>
        Multiply = 1,

        /// <summary>
        /// 覆盖 (直接使用 ModifierValue)
        /// </summary>
        Override = 2
    }
}
