using UnityEngine;
using UnityEngine.Tilemaps;

namespace TH7
{
    public class MapManager : MonoBehaviour
    {
        [Header("Tilemap")]
        public Tilemap groundTilemap;

        [Header("Config")]
        public TerrainConfig terrainConfig;

        public MapData Data { get; private set; }

        void Awake()
        {
            InitializeMap();
        }

        public void InitializeMap()
        {
            if (groundTilemap == null)
            {
                Debug.LogError("[MapManager] groundTilemap is null");
                return;
            }

            Data = new MapData();
            Data.InitFromTilemap(groundTilemap);
            Debug.Log($"[MapManager] Map initialized: {Data.Width}x{Data.Height}");
        }

        // 世界坐标转格子坐标
        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            return groundTilemap.WorldToCell(worldPos);
        }

        // 格子坐标转世界坐标（格子中心）
        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return groundTilemap.GetCellCenterWorld(cellPos);
        }

        // 获取指定位置的逻辑 Tile
        public Tile GetTileAt(Vector3 worldPos)
        {
            var cell = WorldToCell(worldPos);
            return Data.GetTileAtCell(cell);
        }

        // 检查位置是否可通行
        public bool IsPassable(Vector3 worldPos)
        {
            var cell = WorldToCell(worldPos);
            int x = cell.x - Data.Origin.x;
            int y = cell.y - Data.Origin.y;
            return Data.IsPassable(x, y, terrainConfig);
        }

        // 获取移动消耗
        public int GetMovementCost(Vector3 worldPos)
        {
            var cell = WorldToCell(worldPos);
            int x = cell.x - Data.Origin.x;
            int y = cell.y - Data.Origin.y;
            return Data.GetMovementCost(x, y, terrainConfig);
        }
    }
}
