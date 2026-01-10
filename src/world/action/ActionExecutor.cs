using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 行动执行器
    /// 负责执行 HeroAction 并处理动画、状态更新
    /// </summary>
    public class ActionExecutor
    {
        readonly WorldContext context;
        readonly MonoBehaviour coroutineRunner;

        // 事件
        public event Action<HeroAction> OnActionStarted;
        public event Action<HeroAction, ActionResult> OnActionCompleted;
        public event Action<Hero, Vector3Int> OnHeroMoved;

        // 配置
        public float MoveSpeed { get; set; } = 5f;
        public bool SkipAnimations { get; set; } = false;

        bool isExecuting;
        public bool IsExecuting => isExecuting;

        public ActionExecutor(WorldContext context, MonoBehaviour coroutineRunner)
        {
            this.context = context;
            this.coroutineRunner = coroutineRunner;
        }

        /// <summary>
        /// 执行行动
        /// </summary>
        public void Execute(HeroAction action, Action<ActionResult> onComplete)
        {
            if (isExecuting)
            {
                Debug.LogWarning("[ActionExecutor] 已有行动正在执行");
                onComplete?.Invoke(ActionResult.Failed("Already executing"));
                return;
            }

            if (!action.CanExecute(context))
            {
                Debug.LogWarning($"[ActionExecutor] 行动无法执行: {action.Type}");
                onComplete?.Invoke(ActionResult.Failed("Cannot execute"));
                return;
            }

            isExecuting = true;
            OnActionStarted?.Invoke(action);

            coroutineRunner.StartCoroutine(ExecuteCoroutine(action, result =>
            {
                isExecuting = false;
                OnActionCompleted?.Invoke(action, result);
                onComplete?.Invoke(result);
            }));
        }

        IEnumerator ExecuteCoroutine(HeroAction action, Action<ActionResult> onComplete)
        {
            switch (action)
            {
                case MoveAction moveAction:
                    yield return ExecuteMoveCoroutine(moveAction, onComplete);
                    yield break;

                case EnterTownAction enterTownAction:
                    onComplete?.Invoke(ExecuteEnterTown(enterTownAction));
                    break;

                case PickUpAction pickUpAction:
                    onComplete?.Invoke(ExecutePickUp(pickUpAction));
                    break;

                case AttackAction attackAction:
                    onComplete?.Invoke(ExecuteAttack(attackAction));
                    break;

                case WaitAction:
                    onComplete?.Invoke(ActionResult.Succeeded(ActionResultType.None));
                    break;

                case EndTurnAction:
                    onComplete?.Invoke(ActionResult.Succeeded(ActionResultType.TurnEnded));
                    break;

                default:
                    onComplete?.Invoke(ActionResult.Failed($"Unknown action type: {action.Type}"));
                    break;
            }
        }

        IEnumerator ExecuteMoveCoroutine(MoveAction action, Action<ActionResult> onComplete)
        {
            var hero = action.Hero;
            var path = action.Path;

            if (path == null || path.Count == 0)
            {
                onComplete?.Invoke(ActionResult.Failed("Empty path"));
                yield break;
            }

            // 消耗移动力
            int cost = action.GetMovementCost(context);
            if (!hero.ConsumeMovement(cost))
            {
                onComplete?.Invoke(ActionResult.Failed("Not enough movement points"));
                yield break;
            }

            // 移动动画
            if (!SkipAnimations)
            {
                // 开始移动动画
                hero.SetMoving(true);

                Vector3Int previousCell = hero.CellPosition.Value;

                foreach (var cell in path)
                {
                    // 设置朝向
                    Vector3Int direction = cell - previousCell;
                    hero.SetFacing(direction);

                    // 平滑移动到目标位置
                    yield return MoveToCell(hero, cell);

                    // 更新英雄逻辑位置
                    hero.MoveTo(cell);
                    OnHeroMoved?.Invoke(hero, cell);

                    previousCell = cell;
                }

                // 停止移动动画
                hero.SetMoving(false);
            }
            else
            {
                // 跳过动画，直接到达
                hero.MoveTo(action.Destination);
                OnHeroMoved?.Invoke(hero, action.Destination);
            }

            Debug.Log($"[ActionExecutor] {hero.HeroName} 移动到 {action.Destination}，剩余移动力: {hero.MovementPoints.Value}");
            onComplete?.Invoke(ActionResult.Succeeded(ActionResultType.HeroMoved));
        }

        /// <summary>
        /// 平滑移动英雄到目标格子
        /// </summary>
        IEnumerator MoveToCell(Hero hero, Vector3Int targetCell)
        {
            Vector3 startPos = hero.transform.position;
            Vector3 targetPos = context.Map.CellToWorld(targetCell);
            float duration = 1f / MoveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                hero.transform.position = Vector3.Lerp(startPos, targetPos, SmoothStep(t));
                yield return null;
            }

            hero.transform.position = targetPos;
        }

        static float SmoothStep(float t) => t * t * (3f - 2f * t);

        ActionResult ExecuteEnterTown(EnterTownAction action)
        {
            Debug.Log($"[ActionExecutor] {action.Hero.HeroName} 进入城镇 {action.Town.TownName}");
            return ActionResult.EnterTown(action.Town);
        }

        ActionResult ExecutePickUp(PickUpAction action)
        {
            // TODO: 实现拾取逻辑
            Debug.Log($"[ActionExecutor] {action.Hero.HeroName} 拾取物品");
            return ActionResult.Succeeded(ActionResultType.ResourceGained);
        }

        ActionResult ExecuteAttack(AttackAction action)
        {
            Debug.Log($"[ActionExecutor] {action.Hero.HeroName} 攻击目标");
            return ActionResult.TriggerBattle(action.Target);
        }
    }
}
