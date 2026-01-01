using System;
using System.Collections;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// Gameplay 标签容器 - 管理一组标签并提供查询方法
    /// </summary>
    [Serializable]
    public class GameplayTagContainer : IEnumerable<GameplayTag>
    {
        readonly HashSet<GameplayTag> tags = new();

        /// <summary>
        /// 标签数量
        /// </summary>
        public int Count => tags.Count;

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => tags.Count == 0;

        public GameplayTagContainer() { }

        public GameplayTagContainer(IEnumerable<GameplayTag> initialTags)
        {
            if (initialTags == null) return;
            foreach (var tag in initialTags)
            {
                AddTag(tag);
            }
        }

        public GameplayTagContainer(params GameplayTag[] initialTags)
            : this((IEnumerable<GameplayTag>)initialTags) { }

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(GameplayTag tag)
        {
            if (tag.IsValid)
                tags.Add(tag);
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public bool RemoveTag(GameplayTag tag)
        {
            return tags.Remove(tag);
        }

        /// <summary>
        /// 清空所有标签
        /// </summary>
        public void Clear()
        {
            tags.Clear();
        }

        /// <summary>
        /// 检查是否有精确匹配的标签
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            return tag.IsValid && tags.Contains(tag);
        }

        /// <summary>
        /// 检查是否有标签或其父标签
        /// 例如: 容器有 "Status.Buff", 查询 "Status.Buff.Haste" 返回 true
        /// </summary>
        public bool HasTagOrParent(GameplayTag tag)
        {
            if (!tag.IsValid) return false;

            // 精确匹配
            if (tags.Contains(tag)) return true;

            // 检查是否有任何标签是查询标签的父标签
            foreach (var existingTag in tags)
            {
                if (tag.HasParent(existingTag))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否有任一标签（精确匹配）
        /// </summary>
        public bool HasAny(GameplayTagContainer required)
        {
            if (required == null || required.IsEmpty) return false;

            foreach (var tag in required.tags)
            {
                if (tags.Contains(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否有任一标签（包含层级匹配）
        /// </summary>
        public bool HasAnyWithHierarchy(GameplayTagContainer required)
        {
            if (required == null || required.IsEmpty) return false;

            foreach (var tag in required.tags)
            {
                if (HasTagOrParent(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否有所有标签（精确匹配）
        /// </summary>
        public bool HasAll(GameplayTagContainer required)
        {
            if (required == null || required.IsEmpty) return true;

            foreach (var tag in required.tags)
            {
                if (!tags.Contains(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否有所有标签（包含层级匹配）
        /// </summary>
        public bool HasAllWithHierarchy(GameplayTagContainer required)
        {
            if (required == null || required.IsEmpty) return true;

            foreach (var tag in required.tags)
            {
                if (!HasTagOrParent(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否没有任何指定标签
        /// </summary>
        public bool HasNone(GameplayTagContainer blocked)
        {
            if (blocked == null || blocked.IsEmpty) return true;

            foreach (var tag in blocked.tags)
            {
                if (tags.Contains(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否没有任何指定标签（包含层级匹配）
        /// </summary>
        public bool HasNoneWithHierarchy(GameplayTagContainer blocked)
        {
            if (blocked == null || blocked.IsEmpty) return true;

            foreach (var tag in blocked.tags)
            {
                if (HasTagOrParent(tag))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取匹配指定父标签的所有标签
        /// </summary>
        public List<GameplayTag> GetTagsMatchingParent(GameplayTag parent)
        {
            var result = new List<GameplayTag>();
            foreach (var tag in tags)
            {
                if (tag.HasParent(parent) || tag.Matches(parent))
                    result.Add(tag);
            }
            return result;
        }

        /// <summary>
        /// 合并另一个容器的标签
        /// </summary>
        public void Merge(GameplayTagContainer other)
        {
            if (other == null) return;
            foreach (var tag in other.tags)
            {
                tags.Add(tag);
            }
        }

        /// <summary>
        /// 从字符串数组创建
        /// </summary>
        public static GameplayTagContainer FromStrings(params string[] tagStrings)
        {
            var container = new GameplayTagContainer();
            foreach (var str in tagStrings)
            {
                container.AddTag(new GameplayTag(str));
            }
            return container;
        }

        /// <summary>
        /// 转换为数组
        /// </summary>
        public GameplayTag[] ToArray()
        {
            var result = new GameplayTag[tags.Count];
            tags.CopyTo(result);
            return result;
        }

        public IEnumerator<GameplayTag> GetEnumerator() => tags.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return $"[{string.Join(", ", tags)}]";
        }
    }
}
