using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 玩家战斗行动提供者
    /// 处理玩家输入，生成战斗行动
    /// </summary>
    public class BattleActionProvider : IBattleActionProvider
    {
        readonly BattleContext context;
        readonly Camera battleCamera;
        readonly EventSystem eventSystem;

        Action<BattleAction> actionCallback;
        BattleUnit currentUnit;
        List<Vector2Int> reachableTiles;
        List<BattleUnit> attackableTargets;

        bool isEnabled;
        bool isWaitingForInput;

        public bool IsEnabled => isEnabled;

        public BattleActionProvider(BattleContext battleContext, Camera camera)
        {
            context = battleContext;
            battleCamera = camera;
            eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
        }

        public void RequestAction(BattleUnit unit, BattleContext ctx, Action<BattleAction> callback)
        {
            currentUnit = unit;
            actionCallback = callback;
            isWaitingForInput = true;

            // 计算可移动范围
            reachableTiles = context.Map.GetReachableTiles(unit);

            // 计算可攻击目标
            attackableTargets = context.Map.GetAttackableTargets(unit, context);

            // 发布事件更新 UI
            eventSystem?.Publish(new MovableRangeUpdatedEvent
            {
                Unit = unit,
                ReachableTiles = reachableTiles.ToArray()
            });

            eventSystem?.Publish(new AttackableTargetsUpdatedEvent
            {
                Unit = unit,
                Targets = attackableTargets.ToArray()
            });

            eventSystem?.Publish(new BattleUnitSelectedEvent { Unit = unit });

            Debug.Log($"[BattleActionProvider] Requesting action for {unit.DisplayName}, " +
                     $"reachable: {reachableTiles.Count}, attackable: {attackableTargets.Count}");
        }

        public void CancelRequest()
        {
            isWaitingForInput = false;
            currentUnit = null;
            actionCallback = null;
            reachableTiles = null;
            attackableTargets = null;

            eventSystem?.Publish(new BattleUnitDeselectedEvent());
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                CancelRequest();
            }
        }

        /// <summary>
        /// 处理格子点击
        /// </summary>
        public void OnTileClicked(Vector2Int position, bool isRightClick)
        {
            if (!isEnabled || !isWaitingForInput || currentUnit == null)
                return;

            if (isRightClick)
            {
                // 右键取消
                CancelRequest();
                return;
            }

            // 检查是否点击了敌方单位
            var targetUnit = context.GetUnitAt(position);
            if (targetUnit != null && targetUnit.Side != currentUnit.Side && targetUnit.IsAlive)
            {
                // 攻击
                OnUnitClicked(targetUnit, false);
                return;
            }

            // 检查是否是可移动位置
            if (reachableTiles != null && reachableTiles.Contains(position))
            {
                // 移动
                var path = context.Map.FindPath(currentUnit.CellPosition.Value, position, currentUnit);
                if (path != null && path.Count > 0)
                {
                    SubmitAction(new BattleMoveAction(currentUnit, path));
                }
            }
        }

        /// <summary>
        /// 处理单位点击
        /// </summary>
        public void OnUnitClicked(BattleUnit unit, bool isRightClick)
        {
            if (!isEnabled || !isWaitingForInput || currentUnit == null)
                return;

            if (isRightClick)
            {
                // 右键取消
                CancelRequest();
                return;
            }

            if (unit == currentUnit)
            {
                // 点击自己，不做任何事
                return;
            }

            if (unit.Side == currentUnit.Side)
            {
                // 点击友方，不做任何事
                return;
            }

            // 点击敌方，尝试攻击
            if (attackableTargets != null && attackableTargets.Contains(unit))
            {
                // 检查是近战还是远程
                if (currentUnit.IsRanged && currentUnit.Shots.Value > 0)
                {
                    // 远程攻击
                    SubmitAction(new BattleRangedAttackAction(currentUnit, unit));
                }
                else if (currentUnit.IsAdjacent(unit))
                {
                    // 近战攻击（已相邻）
                    SubmitAction(new BattleAttackAction(currentUnit, unit));
                }
                else
                {
                    // 需要移动后攻击
                    var moveAndAttackTargets = context.Map.GetMoveAndAttackTargets(currentUnit, context);
                    foreach (var (target, attackPos) in moveAndAttackTargets)
                    {
                        if (target == unit)
                        {
                            List<Vector2Int> movePath = null;
                            if (attackPos != currentUnit.CellPosition.Value)
                            {
                                movePath = context.Map.FindPath(currentUnit.CellPosition.Value, attackPos, currentUnit);
                            }
                            SubmitAction(new BattleAttackAction(currentUnit, unit, movePath));
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 等待行动
        /// </summary>
        public void OnWaitRequested()
        {
            if (!isEnabled || !isWaitingForInput || currentUnit == null)
                return;

            SubmitAction(new BattleWaitAction(currentUnit));
        }

        /// <summary>
        /// 防御行动
        /// </summary>
        public void OnDefendRequested()
        {
            if (!isEnabled || !isWaitingForInput || currentUnit == null)
                return;

            SubmitAction(new BattleDefendAction(currentUnit));
        }

        /// <summary>
        /// 提交行动
        /// </summary>
        void SubmitAction(BattleAction action)
        {
            if (actionCallback == null)
                return;

            var callback = actionCallback;

            isWaitingForInput = false;
            actionCallback = null;
            reachableTiles = null;
            attackableTargets = null;

            eventSystem?.Publish(new BattleUnitDeselectedEvent());

            Debug.Log($"[BattleActionProvider] Submitting action: {action}");
            callback(action);
        }

        /// <summary>
        /// 从屏幕坐标获取格子坐标
        /// </summary>
        public Vector2Int? ScreenToCell(Vector2 screenPosition)
        {
            if (battleCamera == null)
                return null;

            Vector3 worldPos = battleCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            CancelRequest();
        }
    }
}
