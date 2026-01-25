using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 战斗格子
    /// </summary>
    public class BattleTile
    {
        /// <summary>
        /// 格子位置
        /// </summary>
        public Vector2Int Position { get; }

        /// <summary>
        /// X 坐标
        /// </summary>
        public int X => Position.x;

        /// <summary>
        /// Y 坐标
        /// </summary>
        public int Y => Position.y;

        /// <summary>
        /// 格子类型
        /// </summary>
        public BattleTileType Type { get; set; } = BattleTileType.Normal;

        /// <summary>
        /// 是否可通行
        /// </summary>
        public bool IsWalkable => Type != BattleTileType.Obstacle;

        /// <summary>
        /// 移动消耗
        /// </summary>
        public int MovementCost => Type == BattleTileType.Difficult ? 2 : 1;

        /// <summary>
        /// 占据此格的单位（可为空）
        /// </summary>
        public BattleUnit OccupyingUnit { get; set; }

        /// <summary>
        /// 是否被占据
        /// </summary>
        public bool IsOccupied => OccupyingUnit != null;

        /// <summary>
        /// 是否可作为移动目标（可通行且未被占据）
        /// </summary>
        public bool IsAvailable => IsWalkable && !IsOccupied;

        public BattleTile(int x, int y)
        {
            Position = new Vector2Int(x, y);
        }

        public BattleTile(Vector2Int position)
        {
            Position = position;
        }

        public override string ToString()
        {
            return $"Tile({X}, {Y}) Type={Type} Occupied={IsOccupied}";
        }
    }
}
