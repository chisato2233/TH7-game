using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 行动顺序队列
    /// 基于速度值决定行动顺序，速度高的先行动
    /// </summary>
    public class InitiativeQueue
    {
        /// <summary>
        /// 普通行动队列（按速度排序）
        /// </summary>
        readonly List<BattleUnit> normalQueue = new();

        /// <summary>
        /// 等待队列（选择等待的单位）
        /// </summary>
        readonly List<BattleUnit> waitingQueue = new();

        /// <summary>
        /// 当前索引
        /// </summary>
        int currentIndex;

        /// <summary>
        /// 当前回合的所有单位（用于重置）
        /// </summary>
        readonly List<BattleUnit> allUnits = new();

        /// <summary>
        /// 计算初始行动顺序
        /// </summary>
        public void CalculateOrder(IEnumerable<BattleUnit> attackers, IEnumerable<BattleUnit> defenders)
        {
            normalQueue.Clear();
            waitingQueue.Clear();
            allUnits.Clear();
            currentIndex = 0;

            // 合并所有单位
            if (attackers != null)
            {
                allUnits.AddRange(attackers.Where(u => u != null && u.IsAlive));
            }
            if (defenders != null)
            {
                allUnits.AddRange(defenders.Where(u => u != null && u.IsAlive));
            }

            // 按速度降序排序，速度相同时随机排序
            var sorted = allUnits
                .OrderByDescending(u => u.GetSpeed())
                .ThenBy(_ => Random.value)
                .ToList();

            normalQueue.AddRange(sorted);

            Debug.Log($"[InitiativeQueue] Calculated order for {normalQueue.Count} units");
            foreach (var unit in normalQueue)
            {
                Debug.Log($"  - {unit.DisplayName} (Speed: {unit.GetSpeed()})");
            }
        }

        /// <summary>
        /// 刷新新回合顺序
        /// </summary>
        public void RefreshForNewRound()
        {
            // 将等待队列中的存活单位加回
            foreach (var unit in waitingQueue)
            {
                if (unit != null && unit.IsAlive)
                {
                    normalQueue.Add(unit);
                }
            }
            waitingQueue.Clear();

            // 移除死亡单位
            normalQueue.RemoveAll(u => u == null || !u.IsAlive);

            // 重新排序
            var sorted = normalQueue
                .OrderByDescending(u => u.GetSpeed())
                .ThenBy(_ => Random.value)
                .ToList();

            normalQueue.Clear();
            normalQueue.AddRange(sorted);

            currentIndex = 0;

            Debug.Log($"[InitiativeQueue] Refreshed for new round, {normalQueue.Count} units");
        }

        /// <summary>
        /// 获取下一个行动单位
        /// </summary>
        public BattleUnit GetNextUnit()
        {
            // 先从普通队列获取
            while (currentIndex < normalQueue.Count)
            {
                var unit = normalQueue[currentIndex];
                currentIndex++;

                // 跳过无效单位
                if (unit == null || !unit.IsAlive || !unit.CanAct)
                {
                    continue;
                }

                // 等待中的单位移到等待队列
                if (unit.IsWaiting.Value)
                {
                    unit.IsWaiting.Value = false; // 重置等待状态
                    waitingQueue.Add(unit);
                    continue;
                }

                Debug.Log($"[InitiativeQueue] Next unit: {unit.DisplayName}");
                return unit;
            }

            // 普通队列空了，处理等待队列
            if (waitingQueue.Count > 0)
            {
                // 等待队列按速度升序处理（速度慢的先行动，因为他们等得更久）
                waitingQueue.Sort((a, b) => a.GetSpeed().CompareTo(b.GetSpeed()));

                for (int i = 0; i < waitingQueue.Count; i++)
                {
                    var unit = waitingQueue[i];
                    if (unit != null && unit.IsAlive && unit.CanAct)
                    {
                        waitingQueue.RemoveAt(i);
                        Debug.Log($"[InitiativeQueue] Next unit from waiting queue: {unit.DisplayName}");
                        return unit;
                    }
                }

                // 清理无效单位
                waitingQueue.RemoveAll(u => u == null || !u.IsAlive || !u.CanAct);
            }

            Debug.Log("[InitiativeQueue] No more units to act this round");
            return null; // 本回合结束
        }

        /// <summary>
        /// 查看下一个行动单位（不移除）
        /// </summary>
        public BattleUnit PeekNextUnit()
        {
            // 查看普通队列
            for (int i = currentIndex; i < normalQueue.Count; i++)
            {
                var unit = normalQueue[i];
                if (unit != null && unit.IsAlive && unit.CanAct && !unit.IsWaiting.Value)
                {
                    return unit;
                }
            }

            // 查看等待队列
            if (waitingQueue.Count > 0)
            {
                var sorted = waitingQueue
                    .Where(u => u != null && u.IsAlive && u.CanAct)
                    .OrderBy(u => u.GetSpeed())
                    .FirstOrDefault();
                return sorted;
            }

            return null;
        }

        /// <summary>
        /// 移除单位（死亡时）
        /// </summary>
        public void RemoveUnit(BattleUnit unit)
        {
            normalQueue.Remove(unit);
            waitingQueue.Remove(unit);
            allUnits.Remove(unit);
        }

        /// <summary>
        /// 获取所有存活单位的行动顺序
        /// </summary>
        public IReadOnlyList<BattleUnit> GetRemainingOrder()
        {
            var result = new List<BattleUnit>();

            // 普通队列中未行动的
            for (int i = currentIndex; i < normalQueue.Count; i++)
            {
                var unit = normalQueue[i];
                if (unit != null && unit.IsAlive && unit.CanAct && !unit.IsWaiting.Value)
                {
                    result.Add(unit);
                }
            }

            // 等待队列
            result.AddRange(waitingQueue
                .Where(u => u != null && u.IsAlive && u.CanAct)
                .OrderBy(u => u.GetSpeed()));

            return result;
        }

        /// <summary>
        /// 检查本回合是否还有单位可以行动
        /// </summary>
        public bool HasMoreUnits()
        {
            return PeekNextUnit() != null;
        }

        /// <summary>
        /// 获取当前回合剩余可行动单位数
        /// </summary>
        public int GetRemainingCount()
        {
            int count = 0;

            for (int i = currentIndex; i < normalQueue.Count; i++)
            {
                var unit = normalQueue[i];
                if (unit != null && unit.IsAlive && unit.CanAct)
                {
                    count++;
                }
            }

            count += waitingQueue.Count(u => u != null && u.IsAlive && u.CanAct);

            return count;
        }
    }
}
