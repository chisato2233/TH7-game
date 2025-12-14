using UnityEngine;

namespace TH7
{
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "TH7/Terrain Config")]
    public class TerrainConfig : ScriptableObject
    {
        [Header("Ground Settings")]
        public GroundSettings[] GroundTypes;

        [Header("Surface Settings")]
        public SurfaceSettings[] SurfaceTypes;

        // 移动力消耗查表
        public int GetMovementCost(Tile tile)
        {
            int baseCost = GetGroundCost(tile.Ground);
            int surfaceCost = GetSurfaceCost(tile.Surface);
            return baseCost + surfaceCost;
        }

        public bool IsPassable(Tile tile)
        {
            return IsGroundPassable(tile.Ground) && IsSurfacePassable(tile.Surface);
        }

        int GetGroundCost(GroundType type)
        {
            int index = (int)type;
            if (index < GroundTypes.Length) return GroundTypes[index].MovementCost;
            return 999;
        }

        int GetSurfaceCost(SurfaceType type)
        {
            int index = (int)type;
            if (index < SurfaceTypes.Length) return SurfaceTypes[index].MovementCostModifier;
            return 0;
        }

        bool IsGroundPassable(GroundType type)
        {
            int index = (int)type;
            if (index < GroundTypes.Length) return GroundTypes[index].Passable;
            return false;
        }

        bool IsSurfacePassable(SurfaceType type)
        {
            int index = (int)type;
            if (index < SurfaceTypes.Length) return SurfaceTypes[index].Passable;
            return true;
        }
    }

    [System.Serializable]
    public struct GroundSettings
    {
        public string Name;
        public int MovementCost;
        public bool Passable;
        public bool BlocksVision;
    }

    [System.Serializable]
    public struct SurfaceSettings
    {
        public string Name;
        public int MovementCostModifier;
        public bool Passable;
        public bool BlocksVision;
        public bool ProvidesConcealment;
    }
}
