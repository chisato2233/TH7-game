using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 英雄数据（可序列化，存档用）
    /// </summary>
    [Serializable]
    public class HeroData
    {
        public string HeroId;
        public string HeroName;

        // 位置
        public Vector3Int CellPosition;

        // 移动力
        public Reactive<int> MovementPoints = new(20);
        public int MaxMovementPoints = 20;

        // 所属
        public int OwnerPlayerId;
        public bool IsPlayerControlled => OwnerPlayerId == 0;

        // 军队（7个槽位）
        public UnitStack[] Army = new UnitStack[7];

        public HeroData() { }

        public HeroData(string id, string name, Vector3Int position, int ownerId = 0)
        {
            HeroId = id;
            HeroName = name;
            CellPosition = position;
            OwnerPlayerId = ownerId;
        }

        /// <summary>
        /// 消耗移动力
        /// </summary>
        public bool ConsumeMovement(int cost)
        {
            if (MovementPoints.Value < cost) return false;
            MovementPoints.Value -= cost;
            return true;
        }

        /// <summary>
        /// 重置移动力（新回合）
        /// </summary>
        public void ResetMovement()
        {
            MovementPoints.Value = MaxMovementPoints;
        }

        /// <summary>
        /// 检查是否还能行动
        /// </summary>
        public bool CanAct => MovementPoints.Value > 0;
    }
}
