using System;

namespace TH7
{
    [Serializable]
    public struct Tile
    {
        public GroundType Ground;
        public SurfaceType Surface;
        public MapObjectType Object;
        public BiomeType Biome;

        public Tile(GroundType ground, SurfaceType surface = SurfaceType.None,
                    BiomeType biome = BiomeType.Neutral, MapObjectType obj = MapObjectType.None)
        {
            Ground = ground;
            Surface = surface;
            Biome = biome;
            Object = obj;
        }

        public static Tile CreateLand(BiomeType biome = BiomeType.Neutral)
        {
            return new Tile(GroundType.Land, SurfaceType.None, biome);
        }

        public static Tile CreateWater()
        {
            return new Tile(GroundType.Water);
        }

        public static Tile CreateMountain(BiomeType biome = BiomeType.Neutral)
        {
            return new Tile(GroundType.Land, SurfaceType.Mountain, biome);
        }
    }
}
