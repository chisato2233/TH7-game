using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 战斗地图管理器
    /// 标准战场: 15x11 格子
    /// </summary>
    public class BattleMap
    {
        /// <summary>
        /// 地图宽度
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// 地图高度
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 地形类型
        /// </summary>
        public BattleTerrainType TerrainType { get; private set; }

        /// <summary>
        /// 格子数组
        /// </summary>
        BattleTile[,] tiles;

        /// <summary>
        /// 攻击方起始列（左侧）
        /// </summary>
        public int AttackerStartColumn => 0;

        /// <summary>
        /// 防守方起始列（右侧）
        /// </summary>
        public int DefenderStartColumn => Width - 1;

        /// <summary>
        /// 初始化地图
        /// </summary>
        public void Initialize(int width, int height, BattleTerrainType terrain)
        {
            Width = width;
            Height = height;
            TerrainType = terrain;

            tiles = new BattleTile[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = new BattleTile(x, y);
                }
            }

            // 生成障碍物
            GenerateObstacles();

            Debug.Log($"[BattleMap] Initialized {width}x{height} map with terrain {terrain}");
        }

        /// <summary>
        /// 生成障碍物（可根据地形类型自定义）
        /// </summary>
        void GenerateObstacles()
        {
            // 基础实现：在地图中央随机放置一些障碍物
            // 保证两侧起始位置不会有障碍物
            int obstacleCount = Mathf.RoundToInt(Width * Height * 0.05f); // 5% 的格子

            for (int i = 0; i < obstacleCount; i++)
            {
                int x = Random.Range(2, Width - 2); // 避开两侧起始列
                int y = Random.Range(0, Height);

                var tile = GetTile(x, y);
                if (tile != null && tile.Type == BattleTileType.Normal)
                {
                    tile.Type = BattleTileType.Obstacle;
                }
            }
        }

        /// <summary>
        /// 获取指定位置的格子
        /// </summary>
        public BattleTile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return tiles[x, y];
        }

        /// <summary>
        /// 获取指定位置的格子
        /// </summary>
        public BattleTile GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        /// <summary>
        /// 检查位置是否可通行
        /// </summary>
        public bool IsWalkable(Vector2Int pos)
        {
            var tile = GetTile(pos);
            return tile != null && tile.IsWalkable;
        }

        /// <summary>
        /// 检查位置是否可作为移动目标
        /// </summary>
        public bool IsAvailable(Vector2Int pos)
        {
            var tile = GetTile(pos);
            return tile != null && tile.IsAvailable;
        }

        /// <summary>
        /// 获取单位可达的所有格子
        /// </summary>
        public List<Vector2Int> GetReachableTiles(BattleUnit unit)
        {
            var result = new List<Vector2Int>();
            var start = unit.CellPosition.Value;
            int range = unit.GetMovementRange();
            bool isFlying = unit.IsFlying;

            // BFS 搜索可达格子
            var visited = new Dictionary<Vector2Int, int>(); // position -> cost
            var queue = new Queue<(Vector2Int pos, int cost)>();
            queue.Enqueue((start, 0));
            visited[start] = 0;

            while (queue.Count > 0)
            {
                var (pos, cost) = queue.Dequeue();

                foreach (var neighbor in GetNeighbors(pos))
                {
                    if (visited.ContainsKey(neighbor)) continue;

                    var tile = GetTile(neighbor);
                    if (tile == null) continue;

                    // 飞行单位无视地形
                    if (!isFlying && !tile.IsWalkable) continue;

                    int moveCost = isFlying ? 1 : tile.MovementCost;
                    int newCost = cost + moveCost;

                    if (newCost <= range)
                    {
                        visited[neighbor] = newCost;

                        // 空格可以作为目标
                        if (!tile.IsOccupied)
                        {
                            result.Add(neighbor);
                        }

                        // 如果是友方单位占据，可以穿过但不能停留
                        if (!tile.IsOccupied || (tile.OccupyingUnit != null && tile.OccupyingUnit.Side == unit.Side))
                        {
                            queue.Enqueue((neighbor, newCost));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取可攻击的目标
        /// </summary>
        public List<BattleUnit> GetAttackableTargets(BattleUnit unit, BattleContext context)
        {
            var result = new List<BattleUnit>();
            var enemies = context.GetEnemies(unit.Side);

            if (unit.IsRanged && unit.Shots.Value > 0)
            {
                // 远程单位可以攻击所有存活敌人
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive)
                        result.Add(enemy);
                }
            }
            else
            {
                // 近战单位只能攻击相邻敌人
                foreach (var neighbor in GetNeighbors(unit.CellPosition.Value))
                {
                    var tile = GetTile(neighbor);
                    if (tile?.OccupyingUnit != null &&
                        tile.OccupyingUnit.Side != unit.Side &&
                        tile.OccupyingUnit.IsAlive)
                    {
                        result.Add(tile.OccupyingUnit);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取移动后可攻击的目标（近战单位用）
        /// </summary>
        public List<(BattleUnit target, Vector2Int attackPosition)> GetMoveAndAttackTargets(BattleUnit unit, BattleContext context)
        {
            var result = new List<(BattleUnit, Vector2Int)>();

            if (unit.IsRanged) return result; // 远程单位不需要移动攻击

            var reachableTiles = GetReachableTiles(unit);
            var enemies = context.GetEnemies(unit.Side);

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;

                // 检查是否有相邻的可达格子
                foreach (var neighbor in GetNeighbors(enemy.CellPosition.Value))
                {
                    if (reachableTiles.Contains(neighbor) || neighbor == unit.CellPosition.Value)
                    {
                        result.Add((enemy, neighbor));
                        break; // 每个敌人只需要一个攻击位置
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取相邻格子（8方向）
        /// </summary>
        public List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            var neighbors = new List<Vector2Int>();

            // 8方向
            var directions = new Vector2Int[]
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right,
                new Vector2Int(1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                if (IsValidPosition(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 检查两个位置是否相邻
        /// </summary>
        public bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            var diff = a - b;
            return Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1 && diff != Vector2Int.zero;
        }

        /// <summary>
        /// 寻路（A* 算法）
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to, BattleUnit unit)
        {
            if (from == to) return new List<Vector2Int>();

            bool isFlying = unit?.IsFlying ?? false;

            var openSet = new SortedSet<(int f, int g, Vector2Int pos)>(
                Comparer<(int f, int g, Vector2Int pos)>.Create((a, b) =>
                {
                    int cmp = a.f.CompareTo(b.f);
                    if (cmp != 0) return cmp;
                    cmp = a.g.CompareTo(b.g);
                    if (cmp != 0) return cmp;
                    cmp = a.pos.x.CompareTo(b.pos.x);
                    if (cmp != 0) return cmp;
                    return a.pos.y.CompareTo(b.pos.y);
                }));

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { [from] = 0 };

            openSet.Add((Heuristic(from, to), 0, from));

            while (openSet.Count > 0)
            {
                var current = openSet.Min;
                openSet.Remove(current);
                var currentPos = current.pos;

                if (currentPos == to)
                {
                    return ReconstructPath(cameFrom, currentPos);
                }

                foreach (var neighbor in GetNeighbors(currentPos))
                {
                    var tile = GetTile(neighbor);
                    if (tile == null) continue;
                    if (!isFlying && !tile.IsWalkable) continue;

                    // 目标位置特殊处理：允许有敌人
                    if (neighbor != to && tile.IsOccupied) continue;

                    int cost = isFlying ? 1 : tile.MovementCost;
                    int tentativeG = gScore[currentPos] + cost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = currentPos;
                        gScore[neighbor] = tentativeG;
                        int f = tentativeG + Heuristic(neighbor, to);
                        openSet.Add((f, tentativeG, neighbor));
                    }
                }
            }

            return null; // 无法到达
        }

        int Heuristic(Vector2Int a, Vector2Int b)
        {
            // Chebyshev distance（8方向移动）
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            path.RemoveAt(0); // 移除起点
            return path;
        }

        /// <summary>
        /// 获取攻击方起始位置
        /// </summary>
        public Vector2Int GetAttackerStartPosition(int index)
        {
            int centerY = Height / 2;
            int offset = index / 2;
            int y = index % 2 == 0 ? centerY + offset : centerY - offset - 1;
            y = Mathf.Clamp(y, 0, Height - 1);
            return new Vector2Int(AttackerStartColumn, y);
        }

        /// <summary>
        /// 获取防守方起始位置
        /// </summary>
        public Vector2Int GetDefenderStartPosition(int index)
        {
            int centerY = Height / 2;
            int offset = index / 2;
            int y = index % 2 == 0 ? centerY + offset : centerY - offset - 1;
            y = Mathf.Clamp(y, 0, Height - 1);
            return new Vector2Int(DefenderStartColumn, y);
        }

        /// <summary>
        /// 放置单位到指定位置
        /// </summary>
        public bool PlaceUnit(BattleUnit unit, Vector2Int position)
        {
            var tile = GetTile(position);
            if (tile == null || tile.IsOccupied)
            {
                Debug.LogWarning($"[BattleMap] Cannot place unit at {position}");
                return false;
            }

            tile.OccupyingUnit = unit;
            unit.CellPosition.Value = position;
            return true;
        }

        /// <summary>
        /// 移动单位
        /// </summary>
        public void MoveUnit(BattleUnit unit, Vector2Int newPosition)
        {
            var oldTile = GetTile(unit.CellPosition.Value);
            var newTile = GetTile(newPosition);

            if (oldTile != null)
            {
                oldTile.OccupyingUnit = null;
            }

            if (newTile != null)
            {
                newTile.OccupyingUnit = unit;
            }

            unit.CellPosition.Value = newPosition;
        }

        /// <summary>
        /// 移除单位
        /// </summary>
        public void RemoveUnit(BattleUnit unit)
        {
            var tile = GetTile(unit.CellPosition.Value);
            if (tile != null && tile.OccupyingUnit == unit)
            {
                tile.OccupyingUnit = null;
            }
        }

        /// <summary>
        /// 清理地图
        /// </summary>
        public void Dispose()
        {
            if (tiles != null)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        tiles[x, y] = null;
                    }
                }
                tiles = null;
            }
        }
    }
}
