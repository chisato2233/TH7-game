#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 建筑导入
    /// </summary>
    public partial class FactionImporter
    {
        void ImportBuildings(string factionPath)
        {
            var buildingsPath = Path.Combine(factionPath, "buildings");
            if (!Directory.Exists(buildingsPath)) return;

            EnsureDirectory($"{OUTPUT_PATH}/buildings");

            foreach (var buildingDir in Directory.GetDirectories(buildingsPath))
            {
                var configPath = Path.Combine(buildingDir, "config.yaml");
                if (!File.Exists(configPath)) continue;

                ImportBuildingConfig(buildingDir, configPath);
            }
        }

        void ImportBuildingConfig(string buildingDir, string configPath)
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var data = deserializer.Deserialize<BuildingYamlData>(yaml);
            if (string.IsNullOrEmpty(data.BuildingId))
            {
                Debug.LogWarning($"[FactionImporter] 建筑配置缺少 buildingId: {configPath}");
                return;
            }

            // 创建或加载现有资产
            var assetPath = $"{OUTPUT_PATH}/buildings/BuildingConfig_{data.BuildingId}.asset";
            var config = AssetDatabase.LoadAssetAtPath<BuildingConfig>(assetPath);
            if (config == null)
            {
                config = CreateInstance<BuildingConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            // 填充数据
            config.Type = ParseEnum<BuildingType>(data.Type);
            config.DisplayName = data.DisplayName ?? data.BuildingId;
            config.Description = data.Description ?? "";

            // 基础建造成本
            if (data.BasicCost != null)
            {
                config.BasicCost = new ResourceBundle(
                    data.BasicCost.Gold,
                    data.BasicCost.Wood,
                    data.BasicCost.Ore,
                    data.BasicCost.Crystal
                );
            }

            // 升级成本
            if (data.UpgradeCost != null)
            {
                config.UpgradeCost = new ResourceBundle(
                    data.UpgradeCost.Gold,
                    data.UpgradeCost.Wood,
                    data.UpgradeCost.Ore,
                    data.UpgradeCost.Crystal
                );
            }

            // 前置建筑
            config.Requirements.Clear();
            if (data.Requirements != null)
            {
                foreach (var req in data.Requirements)
                {
                    config.Requirements.Add(new BuildingRequirement
                    {
                        RequiredBuilding = ParseEnum<BuildingType>(req.Building),
                        RequiredTier = ParseEnum<BuildingTier>(req.Tier, BuildingTier.Basic)
                    });
                }
            }

            // 生产配置
            config.GoldPerDay = data.GoldPerDay;
            config.ProducedUnitId = data.ProducedUnitId ?? "";
            config.WeeklyGrowth = data.WeeklyGrowth;

            // 尝试加载图标 (优先使用 YAML 中指定的文件名)
            var iconFile = !string.IsNullOrEmpty(data.Icon)
                ? data.Icon
                : FindFileWithPattern(buildingDir, "icon_") ?? "icon.png";
            var iconPath = Path.Combine(buildingDir, iconFile);
            if (File.Exists(iconPath))
            {
                var unityPath = iconPath.Replace("\\", "/");
                EnsureSpriteImportSettings(unityPath);
                config.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
            }

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入建筑: {data.BuildingId}");
        }
    }
}
#endif
