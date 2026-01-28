using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 野怪态度
    /// </summary>
    public enum CreatureDisposition
    {
        /// <summary>
        /// 凶恶 - 总是战斗
        /// </summary>
        Aggressive,

        /// <summary>
        /// 好战 - 通常战斗，偶尔逃跑
        /// </summary>
        Hostile,

        /// <summary>
        /// 中立 - 可能战斗、逃跑或加入
        /// </summary>
        Neutral,

        /// <summary>
        /// 友好 - 可能逃跑或加入
        /// </summary>
        Friendly,

        /// <summary>
        /// 顺从 - 通常加入
        /// </summary>
        Compliant
    }

    /// <summary>
    /// 野怪交互选项
    /// </summary>
    public enum CreatureInteractionOption
    {
        Fight,      // 战斗
        Flee,       // 逃跑（野怪消失）
        Join,       // 加入玩家军队
        Bribe       // 贿赂（花钱让其离开）
    }

    /// <summary>
    /// 野怪堆 - 地图上的中立生物
    /// 英雄接触时触发战斗或其他交互
    /// </summary>
    public class CreatureStack : MapObject
    {
        [Header("Creature Config")]
        [SerializeField] UnitConfig unitConfig;
        [SerializeField] int count = 10;
        [SerializeField] CreatureDisposition disposition = CreatureDisposition.Aggressive;

        [Header("Growth (Optional)")]
        [SerializeField] bool canGrow = false;
        [SerializeField] int growthPerWeek = 0;

        [Header("Guarded Treasure (Optional)")]
        [SerializeField] ResourceBundle guardedResources;
        [SerializeField] string guardedArtifactId;

        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;

        bool isDefeated;

        /// <summary>
        /// 单位配置
        /// </summary>
        public UnitConfig UnitConfig => unitConfig;

        /// <summary>
        /// 数量
        /// </summary>
        public int Count => count;

        /// <summary>
        /// 态度
        /// </summary>
        public CreatureDisposition Disposition => disposition;

        /// <summary>
        /// 是否已被击败
        /// </summary>
        public override bool IsCollected => isDefeated;

        /// <summary>
        /// 击败后移除
        /// </summary>
        public override bool RemoveAfterInteract => isDefeated;

        protected override void Start()
        {
            base.Start();
            UpdateVisual();
        }

        void UpdateVisual()
        {
            if (spriteRenderer != null && unitConfig != null)
            {
                spriteRenderer.sprite = unitConfig.Icon;
            }
        }

        /// <summary>
        /// 获取军队配置（用于战斗）
        /// </summary>
        public UnitStack[] GetArmy()
        {
            if (unitConfig == null) return null;

            return new UnitStack[]
            {
                new UnitStack
                {
                    UnitId = unitConfig.UnitId,
                    Count = count
                }
            };
        }

        /// <summary>
        /// 获取可用的交互选项
        /// </summary>
        public CreatureInteractionOption[] GetAvailableOptions(Hero hero)
        {
            // 根据态度、英雄外交技能、军队实力对比等计算可用选项
            // 简化实现：凶恶总是战斗，其他态度可能有更多选项

            switch (disposition)
            {
                case CreatureDisposition.Aggressive:
                    return new[] { CreatureInteractionOption.Fight };

                case CreatureDisposition.Hostile:
                    // 如果英雄军队远强于野怪，可能会逃跑
                    if (ShouldFlee(hero))
                        return new[] { CreatureInteractionOption.Flee };
                    return new[] { CreatureInteractionOption.Fight };

                case CreatureDisposition.Neutral:
                    var options = new System.Collections.Generic.List<CreatureInteractionOption>
                    {
                        CreatureInteractionOption.Fight
                    };
                    if (ShouldFlee(hero))
                        options.Add(CreatureInteractionOption.Flee);
                    if (CanJoin(hero))
                        options.Add(CreatureInteractionOption.Join);
                    return options.ToArray();

                case CreatureDisposition.Friendly:
                case CreatureDisposition.Compliant:
                    if (CanJoin(hero))
                        return new[] { CreatureInteractionOption.Join };
                    if (ShouldFlee(hero))
                        return new[] { CreatureInteractionOption.Flee };
                    return new[] { CreatureInteractionOption.Fight };

                default:
                    return new[] { CreatureInteractionOption.Fight };
            }
        }

        /// <summary>
        /// 是否应该逃跑（基于实力对比）
        /// </summary>
        bool ShouldFlee(Hero hero)
        {
            // 简化：如果英雄军队总数是野怪的3倍以上，野怪可能逃跑
            int heroArmyStrength = CalculateArmyStrength(hero.Army);
            int creatureStrength = CalculateStackStrength();

            return heroArmyStrength > creatureStrength * 3;
        }

        /// <summary>
        /// 是否可以加入
        /// </summary>
        bool CanJoin(Hero hero)
        {
            // 简化：友好/顺从态度 + 英雄有足够魅力/外交技能
            // TODO: 检查英雄是否有相同阵营的单位槽位
            return disposition >= CreatureDisposition.Friendly;
        }

        int CalculateArmyStrength(UnitStack[] army)
        {
            if (army == null) return 0;

            int total = 0;
            foreach (var stack in army)
            {
                if (stack != null)
                    total += stack.Count; // 简化：仅计算数量
            }
            return total;
        }

        int CalculateStackStrength()
        {
            return count; // 简化
        }

        public override InteractionResult Interact(Hero hero, SessionContext session)
        {
            if (isDefeated)
                return InteractionResult.Fail("This creature stack has already been defeated.");

            var options = GetAvailableOptions(hero);

            // 如果只有战斗选项，直接返回触发战斗
            if (options.Length == 1 && options[0] == CreatureInteractionOption.Fight)
            {
                Debug.Log($"[CreatureStack] {hero.HeroName} encounters {count} {unitConfig?.DisplayName} - Battle!");
                // 返回一个特殊结果，让调用方知道需要触发战斗
                // InteractionResult 目前不支持战斗，我们返回 Fail 并通过 ActionResult 处理
                return InteractionResult.Fail("TRIGGER_BATTLE");
            }

            // 如果野怪逃跑
            if (options.Length == 1 && options[0] == CreatureInteractionOption.Flee)
            {
                Debug.Log($"[CreatureStack] {count} {unitConfig?.DisplayName} fled from {hero.HeroName}!");
                isDefeated = true;
                return InteractionResult.Ok("The creatures fled in fear!", remove: true);
            }

            // 如果野怪加入
            if (options.Length == 1 && options[0] == CreatureInteractionOption.Join)
            {
                Debug.Log($"[CreatureStack] {count} {unitConfig?.DisplayName} joined {hero.HeroName}!");
                // TODO: 将单位添加到英雄军队
                isDefeated = true;
                return InteractionResult.Ok($"{count} {unitConfig?.DisplayName} joined your army!", remove: true);
            }

            // 多个选项时，应该显示对话框让玩家选择
            // 目前简化为直接战斗
            return InteractionResult.Fail("TRIGGER_BATTLE");
        }

        /// <summary>
        /// 标记为已击败（战斗胜利后调用）
        /// </summary>
        public void MarkDefeated()
        {
            isDefeated = true;

            // 给予守护的资源
            if (guardedResources != null && !guardedResources.IsEmpty)
            {
                Debug.Log($"[CreatureStack] Gained guarded resources: {guardedResources}");
                // TODO: 添加到玩家资源
            }

            // 给予守护的宝物
            if (!string.IsNullOrEmpty(guardedArtifactId))
            {
                Debug.Log($"[CreatureStack] Gained artifact: {guardedArtifactId}");
                // TODO: 添加到英雄背包
            }
        }

        /// <summary>
        /// 每周增长
        /// </summary>
        public void OnWeekPassed()
        {
            if (canGrow && growthPerWeek > 0 && !isDefeated)
            {
                count += growthPerWeek;
                Debug.Log($"[CreatureStack] {unitConfig?.DisplayName} grew to {count}");
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UpdateVisual();
        }

        void OnDrawGizmos()
        {
            // 在编辑器中显示野怪信息
            if (unitConfig != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.5f,
                    $"{unitConfig.DisplayName} x{count}\n[{disposition}]"
                );
            }
        }
#endif
    }
}
