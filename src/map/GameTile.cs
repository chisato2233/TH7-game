using UnityEngine;
using UnityEngine.Tilemaps;

namespace TH7
{
    [CreateAssetMenu(fileName = "GameTile", menuName = "TH7/Game Tile")]
    public class GameTile : TileBase
    {
        public Sprite Sprite;
        public GroundType Ground = GroundType.Land;
        public SurfaceType Surface = SurfaceType.None;
        public BiomeType Biome = BiomeType.Neutral;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = Sprite;
            tileData.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
        }

        public Tile ToLogicTile()
        {
            return new Tile(Ground, Surface, Biome);
        }
    }
}
