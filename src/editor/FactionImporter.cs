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
    /// 文明资产导入工具 - 主窗口
    /// </summary>
    public partial class FactionImporter : EditorWindow
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

        class FactionFolder
        {
            public string Name;
            public string Path;
            public int HeroCount;
            public int UnitCount;
            public int BuildingCount;
        }
    }
}
#endif
