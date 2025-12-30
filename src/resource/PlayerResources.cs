using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 玩家资源数据（响应式，支持 UI 绑定和存档）
    /// </summary>
    [Serializable]
    public class PlayerResources
    {
        // 各资源响应式值
        public Reactive<int> Gold = new(0);
        public Reactive<int> Wood = new(0);
        public Reactive<int> Ore = new(0);
        public Reactive<int> Crystal = new(0);
        public Reactive<int> Gem = new(0);
        public Reactive<int> Sulfur = new(0);
        public Reactive<int> Mercury = new(0);

        /// <summary>
        /// 获取指定资源
        /// </summary>
        public Reactive<int> Get(ResourceType type) => type switch
        {
            ResourceType.Gold => Gold,
            ResourceType.Wood => Wood,
            ResourceType.Ore => Ore,
            ResourceType.Crystal => Crystal,
            ResourceType.Gem => Gem,
            ResourceType.Sulfur => Sulfur,
            ResourceType.Mercury => Mercury,
            _ => Gold
        };

        /// <summary>
        /// 获取资源值
        /// </summary>
        public int GetValue(ResourceType type) => Get(type).Value;

        /// <summary>
        /// 设置资源值
        /// </summary>
        public void SetValue(ResourceType type, int value) => Get(type).Value = Mathf.Max(0, value);

        /// <summary>
        /// 检查是否有足够资源
        /// </summary>
        public bool HasEnough(ResourceBundle cost)
        {
            return Gold.Value >= cost.Gold &&
                   Wood.Value >= cost.Wood &&
                   Ore.Value >= cost.Ore &&
                   Crystal.Value >= cost.Crystal &&
                   Gem.Value >= cost.Gem &&
                   Sulfur.Value >= cost.Sulfur &&
                   Mercury.Value >= cost.Mercury;
        }

        /// <summary>
        /// 增加资源
        /// </summary>
        public void Add(ResourceBundle income)
        {
            Gold.Value += income.Gold;
            Wood.Value += income.Wood;
            Ore.Value += income.Ore;
            Crystal.Value += income.Crystal;
            Gem.Value += income.Gem;
            Sulfur.Value += income.Sulfur;
            Mercury.Value += income.Mercury;
        }

        /// <summary>
        /// 增加指定类型的资源
        /// </summary>
        public void Add(ResourceType type, int amount)
        {
            Get(type).Value += amount;
        }

        /// <summary>
        /// 扣除资源（不检查）
        /// </summary>
        public void Subtract(ResourceBundle cost)
        {
            Gold.Value = Mathf.Max(0, Gold.Value - cost.Gold);
            Wood.Value = Mathf.Max(0, Wood.Value - cost.Wood);
            Ore.Value = Mathf.Max(0, Ore.Value - cost.Ore);
            Crystal.Value = Mathf.Max(0, Crystal.Value - cost.Crystal);
            Gem.Value = Mathf.Max(0, Gem.Value - cost.Gem);
            Sulfur.Value = Mathf.Max(0, Sulfur.Value - cost.Sulfur);
            Mercury.Value = Mathf.Max(0, Mercury.Value - cost.Mercury);
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
        /// 从 ResourceBundle 导入
        /// </summary>
        public void Import(ResourceBundle bundle)
        {
            Gold.Value = bundle.Gold;
            Wood.Value = bundle.Wood;
            Ore.Value = bundle.Ore;
            Crystal.Value = bundle.Crystal;
            Gem.Value = bundle.Gem;
            Sulfur.Value = bundle.Sulfur;
            Mercury.Value = bundle.Mercury;
        }

        /// <summary>
        /// 导出为 ResourceBundle
        /// </summary>
        public ResourceBundle Export() => new ResourceBundle(
            Gold.Value, Wood.Value, Ore.Value,
            Crystal.Value, Gem.Value, Sulfur.Value, Mercury.Value);

        /// <summary>
        /// 设置初始资源
        /// </summary>
        public void SetStartingResources(int gold = 2000, int wood = 10, int ore = 10)
        {
            Gold.Value = gold;
            Wood.Value = wood;
            Ore.Value = ore;
            Crystal.Value = 0;
            Gem.Value = 0;
            Sulfur.Value = 0;
            Mercury.Value = 0;
        }
    }
}
