using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 寻路接口
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// 寻找从起点到终点的路径
        /// </summary>
        /// <param name="start">起点格子坐标</param>
        /// <param name="end">终点格子坐标</param>
        /// <param name="context">世界上下文</param>
        /// <returns>路径（不包含起点），null 表示无法到达</returns>
        List<Vector3Int> FindPath(Vector3Int start, Vector3Int end, WorldContext context);

        /// <summary>
        /// 获取可到达范围
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="maxCost">最大移动力消耗</param>
        /// <param name="context">世界上下文</param>
        /// <returns>可到达的格子及其消耗</returns>
        Dictionary<Vector3Int, int> GetReachableArea(Vector3Int start, int maxCost, WorldContext context);
    }

    /// <summary>
    /// 简单优先队列实现（Unity 兼容）
    /// </summary>
    class SimplePriorityQueue<T>
    {
        readonly List<(T item, float priority)> items = new();
        readonly HashSet<T> itemSet = new();

        public int Count => items.Count;

        public void Enqueue(T item, float priority)
        {
            items.Add((item, priority));
            itemSet.Add(item);
        }

        public T Dequeue()
        {
            if (items.Count == 0) return default;

            int minIndex = 0;
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].priority < items[minIndex].priority)
                    minIndex = i;
            }

            var result = items[minIndex].item;
            items.RemoveAt(minIndex);
            itemSet.Remove(result);
            return result;
        }

        public bool Contains(T item) => itemSet.Contains(item);
    }

    /// <summary>
    /// 简单寻路实现（A* 算法）
    /// </summary>
    public class SimplePathfinder : IPathfinder
    {
        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int end, WorldContext context)
        {
            if (context?.Map?.Data == null) return null;

            var mapData = context.Map.Data;
            var terrainConfig = context.Map.terrainConfig;

            // A* 算法
            var openSet = new SimplePriorityQueue<Vector3Int>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, float> { [start] = 0 };
            var fScore = new Dictionary<Vector3Int, float> { [start] = Heuristic(start, end) };

            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current == end)
                    return ReconstructPath(cameFrom, current);

                foreach (var neighbor in GetNeighbors(current, mapData))
                {
                    var tile = mapData.GetTileAtCell(neighbor);
                    if (!terrainConfig.IsPassable(tile))
                        continue;

                    float moveCost = terrainConfig.GetMovementCost(tile);
                    float tentativeG = gScore[current] + moveCost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, end);

                        if (!openSet.Contains(neighbor))
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }

            return null; // 无法到达
        }

        public Dictionary<Vector3Int, int> GetReachableArea(Vector3Int start, int maxCost, WorldContext context)
        {
            var result = new Dictionary<Vector3Int, int>();
            var mapData = context?.Map?.Data;
            var terrainConfig = context?.Map?.terrainConfig;

            if (mapData == null || terrainConfig == null) return result;

            var visited = new Dictionary<Vector3Int, int> { [start] = 0 };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentCost = visited[current];

                foreach (var neighbor in GetNeighbors(current, mapData))
                {
                    var tile = mapData.GetTileAtCell(neighbor);
                    if (!terrainConfig.IsPassable(tile))
                        continue;

                    int moveCost = terrainConfig.GetMovementCost(tile);
                    int totalCost = currentCost + moveCost;

                    if (totalCost <= maxCost && (!visited.ContainsKey(neighbor) || visited[neighbor] > totalCost))
                    {
                        visited[neighbor] = totalCost;
                        result[neighbor] = totalCost;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result;
        }

        List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            var path = new List<Vector3Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            path.RemoveAt(0); // 移除起点
            return path;
        }

        float Heuristic(Vector3Int a, Vector3Int b)
        {
            // 曼哈顿距离
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell, MapData mapData)
        {
            // 四方向（可扩展为六方向或八方向）
            var directions = new Vector3Int[]
            {
                new(1, 0, 0),
                new(-1, 0, 0),
                new(0, 1, 0),
                new(0, -1, 0)
            };

            foreach (var dir in directions)
            {
                var neighbor = cell + dir;
                int x = neighbor.x - mapData.Origin.x;
                int y = neighbor.y - mapData.Origin.y;
                if (mapData.IsInBounds(x, y))
                    yield return neighbor;
            }
        }
    }
}
