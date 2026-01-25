#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具
    /// </summary>
    public class FactionImporter : EditorWindow
    {
        const string DESIGN_PATH = "Assets/art/design";
        const string OUTPUT_PATH = "Assets/data";

        Vector2 scrollPos;
        List<FactionFolder> factionFolders = new();

        [MenuItem("TH7/Import Faction Assets")]
        public static void ShowWindow()
        {
            var window = GetWindow<FactionImporter>("Faction Importer");
            window.minSize = new Vector2(400, 300);
            window.RefreshFactionList();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("文明资产导入工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"源目录: {DESIGN_PATH}\n输出目录: {OUTPUT_PATH}",
                MessageType.Info);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新列表", GUILayout.Width(100)))
                RefreshFactionList();

            if (GUILayout.Button("全部导入", GUILayout.Width(100)))
                ImportAll();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 文明列表
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var folder in factionFolders)
            {
                DrawFactionEntry(folder);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawFactionEntry(FactionFolder folder)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(folder.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"英雄: {folder.HeroCount}  兵种: {folder.UnitCount}  建筑: {folder.BuildingCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("导入", GUILayout.Width(60), GUILayout.Height(36)))
            {
                ImportFaction(folder);
            }

            EditorGUILayout.EndHorizontal();
        }

        void RefreshFactionList()
        {
            factionFolders.Clear();

            if (!Directory.Exists(DESIGN_PATH))
            {
                Debug.LogWarning($"[FactionImporter] 源目录不存在: {DESIGN_PATH}");
                return;
            }

            foreach (var dir in Directory.GetDirectories(DESIGN_PATH))
            {
                var factionYaml = Path.Combine(dir, "faction.yaml");
                if (!File.Exists(factionYaml)) continue;

                var folder = new FactionFolder
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    HeroCount = CountSubfolders(Path.Combine(dir, "heroes")),
                    UnitCount = CountSubfolders(Path.Combine(dir, "units")),
                    BuildingCount = CountSubfolders(Path.Combine(dir, "buildings"))
                };
                factionFolders.Add(folder);
            }

            Repaint();
        }

        int CountSubfolders(string path)
        {
            if (!Directory.Exists(path)) return 0;
            return Directory.GetDirectories(path).Length;
        }

        void ImportAll()
        {
            foreach (var folder in factionFolders)
            {
                ImportFaction(folder, updateDatabase: false);
            }
            UpdateDatabases();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FactionImporter] 全部导入完成");
        }

        void ImportFaction(FactionFolder folder, bool updateDatabase = true)
        {
            Debug.Log($"[FactionImporter] 开始导入: {folder.Name}");

            try
            {
                // 按依赖顺序导入: Units -> Heroes -> Buildings -> Faction
                // 1. 先导入兵种（无依赖）
                ImportUnits(folder.Path);
                AssetDatabase.SaveAssets();

                // 2. 再导入英雄（依赖 Units）
                ImportHeroes(folder.Path);
                AssetDatabase.SaveAssets();

                // 3. 导入建筑（依赖 Units）
                ImportBuildings(folder.Path);
                AssetDatabase.SaveAssets();

                // 4. 最后导入文明配置（依赖 Heroes、Units 和 Buildings）
                ImportFactionConfig(folder.Path);

                // 更新 Database
                if (updateDatabase)
                {
                    UpdateDatabases();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                Debug.Log($"[FactionImporter] 完成导入: {folder.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FactionImporter] 导入失败 {folder.Name}: {e.Message}\n{e.StackTrace}");
            }
        }

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

            // TODO: 加载战斗 Sprite (config.Sprite 字段待添加到 UnitConfig)
            // var spriteFile = !string.IsNullOrEmpty(data.Sprite)
            //     ? data.Sprite
            //     : FindFileWithPattern(unitDir, "sprite_");

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

        void UpdateDatabases()
        {
            // 更新 HeroConfigDatabase
            UpdateHeroDatabase();

            // 更新 UnitConfigDatabase
            UpdateUnitDatabase();

            // 更新 BuildingConfigDatabase
            UpdateBuildingDatabase();

            // 更新 FactionConfigDatabase
            UpdateFactionDatabase();
        }

        void UpdateHeroDatabase()
        {
            EnsureDirectory($"{OUTPUT_PATH}/database");
            var dbPath = $"{OUTPUT_PATH}/database/HeroConfigDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<HeroConfigDatabase>(dbPath);
            if (db == null)
            {
                db = CreateInstance<HeroConfigDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            var heroes = new List<HeroConfig>();
            var guids = AssetDatabase.FindAssets("t:HeroConfig", new[] { $"{OUTPUT_PATH}/heroes" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<HeroConfig>(path);
                if (config != null)
                    heroes.Add(config);
            }

            db.Heroes = heroes;
            EditorUtility.SetDirty(db);
            Debug.Log($"[FactionImporter] 更新 HeroConfigDatabase: {heroes.Count} 个英雄");
        }

        void UpdateUnitDatabase()
        {
            EnsureDirectory($"{OUTPUT_PATH}/database");
            var dbPath = $"{OUTPUT_PATH}/database/UnitConfigDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<UnitConfigDatabase>(dbPath);
            if (db == null)
            {
                db = CreateInstance<UnitConfigDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            var units = new List<UnitConfig>();
            var guids = AssetDatabase.FindAssets("t:UnitConfig", new[] { $"{OUTPUT_PATH}/units" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<UnitConfig>(path);
                if (config != null)
                    units.Add(config);
            }

            db.Units = units;
            EditorUtility.SetDirty(db);
            Debug.Log($"[FactionImporter] 更新 UnitConfigDatabase: {units.Count} 个兵种");
        }

        void UpdateBuildingDatabase()
        {
            EnsureDirectory($"{OUTPUT_PATH}/database");
            var dbPath = $"{OUTPUT_PATH}/database/BuildingConfigDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<BuildingConfigDatabase>(dbPath);
            if (db == null)
            {
                db = CreateInstance<BuildingConfigDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            var buildings = new List<BuildingConfig>();
            var guids = AssetDatabase.FindAssets("t:BuildingConfig", new[] { $"{OUTPUT_PATH}/buildings" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<BuildingConfig>(path);
                if (config != null)
                    buildings.Add(config);
            }

            db.Buildings = buildings;
            EditorUtility.SetDirty(db);
            Debug.Log($"[FactionImporter] 更新 BuildingConfigDatabase: {buildings.Count} 个建筑");
        }

        void UpdateFactionDatabase()
        {
            EnsureDirectory($"{OUTPUT_PATH}/database");
            var dbPath = $"{OUTPUT_PATH}/database/FactionConfigDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<FactionConfigDatabase>(dbPath);
            if (db == null)
            {
                db = CreateInstance<FactionConfigDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            var factions = new List<FactionConfig>();
            var guids = AssetDatabase.FindAssets("t:FactionConfig", new[] { $"{OUTPUT_PATH}/factions" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<FactionConfig>(path);
                if (config != null)
                    factions.Add(config);
            }

            db.Factions = factions;
            EditorUtility.SetDirty(db);
            Debug.Log($"[FactionImporter] 更新 FactionConfigDatabase: {factions.Count} 个文明");
        }

        void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path).Replace("\\", "/");
                var folderName = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        void EnsureSpriteImportSettings(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }

        /// <summary>
        /// 在目录中查找匹配前缀的 PNG 文件 (如 portrait_*.png, worldsprite_*.png)
        /// </summary>
        string FindFileWithPattern(string directory, string prefix)
        {
            if (!Directory.Exists(directory)) return null;

            foreach (var file in Directory.GetFiles(directory, $"{prefix}*.png"))
            {
                return Path.GetFileName(file);
            }
            return null;
        }

        T ParseEnum<T>(string value, T defaultValue = default) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            if (Enum.TryParse<T>(value, true, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// 解析强引用语法 @AssetName 或普通 ID
        /// </summary>
        T ResolveReference<T>(string reference, string subfolder, string prefix) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(reference)) return null;

            string assetName;
            if (reference.StartsWith("@"))
            {
                // @HeroConfig_xxx 或 @UnitConfig_xxx 格式
                assetName = reference.Substring(1);
            }
            else
            {
                // 普通 ID，自动添加前缀
                assetName = $"{prefix}_{reference}";
            }

            var assetPath = $"{OUTPUT_PATH}/{subfolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset == null)
            {
                Debug.LogWarning($"[FactionImporter] 找不到引用: {reference} -> {assetPath}");
            }

            return asset;
        }

        #region YAML Data Classes

        class FactionFolder
        {
            public string Name;
            public string Path;
            public int HeroCount;
            public int UnitCount;
            public int BuildingCount;
        }

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

        #endregion
    }
}
#endif
