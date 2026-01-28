#if UNITY_EDITOR
using System.Collections.Generic;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - YAML 数据类定义
    /// </summary>
    public partial class FactionImporter
    {
        class HeroYamlData
        {
            public string HeroId { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Faction { get; set; }
            public string Class { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int SpellPower { get; set; }
            public int Knowledge { get; set; }
            public List<StartingArmyEntry> StartingArmy { get; set; }
            public string Portrait { get; set; }      // portrait_512x512.png
            public string WorldSprite { get; set; }   // worldsprite_1024x1024.png
            public string Prefab { get; set; }        // Assets/prefabs/heroes/xxx.prefab
        }

        class StartingArmyEntry
        {
            public string Unit { get; set; }  // @UnitConfig_xxx 或 unit_id
            public int Count { get; set; }
        }

        class UnitYamlData
        {
            public string UnitId { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string Faction { get; set; }
            public string Tier { get; set; }
            public int Attack { get; set; }
            public int Defense { get; set; }
            public int MinDamage { get; set; }
            public int MaxDamage { get; set; }
            public int Health { get; set; }
            public int Speed { get; set; }
            public int GoldCost { get; set; }
            public int GrowthPerWeek { get; set; }
            public List<string> Abilities { get; set; }
            public string Icon { get; set; }      // icon_256x256.png
            public string Sprite { get; set; }    // sprite_512x512.png
            public string Prefab { get; set; }    // Assets/prefabs/units/xxx.prefab
        }

        class FactionYamlData
        {
            public string FactionType { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public ResourcesData StartingResources { get; set; }
            public List<string> AvailableHeroes { get; set; }    // @HeroConfig_xxx 或 hero_id
            public List<string> FactionUnits { get; set; }       // @UnitConfig_xxx 或 unit_id
            public List<string> FactionBuildings { get; set; }   // @BuildingConfig_xxx 或 building_id
        }

        class ResourcesData
        {
            public int Gold { get; set; }
            public int Wood { get; set; }
            public int Ore { get; set; }
            public int Crystal { get; set; }
        }

        class BuildingYamlData
        {
            public string BuildingId { get; set; }
            public string Type { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public ResourcesData BasicCost { get; set; }
            public ResourcesData UpgradeCost { get; set; }
            public List<BuildingRequirementData> Requirements { get; set; }
            public int GoldPerDay { get; set; }
            public string ProducedUnitId { get; set; }
            public int WeeklyGrowth { get; set; }
            public string Icon { get; set; }    // icon_512x512.png
        }

        class BuildingRequirementData
        {
            public string Building { get; set; }
            public string Tier { get; set; }
        }
    }
}
#endif
