#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TH7.Editor
{
    /// <summary>
    /// 英雄 Prefab 变体生成工具
    /// 自动创建 base_hero.prefab 的变体，并关联 AnimatorController
    /// </summary>
    public class HeroPrefabGenerator : EditorWindow
    {
        const string HERO_PREFAB_BASE = "Assets/prefabs/game/world/heros";
        const string BASE_HERO_PATH = "Assets/prefabs/game/world/heros/base_hero.prefab";

        Vector2 scrollPos;
        List<HeroPrefabInfo> heroInfos = new();
        bool selectAll = true;
        GameObject basePrefab;

        [MenuItem("TH7/Generate Hero Prefab Variants")]
        public static void ShowWindow()
        {
            var window = GetWindow<HeroPrefabGenerator>("Hero Prefab Generator");
            window.minSize = new Vector2(550, 450);
            window.LoadBasePrefab();
            window.RefreshHeroList();
        }

        void LoadBasePrefab()
        {
            basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_HERO_PATH);
            if (basePrefab == null)
            {
                Debug.LogError($"[HeroPrefabGenerator] 找不到 base_hero.prefab: {BASE_HERO_PATH}");
            }
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("英雄 Prefab 变体生成工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "自动创建 base_hero.prefab 的变体，\n" +
                "并将 animation/heroview.controller 关联到 heroview 子对象的 Animator",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // 显示 base prefab 状态
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Base Prefab", basePrefab, typeof(GameObject), false);
            EditorGUI.EndDisabledGroup();

            if (basePrefab == null)
            {
                EditorGUILayout.HelpBox("找不到 base_hero.prefab!", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新列表", GUILayout.Width(100)))
                RefreshHeroList();

            EditorGUI.BeginChangeCheck();
            selectAll = EditorGUILayout.ToggleLeft("全选", selectAll, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var info in heroInfos)
                    info.Selected = selectAll;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("生成选中", GUILayout.Width(100)))
                GenerateSelected();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 英雄列表
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var info in heroInfos)
            {
                DrawHeroEntry(info);
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawHeroEntry(HeroPrefabInfo info)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            info.Selected = EditorGUILayout.Toggle(info.Selected, GUILayout.Width(20));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{info.Faction}/{info.HeroId}", EditorStyles.boldLabel);

            // 状态指示
            var statusStyle = new GUIStyle(EditorStyles.miniLabel);

            if (info.HasPrefab && info.HasController)
            {
                statusStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("✓ Prefab 和 Controller 都已存在", statusStyle);
            }
            else if (info.HasPrefab && !info.HasController)
            {
                statusStyle.normal.textColor = Color.yellow;
                EditorGUILayout.LabelField("⚠ Prefab 存在，缺少 Controller", statusStyle);
            }
            else if (!info.HasPrefab && info.HasController)
            {
                statusStyle.normal.textColor = Color.cyan;
                EditorGUILayout.LabelField("→ 可创建 Prefab 并关联 Controller", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("✗ 缺少 Prefab 和 Controller", statusStyle);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        void RefreshHeroList()
        {
            heroInfos.Clear();

            if (!Directory.Exists(HERO_PREFAB_BASE))
            {
                Debug.LogWarning($"[HeroPrefabGenerator] 目录不存在: {HERO_PREFAB_BASE}");
                return;
            }

            // 扫描所有文明文件夹
            foreach (var factionDir in Directory.GetDirectories(HERO_PREFAB_BASE))
            {
                var factionName = Path.GetFileName(factionDir);

                // 跳过非文明文件夹
                if (factionName.EndsWith(".prefab") || factionName.EndsWith(".meta"))
                    continue;

                // 扫描该文明下的所有英雄文件夹
                foreach (var heroDir in Directory.GetDirectories(factionDir))
                {
                    var heroId = Path.GetFileName(heroDir);
                    if (heroId.EndsWith(".meta"))
                        continue;

                    var heroPath = heroDir.Replace("\\", "/");
                    var prefabPath = $"{heroPath}/{heroId}.prefab";
                    var controllerPath = $"{heroPath}/animation/heroview.controller";

                    var info = new HeroPrefabInfo
                    {
                        Faction = factionName,
                        HeroId = heroId,
                        HeroPath = heroPath,
                        PrefabPath = prefabPath,
                        ControllerPath = controllerPath,
                        HasPrefab = File.Exists(prefabPath),
                        HasController = File.Exists(controllerPath),
                        Selected = true
                    };
                    heroInfos.Add(info);
                }
            }

            Repaint();
        }

        void GenerateSelected()
        {
            int created = 0;
            int updated = 0;
            int skipped = 0;

            foreach (var info in heroInfos)
            {
                if (!info.Selected) continue;

                if (info.HasPrefab)
                {
                    // Prefab 已存在，尝试更新 Controller
                    if (info.HasController)
                    {
                        if (UpdatePrefabController(info))
                            updated++;
                        else
                            skipped++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    // 创建新的 Prefab 变体
                    if (CreatePrefabVariant(info))
                        created++;
                    else
                        skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshHeroList();

            Debug.Log($"[HeroPrefabGenerator] 完成: 创建 {created} 个, 更新 {updated} 个, 跳过 {skipped} 个");
        }

        bool CreatePrefabVariant(HeroPrefabInfo info)
        {
            if (basePrefab == null)
            {
                Debug.LogError("[HeroPrefabGenerator] base_hero.prefab 未加载");
                return false;
            }

            // 实例化 base prefab
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = info.HeroId;

            // 如果有 Controller，设置到 heroview 的 Animator
            if (info.HasController)
            {
                var heroview = instance.transform.Find("heroview");
                if (heroview != null)
                {
                    var animator = heroview.GetComponent<Animator>();
                    if (animator != null)
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(info.ControllerPath);
                        if (controller != null)
                        {
                            animator.runtimeAnimatorController = controller;
                            Debug.Log($"[HeroPrefabGenerator] 关联 Controller: {info.ControllerPath}");
                        }
                    }
                }
            }

            // 保存为变体
            var variant = PrefabUtility.SaveAsPrefabAsset(instance, info.PrefabPath);
            DestroyImmediate(instance);

            if (variant != null)
            {
                Debug.Log($"[HeroPrefabGenerator] 创建 Prefab 变体: {info.PrefabPath}");
                return true;
            }

            return false;
        }

        bool UpdatePrefabController(HeroPrefabInfo info)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(info.PrefabPath);
            if (prefab == null) return false;

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(info.ControllerPath);
            if (controller == null) return false;

            // 打开 Prefab 进行编辑
            var prefabPath = info.PrefabPath;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);

            var heroview = root.transform.Find("heroview");
            if (heroview != null)
            {
                var animator = heroview.GetComponent<Animator>();
                if (animator != null)
                {
                    // 检查是否需要更新
                    if (animator.runtimeAnimatorController != controller)
                    {
                        animator.runtimeAnimatorController = controller;
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                        Debug.Log($"[HeroPrefabGenerator] 更新 Controller: {info.PrefabPath}");
                    }
                }
            }

            PrefabUtility.UnloadPrefabContents(root);
            return true;
        }

        class HeroPrefabInfo
        {
            public string Faction;
            public string HeroId;
            public string HeroPath;
            public string PrefabPath;
            public string ControllerPath;
            public bool HasPrefab;
            public bool HasController;
            public bool Selected;
        }
    }
}
#endif
