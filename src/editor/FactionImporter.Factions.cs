#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 文明配置导入
    /// </summary>
    public partial class FactionImporter
    {
        void ImportFactionConfig(string factionPath)
        {
            var configPath = Path.Combine(factionPath, "faction.yaml");
            if (!File.Exists(configPath)) return;

            EnsureDirectory($"{OUTPUT_PATH}/factions");

            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var data = deserializer.Deserialize<FactionYamlData>(yaml);
            var factionName = Path.GetFileName(factionPath);

            // 创建或加载现有资产
            var assetPath = $"{OUTPUT_PATH}/factions/FactionConfig_{factionName}.asset";
            var config = AssetDatabase.LoadAssetAtPath<FactionConfig>(assetPath);
            if (config == null)
            {
                config = CreateInstance<FactionConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            // 填充数据
            config.FactionType = ParseEnum<BiomeType>(data.FactionType);
            config.DisplayName = data.DisplayName ?? factionName;
            config.Description = data.Description ?? "";

            // 初始资源
            if (data.StartingResources != null)
            {
                config.StartingResources = new ResourceBundle(
                    data.StartingResources.Gold,
                    data.StartingResources.Wood,
                    data.StartingResources.Ore,
                    data.StartingResources.Crystal
                );
            }

            // 英雄（强引用）
            config.AvailableHeroes.Clear();
            if (data.AvailableHeroes != null)
            {
                foreach (var heroRef in data.AvailableHeroes)
                {
                    var heroConfig = ResolveReference<HeroConfig>(heroRef, "heroes", "HeroConfig");
                    if (heroConfig != null)
                        config.AvailableHeroes.Add(heroConfig);
                }
            }

            // 兵种（强引用）
            config.FactionUnits.Clear();
            if (data.FactionUnits != null)
            {
                foreach (var unitRef in data.FactionUnits)
                {
                    var unitConfig = ResolveReference<UnitConfig>(unitRef, "units", "UnitConfig");
                    if (unitConfig != null)
                        config.FactionUnits.Add(unitConfig);
                }
            }

            // 建筑（强引用）
            config.FactionBuildings.Clear();
            if (data.FactionBuildings != null)
            {
                foreach (var buildingRef in data.FactionBuildings)
                {
                    var buildingConfig = ResolveReference<BuildingConfig>(buildingRef, "buildings", "BuildingConfig");
                    if (buildingConfig != null)
                        config.FactionBuildings.Add(buildingConfig);
                }
            }

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入文明: {factionName}");
        }
    }
}
#endif
