using UnityEngine;
using UnityEngine.Tilemaps;

namespace TH7
{
    // 运行时地图数据，从 Tilemap 初始化
    public class MapData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Vector3Int Origin { get; private set; }

        Tile[] tiles;

        public void InitFromTilemap(Tilemap tilemap)
        {
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;

            Origin = new Vector3Int(bounds.xMin, bounds.yMin, 0);
            Width = bounds.size.x;
            Height = bounds.size.y;
            tiles = new Tile[Width * Height];

            int gameTileCount = 0;
            int fallbackCount = 0;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cellPos = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);

                    // 优先尝试获取 GameTile
                    var gameTile = tilemap.GetTile<GameTile>(cellPos);
                    if (gameTile != null)
                    {
                        tiles[y * Width + x] = gameTile.ToLogicTile();
                        gameTileCount++;
                    }
                    else
                    {
                        // 回退：检查是否有任何 Tile（普通 Unity Tile）
                        var anyTile = tilemap.GetTile(cellPos);
                        if (anyTile != null)
                        {
                            // 普通 Tile 默认为可通行陆地
                            tiles[y * Width + x] = new Tile(GroundType.Land);
                            fallbackCount++;
                        }
                        else
                        {
                            tiles[y * Width + x] = new Tile(GroundType.Void);
                        }
                    }
                }
            }

            Debug.Log($"[MapData] 初始化完成: GameTile={gameTileCount}, 普通Tile={fallbackCount}, Void={Width * Height - gameTileCount - fallbackCount}");
        }

        public Tile GetTile(int x, int y)
        {
            if (!IsInBounds(x, y)) return new Tile(GroundType.Void);
            return tiles[y * Width + x];
        }

        public Tile GetTileAtCell(Vector3Int cellPos)
        {
            int x = cellPos.x - Origin.x;
            int y = cellPos.y - Origin.y;
            return GetTile(x, y);
        }

        public void SetTile(int x, int y, Tile tile)
        {
            if (!IsInBounds(x, y)) return;
            tiles[y * Width + x] = tile;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsPassable(int x, int y, TerrainConfig config)
        {
            var tile = GetTile(x, y);
            return config.IsPassable(tile);
        }

        public int GetMovementCost(int x, int y, TerrainConfig config)
        {
            var tile = GetTile(x, y);
            return config.GetMovementCost(tile);
        }
    }
}
