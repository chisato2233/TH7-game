using System;
using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄行动类型
    /// </summary>
    public enum HeroActionType
    {
        None,
        Move,           // 移动到目标格子
        EnterTown,      // 进入城镇
        PickUp,         // 拾取物品/资源
        Attack,         // 攻击目标
        Interact,       // 与地图物件交互
        Wait,           // 等待（跳过当前英雄）
        EndTurn         // 结束回合
    }

    /// <summary>
    /// 英雄行动基类
    /// </summary>
    public abstract class HeroAction
    {
        public HeroData Hero { get; }
        public abstract HeroActionType Type { get; }

        protected HeroAction(HeroData hero)
        {
            Hero = hero;
        }

        /// <summary>
        /// 验证行动是否可执行
        /// </summary>
        public abstract bool CanExecute(WorldContext context);

        /// <summary>
        /// 获取移动力消耗
        /// </summary>
        public abstract int GetMovementCost(WorldContext context);
    }

    /// <summary>
    /// 移动行动
    /// </summary>
    public class MoveAction : HeroAction
    {
        public List<Vector3Int> Path { get; }
        public Vector3Int Destination => Path?.Count > 0 ? Path[^1] : Hero.CellPosition;

        public override HeroActionType Type => HeroActionType.Move;

        int? cachedCost;

        public MoveAction(HeroData hero, List<Vector3Int> path) : base(hero)
        {
            Path = path ?? new List<Vector3Int>();
        }

        public override bool CanExecute(WorldContext context)
        {
            if (Path == null || Path.Count == 0) return false;
            return Hero.MovementPoints.Value >= GetMovementCost(context);
        }

        public override int GetMovementCost(WorldContext context)
        {
            if (cachedCost.HasValue) return cachedCost.Value;

            int total = 0;
            foreach (var cell in Path)
            {
                total += context.Map.GetMovementCost(context.Map.CellToWorld(cell));
            }
            cachedCost = total;
            return total;
        }
    }

    /// <summary>
    /// 进入城镇行动
    /// </summary>
    public class EnterTownAction : HeroAction
    {
        public TownData Town { get; }

        public override HeroActionType Type => HeroActionType.EnterTown;

        public EnterTownAction(HeroData hero, TownData town) : base(hero)
        {
            Town = town;
        }

        public override bool CanExecute(WorldContext context)
        {
            return Town != null;
        }

        public override int GetMovementCost(WorldContext context) => 0;
    }

    /// <summary>
    /// 拾取物品行动
    /// </summary>
    public class PickUpAction : HeroAction
    {
        public Vector3Int TargetCell { get; }
        public MapObjectType ObjectType { get; }

        public override HeroActionType Type => HeroActionType.PickUp;

        public PickUpAction(HeroData hero, Vector3Int cell, MapObjectType objectType) : base(hero)
        {
            TargetCell = cell;
            ObjectType = objectType;
        }

        public override bool CanExecute(WorldContext context)
        {
            // 检查目标格子是否有可拾取物品
            return true; // 简化实现
        }

        public override int GetMovementCost(WorldContext context) => 0;
    }

    /// <summary>
    /// 攻击行动
    /// </summary>
    public class AttackAction : HeroAction
    {
        public Vector3Int TargetCell { get; }
        public object Target { get; } // 可以是 HeroData 或 MonsterData

        public override HeroActionType Type => HeroActionType.Attack;

        public AttackAction(HeroData hero, Vector3Int cell, object target) : base(hero)
        {
            TargetCell = cell;
            Target = target;
        }

        public override bool CanExecute(WorldContext context)
        {
            return Target != null;
        }

        public override int GetMovementCost(WorldContext context) => 0;
    }

    /// <summary>
    /// 等待行动（跳过当前英雄）
    /// </summary>
    public class WaitAction : HeroAction
    {
        public override HeroActionType Type => HeroActionType.Wait;

        public WaitAction(HeroData hero) : base(hero) { }

        public override bool CanExecute(WorldContext context) => true;
        public override int GetMovementCost(WorldContext context) => 0;
    }

    /// <summary>
    /// 结束回合行动
    /// </summary>
    public class EndTurnAction : HeroAction
    {
        public override HeroActionType Type => HeroActionType.EndTurn;

        public EndTurnAction(HeroData hero) : base(hero) { }

        public override bool CanExecute(WorldContext context) => true;
        public override int GetMovementCost(WorldContext context) => 0;
    }
}
