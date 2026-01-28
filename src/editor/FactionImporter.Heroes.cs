#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 英雄导入
    /// </summary>
    public partial class FactionImporter
    {
        void ImportHeroes(string factionPath)
        {
            var heroesPath = Path.Combine(factionPath, "heroes");
            if (!Directory.Exists(heroesPath)) return;

            EnsureDirectory($"{OUTPUT_PATH}/heroes");

            foreach (var heroDir in Directory.GetDirectories(heroesPath))
            {
                var configPath = Path.Combine(heroDir, "config.yaml");
                if (!File.Exists(configPath)) continue;

                ImportHeroConfig(heroDir, configPath);
            }
        }

        void ImportHeroConfig(string heroDir, string configPath)
        {
            var yaml = File.ReadAllText(configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var data = deserializer.Deserialize<HeroYamlData>(yaml);
            if (string.IsNullOrEmpty(data.HeroId))
            {
                Debug.LogWarning($"[FactionImporter] 英雄配置缺少 heroId: {configPath}");
                return;
            }

            // 创建或加载现有资产
            var assetPath = $"{OUTPUT_PATH}/heroes/HeroConfig_{data.HeroId}.asset";
            var config = AssetDatabase.LoadAssetAtPath<HeroConfig>(assetPath);
            if (config == null)
            {
                config = CreateInstance<HeroConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            // 填充数据
            config.HeroId = data.HeroId;
            config.DisplayName = data.DisplayName ?? data.HeroId;
            config.Description = data.Description ?? "";
            config.Faction = ParseEnum<BiomeType>(data.Faction);
            config.Class = ParseEnum<HeroClass>(data.Class);
            config.Attack = data.Attack;
            config.Defense = data.Defense;
            config.SpellPower = data.SpellPower;
            config.Knowledge = data.Knowledge;

            // 初始军队（强引用）
            if (data.StartingArmy != null)
            {
                config.StartingArmy = new StartingUnit[data.StartingArmy.Count];
                for (int i = 0; i < data.StartingArmy.Count; i++)
                {
                    var unitRef = data.StartingArmy[i].Unit;
                    var unitConfig = ResolveReference<UnitConfig>(unitRef, "units", "UnitConfig");
                    config.StartingArmy[i] = new StartingUnit
                    {
                        Unit = unitConfig,
                        Count = data.StartingArmy[i].Count
                    };
                }
            }

            // 尝试加载头像 (优先使用 YAML 中指定的文件名，否则搜索 portrait_*.png)
            var portraitFile = !string.IsNullOrEmpty(data.Portrait)
                ? data.Portrait
                : FindFileWithPattern(heroDir, "portrait_");
            if (!string.IsNullOrEmpty(portraitFile))
            {
                var portraitPath = Path.Combine(heroDir, portraitFile);
                if (File.Exists(portraitPath))
                {
                    var unityPath = portraitPath.Replace("\\", "/");
                    EnsureSpriteImportSettings(unityPath);
                    config.Portrait = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
                }
            }

            // 尝试加载世界Sprite (优先使用 YAML 中指定的文件名，否则搜索 worldsprite_*.png)
            var worldSpriteFile = !string.IsNullOrEmpty(data.WorldSprite)
                ? data.WorldSprite
                : FindFileWithPattern(heroDir, "worldsprite_");
            if (!string.IsNullOrEmpty(worldSpriteFile))
            {
                var worldSpritePath = Path.Combine(heroDir, worldSpriteFile);
                if (File.Exists(worldSpritePath))
                {
                    var unityPath = worldSpritePath.Replace("\\", "/");
                    EnsureSpriteImportSettings(unityPath);
                    config.MapIcon = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
                }
            }

            // 加载 Prefab
            // 优先级: 1. yaml中指定的路径 2. 默认路径 3. base_hero.prefab
            const string HERO_PREFAB_BASE = "Assets/prefabs/game/world/heros";
            string prefabPath = null;

            if (!string.IsNullOrEmpty(data.Prefab))
            {
                // 使用 yaml 中指定的路径
                prefabPath = data.Prefab;
            }
            else
            {
                // 尝试默认路径: {base}/{faction}/{hero_id}/{hero_id}.prefab
                var factionLower = data.Faction?.ToLower() ?? "neutral";
                var defaultPath = $"{HERO_PREFAB_BASE}/{factionLower}/{data.HeroId}/{data.HeroId}.prefab";
                if (File.Exists(defaultPath))
                {
                    prefabPath = defaultPath;
                }
                else
                {
                    // 使用 base_hero.prefab
                    prefabPath = $"{HERO_PREFAB_BASE}/base_hero.prefab";
                }
            }

            config.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (config.Prefab == null)
                Debug.LogWarning($"[FactionImporter] 找不到 Prefab: {prefabPath}");

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入英雄: {data.HeroId}");
        }
    }
}
#endif
