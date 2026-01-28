#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 数据库更新
    /// </summary>
    public partial class FactionImporter
    {
        void UpdateDatabases()
        {
            UpdateHeroDatabase();
            UpdateUnitDatabase();
            UpdateBuildingDatabase();
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
    }
}
#endif
