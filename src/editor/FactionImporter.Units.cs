#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 兵种导入
    /// </summary>
    public partial class FactionImporter
    {
        void ImportUnits(string factionPath)
        {
            var unitsPath = Path.Combine(factionPath, "units");
            if (!Directory.Exists(unitsPath)) return;

            EnsureDirectory($"{OUTPUT_PATH}/units");

            foreach (var unitDir in Directory.GetDirectories(unitsPath))
            {
                var configPath = Path.Combine(unitDir, "config.yaml");
                if (!File.Exists(configPath)) continue;

                ImportUnitConfig(unitDir, configPath);
            }
        }

        void ImportUnitConfig(string unitDir, string configPath)
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var data = deserializer.Deserialize<UnitYamlData>(yaml);
            if (string.IsNullOrEmpty(data.UnitId))
            {
                Debug.LogWarning($"[FactionImporter] 兵种配置缺少 unitId: {configPath}");
                return;
            }

            // 创建或加载现有资产
            var assetPath = $"{OUTPUT_PATH}/units/UnitConfig_{data.UnitId}.asset";
            var config = AssetDatabase.LoadAssetAtPath<UnitConfig>(assetPath);
            if (config == null)
            {
                config = CreateInstance<UnitConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            // 填充数据
            config.UnitId = data.UnitId;
            config.DisplayName = data.DisplayName ?? data.UnitId;
            config.Description = data.Description ?? "";
            config.Faction = ParseEnum<BiomeType>(data.Faction);
            config.Tier = ParseEnum<UnitTier>(data.Tier, UnitTier.Tier1);
            config.Attack = data.Attack;
            config.Defense = data.Defense;
            config.MinDamage = data.MinDamage;
            config.MaxDamage = data.MaxDamage;
            config.Health = data.Health;
            config.Speed = data.Speed;
            config.WeeklyGrowth = data.GrowthPerWeek > 0 ? data.GrowthPerWeek : 7;

            // 招募成本
            config.RecruitCost = new ResourceBundle(data.GoldCost, 0, 0, 0);

            // 能力
            if (data.Abilities != null)
                config.AbilityIds = data.Abilities.ToArray();

            // 尝试加载图标 (优先使用 YAML 中指定的文件名)
            var iconFile = !string.IsNullOrEmpty(data.Icon)
                ? data.Icon
                : FindFileWithPattern(unitDir, "icon_") ?? "icon.png";
            var iconPath = Path.Combine(unitDir, iconFile);
            if (File.Exists(iconPath))
            {
                var unityPath = iconPath.Replace("\\", "/");
                EnsureSpriteImportSettings(unityPath);
                config.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
            }

            // 加载 Prefab
            // 优先级: 1. yaml中指定的路径 2. 默认路径 3. base_unit.prefab
            const string UNIT_PREFAB_BASE = "Assets/prefabs/game/world/units";
            string unitPrefabPath = null;

            if (!string.IsNullOrEmpty(data.Prefab))
            {
                unitPrefabPath = data.Prefab;
            }
            else
            {
                var factionLower = data.Faction?.ToLower() ?? "neutral";
                var defaultPath = $"{UNIT_PREFAB_BASE}/{factionLower}/{data.UnitId}/{data.UnitId}.prefab";
                if (File.Exists(defaultPath))
                {
                    unitPrefabPath = defaultPath;
                }
                else
                {
                    unitPrefabPath = $"{UNIT_PREFAB_BASE}/base_unit.prefab";
                }
            }

            config.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(unitPrefabPath);
            if (config.Prefab == null)
                Debug.LogWarning($"[FactionImporter] 找不到 Prefab: {unitPrefabPath}");

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入兵种: {data.UnitId}");
        }
    }
}
#endif
