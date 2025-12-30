using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 资源包 - 存储多种资源的容器
    /// </summary>
    [Serializable]
    public class ResourceBundle
    {
        [SerializeField] private int[] amounts = new int[RESOURCE_COUNT];

        const int RESOURCE_COUNT = 7;

        public ResourceBundle() { }

        public ResourceBundle(int gold = 0, int wood = 0, int ore = 0,
            int crystal = 0, int gem = 0, int sulfur = 0, int mercury = 0)
        {
            amounts[(int)ResourceType.Gold] = gold;
            amounts[(int)ResourceType.Wood] = wood;
            amounts[(int)ResourceType.Ore] = ore;
            amounts[(int)ResourceType.Crystal] = crystal;
            amounts[(int)ResourceType.Gem] = gem;
            amounts[(int)ResourceType.Sulfur] = sulfur;
            amounts[(int)ResourceType.Mercury] = mercury;
        }

        public int this[ResourceType type]
        {
            get => amounts[(int)type];
            set => amounts[(int)type] = Mathf.Max(0, value);
        }

        public int Gold { get => this[ResourceType.Gold]; set => this[ResourceType.Gold] = value; }
        public int Wood { get => this[ResourceType.Wood]; set => this[ResourceType.Wood] = value; }
        public int Ore { get => this[ResourceType.Ore]; set => this[ResourceType.Ore] = value; }
        public int Crystal { get => this[ResourceType.Crystal]; set => this[ResourceType.Crystal] = value; }
        public int Gem { get => this[ResourceType.Gem]; set => this[ResourceType.Gem] = value; }
        public int Sulfur { get => this[ResourceType.Sulfur]; set => this[ResourceType.Sulfur] = value; }
        public int Mercury { get => this[ResourceType.Mercury]; set => this[ResourceType.Mercury] = value; }

        /// <summary>
        /// 检查是否有足够的资源
        /// </summary>
        public bool HasEnough(ResourceBundle cost)
        {
            for (int i = 0; i < RESOURCE_COUNT; i++)
            {
                if (amounts[i] < cost.amounts[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 扣除资源（不检查，调用前请先 HasEnough）
        /// </summary>
        public void Subtract(ResourceBundle cost)
        {
            for (int i = 0; i < RESOURCE_COUNT; i++)
                amounts[i] = Mathf.Max(0, amounts[i] - cost.amounts[i]);
        }

        /// <summary>
        /// 增加资源
        /// </summary>
        public void Add(ResourceBundle income)
        {
            for (int i = 0; i < RESOURCE_COUNT; i++)
                amounts[i] += income.amounts[i];
        }

        /// <summary>
        /// 尝试消费资源
        /// </summary>
        public bool TrySpend(ResourceBundle cost)
        {
            if (!HasEnough(cost)) return false;
            Subtract(cost);
            return true;
        }

        /// <summary>
        /// 复制
        /// </summary>
        public ResourceBundle Clone()
        {
            var clone = new ResourceBundle();
            Array.Copy(amounts, clone.amounts, RESOURCE_COUNT);
            return clone;
        }

        /// <summary>
        /// 清零
        /// </summary>
        public void Clear()
        {
            Array.Clear(amounts, 0, RESOURCE_COUNT);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < RESOURCE_COUNT; i++)
                    if (amounts[i] > 0) return false;
                return true;
            }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Gold > 0) parts.Add($"Gold:{Gold}");
            if (Wood > 0) parts.Add($"Wood:{Wood}");
            if (Ore > 0) parts.Add($"Ore:{Ore}");
            if (Crystal > 0) parts.Add($"Crystal:{Crystal}");
            if (Gem > 0) parts.Add($"Gem:{Gem}");
            if (Sulfur > 0) parts.Add($"Sulfur:{Sulfur}");
            if (Mercury > 0) parts.Add($"Mercury:{Mercury}");
            return parts.Count > 0 ? string.Join(", ", parts) : "Empty";
        }

        // 运算符重载
        public static ResourceBundle operator +(ResourceBundle a, ResourceBundle b)
        {
            var result = a.Clone();
            result.Add(b);
            return result;
        }

        public static ResourceBundle operator -(ResourceBundle a, ResourceBundle b)
        {
            var result = a.Clone();
            result.Subtract(b);
            return result;
        }

        public static ResourceBundle operator *(ResourceBundle a, int multiplier)
        {
            var result = new ResourceBundle();
            for (int i = 0; i < RESOURCE_COUNT; i++)
                result.amounts[i] = a.amounts[i] * multiplier;
            return result;
        }
    }
}
