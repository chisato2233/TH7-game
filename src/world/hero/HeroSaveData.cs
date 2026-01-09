using System;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄存档数据（纯数据结构，用于序列化）
    /// </summary>
    [Serializable]
    public struct HeroSaveData
    {
        public string HeroId;
        public string HeroName;
        public int OwnerPlayerId;

        public Vector3Int CellPosition;
        public int MovementPoints;
        public int MaxMovementPoints;

        public UnitStack[] Army;

        // TODO: 添加更多需要存档的数据
        // public List<string> LearnedAbilities;
        // public Dictionary<string, int> Attributes;
    }
}
