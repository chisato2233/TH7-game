using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗行动执行器
    /// 负责执行各种战斗行动，包括移动、攻击、技能等
    /// </summary>
    public class BattleActionExecutor
    {
        readonly BattleContext context;
        readonly MonoBehaviour coroutineRunner;
        readonly EventSystem eventSystem;

        /// <summary>
        /// 行动开始事件
        /// </summary>
        public event Action<BattleAction> OnActionStarted;

        /// <summary>
        /// 行动完成事件
        /// </summary>
        public event Action<BattleAction, BattleActionResult> OnActionCompleted;

        /// <summary>
        /// 移动速度（格/秒）
        /// </summary>
        public float MoveSpeed { get; set; } = 8f;

        /// <summary>
        /// 攻击动画持续时间
        /// </summary>
        public float AttackDuration { get; set; } = 0.5f;

        /// <summary>
        /// 是否正在执行
        /// </summary>
        bool isExecuting;

        public BattleActionExecutor(BattleContext battleContext, MonoBehaviour runner)
        {
            context = battleContext;
            coroutineRunner = runner;
            eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
        }

        /// <summary>
        /// 执行行动
        /// </summary>
        public void Execute(BattleAction action, Action<BattleActionResult> onComplete)
        {
            if (isExecuting)
            {
                Debug.LogWarning("[BattleActionExecutor] Already executing an action");
                onComplete?.Invoke(BattleActionResult.Failed("Already executing"));
                return;
            }

            if (action == null)
            {
                onComplete?.Invoke(BattleActionResult.Failed("Action is null"));
                return;
            }

            if (!action.CanExecute(context))
            {
                Debug.LogWarning($"[BattleActionExecutor] Cannot execute action: {action}");
                onComplete?.Invoke(BattleActionResult.Failed("Cannot execute"));
                return;
            }

            isExecuting = true;
            OnActionStarted?.Invoke(action);
            eventSystem?.Publish(new BattleActionStartedEvent { Action = action });

            Debug.Log($"[BattleActionExecutor] Executing: {action}");

            coroutineRunner.StartCoroutine(ExecuteCoroutine(action, result =>
            {
                isExecuting = false;
                OnActionCompleted?.Invoke(action, result);
                eventSystem?.Publish(new BattleActionCompletedEvent { Action = action, Result = result });
                onComplete?.Invoke(result);
            }));
        }

        IEnumerator ExecuteCoroutine(BattleAction action, Action<BattleActionResult> onComplete)
        {
            switch (action)
            {
                case BattleMoveAction moveAction:
                    yield return ExecuteMove(moveAction, onComplete);
                    break;

                case BattleAttackAction attackAction:
                    yield return ExecuteAttack(attackAction, onComplete);
                    break;

                case BattleRangedAttackAction rangedAction:
                    yield return ExecuteRangedAttack(rangedAction, onComplete);
                    break;

                case BattleAbilityAction abilityAction:
                    yield return ExecuteAbility(abilityAction, onComplete);
                    break;

                case BattleWaitAction waitAction:
                    onComplete?.Invoke(ExecuteWait(waitAction));
                    break;

                case BattleDefendAction defendAction:
                    onComplete?.Invoke(ExecuteDefend(defendAction));
                    break;

                default:
                    onComplete?.Invoke(BattleActionResult.Failed("Unknown action type"));
                    break;
            }
        }

        #region Move Action

        IEnumerator ExecuteMove(BattleMoveAction action, Action<BattleActionResult> onComplete)
        {
            var unit = action.Unit;
            unit.StartMoving();

            foreach (var cell in action.Path)
            {
                // 更新朝向
                var direction = cell - unit.CellPosition.Value;
                unit.SetFacing(direction);

                // 移动到目标格子
                yield return MoveToCell(unit, cell);

                // 更新地图位置
                context.Map.MoveUnit(unit, cell);
            }

            unit.StopMoving();
            onComplete?.Invoke(BattleActionResult.Success());
        }

        IEnumerator MoveToCell(BattleUnit unit, Vector2Int targetCell)
        {
            Vector3 startPos = unit.transform.position;
            Vector3 targetPos = CellToWorld(targetCell);
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / MoveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                unit.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            unit.transform.position = targetPos;
        }

        #endregion

        #region Attack Action

        IEnumerator ExecuteAttack(BattleAttackAction action, Action<BattleActionResult> onComplete)
        {
            var unit = action.Unit;
            var target = action.Target;

            // 先移动到攻击位置
            if (action.MovePath != null && action.MovePath.Count > 0)
            {
                unit.StartMoving();
                foreach (var cell in action.MovePath)
                {
                    var direction = cell - unit.CellPosition.Value;
                    unit.SetFacing(direction);
                    yield return MoveToCell(unit, cell);
                    context.Map.MoveUnit(unit, cell);
                }
                unit.StopMoving();
            }

            // 朝向目标
            unit.SetFacing(target.CellPosition.Value - unit.CellPosition.Value);

            // 执行攻击
            unit.StartActing();
            unit.PlayAttackAnimation();

            // 计算伤害
            int damage = unit.RollDamage();
            var damageResult = target.TakeDamage(damage, unit);

            // 发布伤害事件
            eventSystem?.Publish(new DamageDealtEvent
            {
                Source = unit,
                Target = target,
                Damage = damageResult.FinalDamage,
                UnitsKilled = damageResult.UnitsKilled,
                IsCritical = damageResult.IsCritical,
                IsRanged = false,
                IsRetaliation = false
            });

            // 目标受伤动画
            target.PlayHitAnimation();

            yield return new WaitForSeconds(AttackDuration);

            // 反击检查
            DamageResult? retaliationDamage = null;
            if (target.CanRetaliate(unit))
            {
                target.SetFacing(unit.CellPosition.Value - target.CellPosition.Value);
                target.PlayAttackAnimation();

                int retaliateDmg = target.RollDamage();
                var retaliateResult = unit.TakeDamage(retaliateDmg, target);
                target.MarkRetaliated();

                retaliationDamage = retaliateResult;

                eventSystem?.Publish(new DamageDealtEvent
                {
                    Source = target,
                    Target = unit,
                    Damage = retaliateResult.FinalDamage,
                    UnitsKilled = retaliateResult.UnitsKilled,
                    IsRetaliation = true
                });

                unit.PlayHitAnimation();

                yield return new WaitForSeconds(AttackDuration * 0.5f);
            }

            unit.StopMoving();

            var result = BattleActionResult.Success(damageResult);
            result.RetaliationDamage = retaliationDamage;
            onComplete?.Invoke(result);
        }

        #endregion

        #region Ranged Attack Action

        IEnumerator ExecuteRangedAttack(BattleRangedAttackAction action, Action<BattleActionResult> onComplete)
        {
            var unit = action.Unit;
            var target = action.Target;

            // 朝向目标
            unit.SetFacing(target.CellPosition.Value - unit.CellPosition.Value);

            unit.StartActing();
            unit.PlayAttackAnimation();

            // 消耗弹药
            unit.Shots.Value--;

            // 计算伤害
            int damage = unit.RollDamage();

            // 检查近身惩罚：如果有敌人相邻，远程攻击伤害减半
            bool hasAdjacentEnemy = false;
            var enemies = context.GetEnemies(unit.Side);
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive && unit.IsAdjacent(enemy))
                {
                    hasAdjacentEnemy = true;
                    break;
                }
            }

            if (hasAdjacentEnemy)
            {
                damage = Mathf.RoundToInt(damage * 0.5f);
                Debug.Log($"[BattleActionExecutor] Ranged attack penalty applied: {damage}");
            }

            var damageResult = target.TakeDamage(damage, unit);

            eventSystem?.Publish(new DamageDealtEvent
            {
                Source = unit,
                Target = target,
                Damage = damageResult.FinalDamage,
                UnitsKilled = damageResult.UnitsKilled,
                IsRanged = true
            });

            target.PlayHitAnimation();

            yield return new WaitForSeconds(AttackDuration);

            // 远程攻击无反击

            unit.StopMoving();
            onComplete?.Invoke(BattleActionResult.Success(damageResult));
        }

        #endregion

        #region Ability Action

        IEnumerator ExecuteAbility(BattleAbilityAction action, Action<BattleActionResult> onComplete)
        {
            var unit = action.Unit;

            unit.StartActing();

            // 朝向目标
            if (action.Target != null)
            {
                unit.SetFacing(action.Target.CellPosition.Value - unit.CellPosition.Value);
            }

            // 激活技能
            var activationResult = unit.AbilitySystem?.TryActivateAbility(
                action.AbilityId,
                action.Target?.AbilitySystem
            );

            yield return new WaitForSeconds(AttackDuration);

            unit.StopMoving();

            if (activationResult == AbilityActivationResult.Success)
            {
                onComplete?.Invoke(BattleActionResult.Success());
            }
            else
            {
                onComplete?.Invoke(BattleActionResult.Failed($"Ability failed: {activationResult}"));
            }
        }

        #endregion

        #region Wait & Defend

        BattleActionResult ExecuteWait(BattleWaitAction action)
        {
            action.Unit.Wait();
            Debug.Log($"[BattleActionExecutor] {action.Unit.DisplayName} is waiting");
            return BattleActionResult.Success();
        }

        BattleActionResult ExecuteDefend(BattleDefendAction action)
        {
            action.Unit.Defend();
            Debug.Log($"[BattleActionExecutor] {action.Unit.DisplayName} is defending");
            return BattleActionResult.Success();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 格子坐标转世界坐标
        /// </summary>
        Vector3 CellToWorld(Vector2Int cell)
        {
            // 基础实现：1:1 映射
            // 实际项目中应该使用 Tilemap 或自定义转换
            return new Vector3(cell.x, cell.y, 0);
        }

        /// <summary>
        /// 设置坐标转换器
        /// </summary>
        public Func<Vector2Int, Vector3> CellToWorldConverter { get; set; }

        #endregion
    }
}
