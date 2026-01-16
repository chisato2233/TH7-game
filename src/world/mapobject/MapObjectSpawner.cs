using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 资源生成配置
    /// </summary>
    [System.Serializable]
    public class ResourceSpawnConfig
    {
        public ResourceType ResourceType;
        public GameObject Prefab;
        [Range(0, 100)] public int Weight = 10;
        public int MinAmount = 3;
        public int MaxAmount = 10;
    }

    /// <summary>
    /// 地图物件生成器
    /// 运行时随机在可通行地块上生成资源堆
    /// </summary>
    public class MapObjectSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] MapManager mapManager;

        [Header("Spawn Settings")]
        [SerializeField] int spawnCount = 20;
        [SerializeField] int minDistanceFromOrigin = 3;
        [SerializeField] int minDistanceBetween = 2;

        [Header("Resource Configs")]
        [SerializeField] List<ResourceSpawnConfig> resourceConfigs = new();

        [Header("Debug")]
        [SerializeField] bool debugMode;

        readonly List<Vector3Int> spawnedPositions = new();
        readonly List<MapObject> spawnedObjects = new();

        /// <summary>
        /// 初始化并生成资源（由 WorldSceneController 调用）
        /// </summary>
        public void Initialize(MapManager map)
        {
            mapManager = map;
            SpawnResources();
        }

        /// <summary>
        /// 生成资源堆
        /// </summary>
        public void SpawnResources()
        {
            if (mapManager?.Data == null)
            {
                Debug.LogError("[MapObjectSpawner] MapManager or MapData is null");
                return;
            }

            if (resourceConfigs.Count == 0)
            {
                Debug.LogWarning("[MapObjectSpawner] No resource configs defined");
                return;
            }

            spawnedPositions.Clear();
            int spawned = 0;
            int attempts = 0;
            int maxAttempts = spawnCount * 10;

            while (spawned < spawnCount && attempts < maxAttempts)
            {
                attempts++;

                var cell = GetRandomPassableCell();
                if (!cell.HasValue) continue;

                if (!IsValidSpawnPosition(cell.Value)) continue;

                var config = SelectRandomConfig();
                if (config?.Prefab == null) continue;

                var obj = SpawnResourcePile(cell.Value, config);
                if (obj != null)
                {
                    spawnedPositions.Add(cell.Value);
                    spawnedObjects.Add(obj);
                    spawned++;

                    if (debugMode)
                        Debug.Log($"[MapObjectSpawner] Spawned {config.ResourceType} at {cell.Value}");
                }
            }

            Debug.Log($"[MapObjectSpawner] Spawned {spawned}/{spawnCount} resources in {attempts} attempts");
        }

        Vector3Int? GetRandomPassableCell()
        {
            var data = mapManager.Data;
            int x = Random.Range(0, data.Width);
            int y = Random.Range(0, data.Height);

            var cell = new Vector3Int(x + data.Origin.x, y + data.Origin.y, 0);

            if (data.IsPassable(x, y, mapManager.terrainConfig))
                return cell;

            return null;
        }

        bool IsValidSpawnPosition(Vector3Int cell)
        {
            // 检查与原点距离
            if (Mathf.Abs(cell.x) < minDistanceFromOrigin && Mathf.Abs(cell.y) < minDistanceFromOrigin)
                return false;

            // 检查与已生成物件的距离
            foreach (var pos in spawnedPositions)
            {
                int dist = Mathf.Abs(cell.x - pos.x) + Mathf.Abs(cell.y - pos.y);
                if (dist < minDistanceBetween)
                    return false;
            }

            return true;
        }

        ResourceSpawnConfig SelectRandomConfig()
        {
            int totalWeight = 0;
            foreach (var config in resourceConfigs)
                totalWeight += config.Weight;

            if (totalWeight <= 0) return resourceConfigs[0];

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var config in resourceConfigs)
            {
                cumulative += config.Weight;
                if (roll < cumulative)
                    return config;
            }

            return resourceConfigs[0];
        }

        MapObject SpawnResourcePile(Vector3Int cell, ResourceSpawnConfig config)
        {
            var worldPos = mapManager.CellToWorld(cell);
            var go = Instantiate(config.Prefab, worldPos, Quaternion.identity, transform);

            var pile = go.GetComponent<ResourcePile>();
            if (pile != null)
            {
                // 设置随机数量
                int amount = Random.Range(config.MinAmount, config.MaxAmount + 1);
                SetResourcePileAmount(pile, config.ResourceType, amount);
                return pile;
            }

            // 如果不是 ResourcePile，尝试获取 MapObject
            var mapObj = go.GetComponent<MapObject>();
            if (mapObj != null)
                return mapObj;

            Debug.LogWarning($"[MapObjectSpawner] Prefab {config.Prefab.name} has no MapObject component");
            Destroy(go);
            return null;
        }

        void SetResourcePileAmount(ResourcePile pile, ResourceType type, int amount)
        {
            // 通过反射或 SerializeField 设置私有字段
            var typeField = typeof(ResourcePile).GetField("resourceType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var amountField = typeof(ResourcePile).GetField("amount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            typeField?.SetValue(pile, type);
            amountField?.SetValue(pile, amount);
        }

        /// <summary>
        /// 清除所有生成的物件
        /// </summary>
        public void ClearSpawnedObjects()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj.gameObject);
            }
            spawnedObjects.Clear();
            spawnedPositions.Clear();
        }

        /// <summary>
        /// 获取所有生成的物件（用于注册到 WorldContext）
        /// </summary>
        public IReadOnlyList<MapObject> GetSpawnedObjects() => spawnedObjects;

        void OnDestroy()
        {
            ClearSpawnedObjects();
        }
    }
}
