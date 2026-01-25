using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 战斗 AI 行动提供者
    /// 为 AI 控制的单位生成行动
    /// </summary>
    public class BattleAIProvider : IBattleActionProvider
    {
        readonly BattleContext context;
        bool isEnabled = true;

        public bool IsEnabled => isEnabled;

        /// <summary>
        /// AI 思考延迟（秒）
        /// </summary>
        public float ThinkDelay { get; set; } = 0.5f;

        public BattleAIProvider(BattleContext battleContext)
        {
            context = battleContext;
        }

        public void RequestAction(BattleUnit unit, BattleContext ctx, Action<BattleAction> callback)
        {
            if (!isEnabled)
            {
                callback?.Invoke(null);
                return;
            }

            Debug.Log($"[BattleAIProvider] Thinking for {unit.DisplayName}...");

            // 简单延迟模拟思考时间
            // 实际项目中可以用协程实现
            var action = DecideAction(unit);

            Debug.Log($"[BattleAIProvider] Decided: {action}");
            callback?.Invoke(action);
        }

        public void CancelRequest()
        {
            // AI 不需要取消
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        /// <summary>
        /// 决定行动
        /// </summary>
        BattleAction DecideAction(BattleUnit unit)
        {
            var enemies = context.GetEnemies(unit.Side).Where(e => e.IsAlive).ToList();
            if (enemies.Count == 0)
            {
                // 没有敌人，防御
                return new BattleDefendAction(unit);
            }

            // 远程单位策略
            if (unit.IsRanged && unit.Shots.Value > 0)
            {
                return DecideRangedAction(unit, enemies);
            }

            // 近战单位策略
            return DecideMeleeAction(unit, enemies);
        }

        /// <summary>
        /// 远程单位决策
        /// </summary>
        BattleAction DecideRangedAction(BattleUnit unit, List<BattleUnit> enemies)
        {
            // 检查是否有近身敌人
            var adjacentEnemies = enemies.Where(e => unit.IsAdjacent(e)).ToList();

            if (adjacentEnemies.Count > 0)
            {
                // 有近身敌人，检查是否应该近战还是远程
                // 如果有多个相邻敌人，可能应该移走
                var reachableTiles = context.Map.GetReachableTiles(unit);
                var safeTiles = reachableTiles.Where(t =>
                {
                    foreach (var enemy in enemies)
                    {
                        if (Mathf.Abs(t.x - enemy.CellPosition.Value.x) <= 1 &&
                            Mathf.Abs(t.y - enemy.CellPosition.Value.y) <= 1)
                        {
                            return false;
                        }
                    }
                    return true;
                }).ToList();

                if (safeTiles.Count > 0)
                {
                    // 移动到安全位置
                    var safeTile = safeTiles[UnityEngine.Random.Range(0, safeTiles.Count)];
                    var path = context.Map.FindPath(unit.CellPosition.Value, safeTile, unit);
                    if (path != null && path.Count > 0)
                    {
                        return new BattleMoveAction(unit, path);
                    }
                }

                // 无法躲避，进行远程攻击（有惩罚）
            }

            // 选择目标：优先攻击血量最低的
            var target = enemies
                .OrderBy(e => e.Count.Value * e.Config.Health + e.CurrentHealth.Value)
                .First();

            return new BattleRangedAttackAction(unit, target);
        }

        /// <summary>
        /// 近战单位决策
        /// </summary>
        BattleAction DecideMeleeAction(BattleUnit unit, List<BattleUnit> enemies)
        {
            // 检查相邻敌人
            var adjacentEnemies = enemies.Where(e => unit.IsAdjacent(e)).ToList();

            if (adjacentEnemies.Count > 0)
            {
                // 选择最弱的相邻敌人攻击
                var target = adjacentEnemies
                    .OrderBy(e => e.Count.Value * e.Config.Health + e.CurrentHealth.Value)
                    .First();

                return new BattleAttackAction(unit, target);
            }

            // 没有相邻敌人，尝试移动攻击
            var moveAndAttackTargets = context.Map.GetMoveAndAttackTargets(unit, context);
            if (moveAndAttackTargets.Count > 0)
            {
                // 选择最弱的可攻击目标
                var (target, attackPos) = moveAndAttackTargets
                    .OrderBy(t => t.target.Count.Value * t.target.Config.Health + t.target.CurrentHealth.Value)
                    .First();

                List<Vector2Int> movePath = null;
                if (attackPos != unit.CellPosition.Value)
                {
                    movePath = context.Map.FindPath(unit.CellPosition.Value, attackPos, unit);
                }

                return new BattleAttackAction(unit, target, movePath);
            }

            // 无法攻击，移动接近最近的敌人
            var reachableTiles = context.Map.GetReachableTiles(unit);
            if (reachableTiles.Count > 0)
            {
                // 找到距离敌人最近的可达格子
                Vector2Int? bestTile = null;
                float bestDistance = float.MaxValue;

                foreach (var tile in reachableTiles)
                {
                    foreach (var enemy in enemies)
                    {
                        float dist = Vector2Int.Distance(tile, enemy.CellPosition.Value);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestTile = tile;
                        }
                    }
                }

                if (bestTile.HasValue)
                {
                    var path = context.Map.FindPath(unit.CellPosition.Value, bestTile.Value, unit);
                    if (path != null && path.Count > 0)
                    {
                        return new BattleMoveAction(unit, path);
                    }
                }
            }

            // 无法移动，防御
            return new BattleDefendAction(unit);
        }
    }
}
