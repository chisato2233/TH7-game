using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗行动基类
    /// </summary>
    public abstract class BattleAction
    {
        /// <summary>
        /// 执行行动的单位
        /// </summary>
        public BattleUnit Unit { get; }

        /// <summary>
        /// 行动类型
        /// </summary>
        public abstract BattleActionType Type { get; }

        protected BattleAction(BattleUnit unit)
        {
            Unit = unit;
        }

        /// <summary>
        /// 检查是否可以执行
        /// </summary>
        public abstract bool CanExecute(BattleContext context);

        public override string ToString()
        {
            return $"{Type} by {Unit?.DisplayName}";
        }
    }

    /// <summary>
    /// 移动行动
    /// </summary>
    public class BattleMoveAction : BattleAction
    {
        /// <summary>
        /// 移动路径
        /// </summary>
        public List<Vector2Int> Path { get; }

        /// <summary>
        /// 目标位置
        /// </summary>
        public Vector2Int Destination => Path?.Count > 0 ? Path[^1] : Unit.CellPosition.Value;

        public override BattleActionType Type => BattleActionType.Move;

        public BattleMoveAction(BattleUnit unit, List<Vector2Int> path) : base(unit)
        {
            Path = path;
        }

        public override bool CanExecute(BattleContext context)
        {
            return Unit != null && Unit.IsAlive && Path != null && Path.Count > 0;
        }

        public override string ToString()
        {
            return $"Move {Unit?.DisplayName} to {Destination}";
        }
    }

    /// <summary>
    /// 近战攻击行动
    /// </summary>
    public class BattleAttackAction : BattleAction
    {
        /// <summary>
        /// 攻击目标
        /// </summary>
        public BattleUnit Target { get; }

        /// <summary>
        /// 移动后攻击的路径（可为空）
        /// </summary>
        public List<Vector2Int> MovePath { get; }

        /// <summary>
        /// 攻击位置（移动后的位置）
        /// </summary>
        public Vector2Int AttackPosition => MovePath?.Count > 0 ? MovePath[^1] : Unit.CellPosition.Value;

        public override BattleActionType Type => BattleActionType.Attack;

        public BattleAttackAction(BattleUnit unit, BattleUnit target, List<Vector2Int> movePath = null) : base(unit)
        {
            Target = target;
            MovePath = movePath;
        }

        public override bool CanExecute(BattleContext context)
        {
            return Unit != null && Unit.IsAlive && Target != null && Target.IsAlive;
        }

        public override string ToString()
        {
            return $"Attack {Target?.DisplayName} by {Unit?.DisplayName}";
        }
    }

    /// <summary>
    /// 远程攻击行动
    /// </summary>
    public class BattleRangedAttackAction : BattleAction
    {
        /// <summary>
        /// 攻击目标
        /// </summary>
        public BattleUnit Target { get; }

        public override BattleActionType Type => BattleActionType.RangedAttack;

        public BattleRangedAttackAction(BattleUnit unit, BattleUnit target) : base(unit)
        {
            Target = target;
        }

        public override bool CanExecute(BattleContext context)
        {
            return Unit != null &&
                   Unit.IsAlive &&
                   Unit.IsRanged &&
                   Unit.Shots.Value > 0 &&
                   Target != null &&
                   Target.IsAlive;
        }

        public override string ToString()
        {
            return $"Ranged Attack {Target?.DisplayName} by {Unit?.DisplayName}";
        }
    }

    /// <summary>
    /// 技能行动
    /// </summary>
    public class BattleAbilityAction : BattleAction
    {
        /// <summary>
        /// 技能 ID
        /// </summary>
        public string AbilityId { get; }

        /// <summary>
        /// 技能目标单位（可为空）
        /// </summary>
        public BattleUnit Target { get; }

        /// <summary>
        /// 技能目标位置（可为空）
        /// </summary>
        public Vector2Int? TargetPosition { get; }

        public override BattleActionType Type => BattleActionType.Ability;

        public BattleAbilityAction(BattleUnit unit, string abilityId, BattleUnit target = null, Vector2Int? targetPos = null)
            : base(unit)
        {
            AbilityId = abilityId;
            Target = target;
            TargetPosition = targetPos;
        }

        public override bool CanExecute(BattleContext context)
        {
            if (Unit == null || !Unit.IsAlive || string.IsNullOrEmpty(AbilityId))
                return false;

            // 检查技能是否可用
            var result = Unit.AbilitySystem?.CanActivateAbility(AbilityId);
            return result == AbilityActivationResult.Success;
        }

        public override string ToString()
        {
            return $"Use Ability {AbilityId} by {Unit?.DisplayName}";
        }
    }

    /// <summary>
    /// 等待行动
    /// </summary>
    public class BattleWaitAction : BattleAction
    {
        public override BattleActionType Type => BattleActionType.Wait;

        public BattleWaitAction(BattleUnit unit) : base(unit) { }

        public override bool CanExecute(BattleContext context)
        {
            return Unit != null && Unit.IsAlive && Unit.CanAct;
        }

        public override string ToString()
        {
            return $"Wait by {Unit?.DisplayName}";
        }
    }

    /// <summary>
    /// 防御行动
    /// </summary>
    public class BattleDefendAction : BattleAction
    {
        public override BattleActionType Type => BattleActionType.Defend;

        public BattleDefendAction(BattleUnit unit) : base(unit) { }

        public override bool CanExecute(BattleContext context)
        {
            return Unit != null && Unit.IsAlive && Unit.CanAct;
        }

        public override string ToString()
        {
            return $"Defend by {Unit?.DisplayName}";
        }
    }
}
