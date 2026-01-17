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
            EditorGUILayout.LabelField($"英雄: {folder.HeroCount}  兵种: {folder.UnitCount}", EditorStyles.miniLabel);
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
                    UnitCount = CountSubfolders(Path.Combine(dir, "units"))
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
                // 按依赖顺序导入: Units -> Heroes -> Faction
                // 1. 先导入兵种（无依赖）
                ImportUnits(folder.Path);
                AssetDatabase.SaveAssets();

                // 2. 再导入英雄（依赖 Units）
                ImportHeroes(folder.Path);
                AssetDatabase.SaveAssets();

                // 3. 最后导入文明配置（依赖 Heroes 和 Units）
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

            // 尝试加载头像
            var portraitPath = Path.Combine(heroDir, "portrait.png");
            if (File.Exists(portraitPath))
            {
                var unityPath = portraitPath.Replace("\\", "/");
                EnsureSpriteImportSettings(unityPath);
                config.Portrait = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
            }

            // 尝试加载地图图标
            var mapIconPath = Path.Combine(heroDir, "mapicon.png");
            if (File.Exists(mapIconPath))
            {
                var unityPath = mapIconPath.Replace("\\", "/");
                EnsureSpriteImportSettings(unityPath);
                config.MapIcon = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
            }

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

            // 尝试加载图标
            var iconPath = Path.Combine(unitDir, "icon.png");
            if (File.Exists(iconPath))
            {
                var unityPath = iconPath.Replace("\\", "/");
                EnsureSpriteImportSettings(unityPath);
                config.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
            }

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入兵种: {data.UnitId}");
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

            EditorUtility.SetDirty(config);
            Debug.Log($"[FactionImporter] 导入文明: {factionName}");
        }

        void UpdateDatabases()
        {
            // 更新 HeroConfigDatabase
            UpdateHeroDatabase();

            // 更新 UnitConfigDatabase
            UpdateUnitDatabase();

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
        }

        class FactionYamlData
        {
            public string FactionType { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public ResourcesData StartingResources { get; set; }
            public List<string> AvailableHeroes { get; set; }  // @HeroConfig_xxx 或 hero_id
            public List<string> FactionUnits { get; set; }     // @UnitConfig_xxx 或 unit_id
        }

        class ResourcesData
        {
            public int Gold { get; set; }
            public int Wood { get; set; }
            public int Ore { get; set; }
            public int Crystal { get; set; }
        }

        #endregion
    }
}
#endif
