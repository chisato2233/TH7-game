using System;

namespace GameFramework
{
    /// <summary>
    /// Gameplay 标签 - 层级化字符串标签
    /// 例如: "Status.Buff.Haste", "Ability.Spell.Fire"
    /// </summary>
    [Serializable]
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        public static readonly GameplayTag None = new(string.Empty);

        readonly string value;

        /// <summary>
        /// 标签值
        /// </summary>
        public string Value => value ?? string.Empty;

        /// <summary>
        /// 标签是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(value);

        public GameplayTag(string value)
        {
            this.value = value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 精确匹配
        /// </summary>
        public bool Matches(GameplayTag other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查是否有指定父标签
        /// 例如: "Status.Buff.Haste".HasParent("Status.Buff") == true
        /// </summary>
        public bool HasParent(GameplayTag parent)
        {
            if (!IsValid || !parent.IsValid) return false;
            if (Matches(parent)) return true;

            return Value.StartsWith(parent.Value + ".", StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查是否匹配任一标签（包含层级匹配）
        /// </summary>
        public bool MatchesAny(GameplayTagContainer container)
        {
            if (container == null || !IsValid) return false;
            return container.HasTagOrParent(this);
        }

        /// <summary>
        /// 获取父标签
        /// 例如: "Status.Buff.Haste" -> "Status.Buff"
        /// </summary>
        public GameplayTag GetParent()
        {
            if (!IsValid) return None;

            int lastDot = Value.LastIndexOf('.');
            if (lastDot <= 0) return None;

            return new GameplayTag(Value[..lastDot]);
        }

        /// <summary>
        /// 获取标签深度
        /// 例如: "Status.Buff.Haste" -> 3
        /// </summary>
        public int GetDepth()
        {
            if (!IsValid) return 0;

            int depth = 1;
            foreach (char c in Value)
            {
                if (c == '.') depth++;
            }
            return depth;
        }

        public bool Equals(GameplayTag other) => Matches(other);

        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value;

        public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
        public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);

        public static implicit operator GameplayTag(string value) => new(value);
        public static implicit operator string(GameplayTag tag) => tag.Value;
    }
}
