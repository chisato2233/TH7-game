using System;

namespace GameFramework
{
    /// <summary>
    /// 属性修改器 - 描述如何修改一个属性
    /// 使用字符串标识属性，支持任意属性名
    /// </summary>
    [Serializable]
    public struct AttributeModifier : IEquatable<AttributeModifier>
    {
        /// <summary>
        /// 要修改的属性名称
        /// </summary>
        public string Attribute;

        /// <summary>
        /// 运算类型
        /// </summary>
        public ModifierOp Operation;

        /// <summary>
        /// 修改值
        /// </summary>
        public float Value;

        /// <summary>
        /// 修改器来源（用于追踪和移除）
        /// </summary>
        [NonSerialized]
        public object Source;

        /// <summary>
        /// 修改器优先级（同类型运算时的顺序）
        /// </summary>
        public int Priority;

        public AttributeModifier(string attribute, ModifierOp operation, float value, object source = null, int priority = 0)
        {
            Attribute = attribute;
            Operation = operation;
            Value = value;
            Source = source;
            Priority = priority;
        }

        /// <summary>
        /// 创建加法修改器
        /// </summary>
        public static AttributeModifier Add(string attribute, float value, object source = null)
        {
            return new AttributeModifier(attribute, ModifierOp.Add, value, source);
        }

        /// <summary>
        /// 创建乘法修改器
        /// </summary>
        public static AttributeModifier Multiply(string attribute, float value, object source = null)
        {
            return new AttributeModifier(attribute, ModifierOp.Multiply, value, source);
        }

        /// <summary>
        /// 创建覆盖修改器
        /// </summary>
        public static AttributeModifier Override(string attribute, float value, object source = null)
        {
            return new AttributeModifier(attribute, ModifierOp.Override, value, source);
        }

        /// <summary>
        /// 检查属性名是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Attribute);

        public bool Equals(AttributeModifier other)
        {
            return Attribute == other.Attribute &&
                   Operation == other.Operation &&
                   Math.Abs(Value - other.Value) < 0.0001f &&
                   ReferenceEquals(Source, other.Source);
        }

        public override bool Equals(object obj) => obj is AttributeModifier other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Attribute, Operation, Value, Source);

        public override string ToString()
        {
            string opStr = Operation switch
            {
                ModifierOp.Add => "+",
                ModifierOp.Multiply => "*",
                ModifierOp.Override => "=",
                _ => "?"
            };
            return $"{Attribute} {opStr} {Value}";
        }
    }
}
