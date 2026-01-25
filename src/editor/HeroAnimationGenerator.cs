#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace TH7.Editor
{
    /// <summary>
    /// 英雄动画资源批量生成工具
    /// 自动从 art/design 读取序列帧，生成 AnimationClip 和 AnimatorController
    /// </summary>
    public class HeroAnimationGenerator : EditorWindow
    {
        const string HERO_PREFAB_BASE = "Assets/prefabs/game/world/heros";
        const string ART_DESIGN_BASE = "Assets/art/design";
        const float DEFAULT_FRAME_RATE = 30f;  // 默认帧率

        Vector2 scrollPos;
        List<HeroAnimationInfo> heroInfos = new();
        bool selectAll = true;
        float frameRate = DEFAULT_FRAME_RATE;

        [MenuItem("TH7/Generate Hero Animations")]
        public static void ShowWindow()
        {
            var window = GetWindow<HeroAnimationGenerator>("Hero Animation Generator");
            window.minSize = new Vector2(550, 450);
            window.RefreshHeroList();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("英雄动画资源生成工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "从 art/design/{faction}/heroes/{hero_id}/animations/ 读取序列帧，\n" +
                "自动生成 idle.anim、move.anim 和 heroview.controller 到 prefabs 目录",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // 设置区域
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("帧率 (FPS):", GUILayout.Width(80));
            frameRate = EditorGUILayout.FloatField(frameRate, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

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

        void DrawHeroEntry(HeroAnimationInfo info)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            info.Selected = EditorGUILayout.Toggle(info.Selected, GUILayout.Width(20));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{info.Faction}/{info.HeroId}", EditorStyles.boldLabel);

            // 序列帧信息
            var frameInfo = $"idle: {info.IdleFrameCount}帧, move: {info.MoveFrameCount}帧";
            EditorGUILayout.LabelField(frameInfo, EditorStyles.miniLabel);

            // 状态指示
            var status = GetStatusText(info);
            var statusStyle = new GUIStyle(EditorStyles.miniLabel);
            statusStyle.normal.textColor = info.HasAllAnimations ? Color.green :
                (info.HasSourceFrames ? Color.yellow : Color.red);
            EditorGUILayout.LabelField(status, statusStyle);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        string GetStatusText(HeroAnimationInfo info)
        {
            if (!info.HasSourceFrames)
                return "✗ 缺少序列帧源文件";

            var missing = new List<string>();
            if (!info.HasAnimationFolder) missing.Add("animation/");
            if (!info.HasIdleAnim) missing.Add("idle.anim");
            if (!info.HasMoveAnim) missing.Add("move.anim");
            if (!info.HasController) missing.Add("heroview.controller");

            if (missing.Count == 0)
                return "✓ 完整";
            return $"缺少: {string.Join(", ", missing)}";
        }

        void RefreshHeroList()
        {
            heroInfos.Clear();

            if (!Directory.Exists(HERO_PREFAB_BASE))
            {
                Debug.LogWarning($"[HeroAnimationGenerator] 目录不存在: {HERO_PREFAB_BASE}");
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

                    var animationDir = Path.Combine(heroDir, "animation").Replace("\\", "/");

                    // 查找 art/design 中的序列帧源路径
                    var artAnimDir = $"{ART_DESIGN_BASE}/{factionName}/heroes/{heroId}/animations";
                    var idleFramesDir = $"{artAnimDir}/idle_anim_image";
                    var moveFramesDir = $"{artAnimDir}/move_anim_image";

                    var info = new HeroAnimationInfo
                    {
                        Faction = factionName,
                        HeroId = heroId,
                        HeroPath = heroDir.Replace("\\", "/"),
                        AnimationPath = animationDir,
                        ArtAnimationPath = artAnimDir,
                        IdleFramesPath = idleFramesDir,
                        MoveFramesPath = moveFramesDir,
                        IdleFrameCount = CountPngFiles(idleFramesDir),
                        MoveFrameCount = CountPngFiles(moveFramesDir),
                        HasAnimationFolder = Directory.Exists(animationDir),
                        HasIdleAnim = File.Exists(Path.Combine(animationDir, "idle.anim")),
                        HasMoveAnim = File.Exists(Path.Combine(animationDir, "move.anim")),
                        HasController = File.Exists(Path.Combine(animationDir, "heroview.controller")),
                        Selected = true
                    };
                    heroInfos.Add(info);
                }
            }

            Repaint();
        }

        int CountPngFiles(string directory)
        {
            if (!Directory.Exists(directory)) return 0;
            return Directory.GetFiles(directory, "*.png").Length;
        }

        void GenerateSelected()
        {
            int generated = 0;
            int skipped = 0;

            foreach (var info in heroInfos)
            {
                if (!info.Selected) continue;

                if (!info.HasSourceFrames)
                {
                    Debug.LogWarning($"[HeroAnimationGenerator] 跳过 {info.HeroId}: 缺少序列帧源文件");
                    skipped++;
                    continue;
                }

                if (info.HasAllAnimations)
                {
                    skipped++;
                    continue;
                }

                GenerateAnimationAssets(info);
                generated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshHeroList();

            Debug.Log($"[HeroAnimationGenerator] 完成: 生成 {generated} 个, 跳过 {skipped} 个");
        }

        void GenerateAnimationAssets(HeroAnimationInfo info)
        {
            // 确保 animation 文件夹存在
            if (!Directory.Exists(info.AnimationPath))
            {
                Directory.CreateDirectory(info.AnimationPath);
                AssetDatabase.Refresh();
            }

            // 创建 idle.anim
            AnimationClip idleClip = null;
            var idlePath = $"{info.AnimationPath}/idle.anim";
            if (!info.HasIdleAnim && info.IdleFrameCount > 0)
            {
                idleClip = CreateAnimationClipFromFrames("idle", info.IdleFramesPath, true);
                if (idleClip != null)
                {
                    AssetDatabase.CreateAsset(idleClip, idlePath);
                    Debug.Log($"[HeroAnimationGenerator] 创建: {idlePath} ({info.IdleFrameCount}帧)");
                }
            }
            else
            {
                idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            }

            // 创建 move.anim
            AnimationClip moveClip = null;
            var movePath = $"{info.AnimationPath}/move.anim";
            if (!info.HasMoveAnim && info.MoveFrameCount > 0)
            {
                moveClip = CreateAnimationClipFromFrames("move", info.MoveFramesPath, true);
                if (moveClip != null)
                {
                    AssetDatabase.CreateAsset(moveClip, movePath);
                    Debug.Log($"[HeroAnimationGenerator] 创建: {movePath} ({info.MoveFrameCount}帧)");
                }
            }
            else
            {
                moveClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(movePath);
            }

            // 创建 heroview.controller
            var controllerPath = $"{info.AnimationPath}/heroview.controller";
            if (!info.HasController)
            {
                // 确保有 clip 可用（如果没有序列帧，创建空 clip）
                if (idleClip == null)
                    idleClip = CreateEmptyAnimationClip("idle", true);
                if (moveClip == null)
                    moveClip = CreateEmptyAnimationClip("move", true);

                // CreateAnimatorControllerAtPath 会直接保存到磁盘，不需要额外调用 CreateAsset
                CreateAnimatorController(idleClip, moveClip, controllerPath);
                Debug.Log($"[HeroAnimationGenerator] 创建: {controllerPath}");
            }
        }

        AnimationClip CreateAnimationClipFromFrames(string name, string framesDirectory, bool loop)
        {
            if (!Directory.Exists(framesDirectory))
            {
                Debug.LogWarning($"[HeroAnimationGenerator] 序列帧目录不存在: {framesDirectory}");
                return CreateEmptyAnimationClip(name, loop);
            }

            // 获取所有 PNG 文件并排序
            var frameFiles = Directory.GetFiles(framesDirectory, "*.png")
                .OrderBy(f => f)
                .ToArray();

            if (frameFiles.Length == 0)
            {
                Debug.LogWarning($"[HeroAnimationGenerator] 序列帧目录为空: {framesDirectory}");
                return CreateEmptyAnimationClip(name, loop);
            }

            // 加载所有 Sprite
            var sprites = new List<Sprite>();
            foreach (var file in frameFiles)
            {
                var unityPath = file.Replace("\\", "/");

                // 确保是 Sprite 导入设置
                var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[HeroAnimationGenerator] 无法加载序列帧: {framesDirectory}");
                return CreateEmptyAnimationClip(name, loop);
            }

            // 创建 AnimationClip
            var clip = new AnimationClip();
            clip.name = name;
            clip.frameRate = frameRate;

            // 创建关键帧
            var keyframes = new ObjectReferenceKeyframe[sprites.Count];
            float timePerFrame = 1f / frameRate;

            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = sprites[i]
                };
            }

            // 设置 Sprite 曲线
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            // 设置循环和总时长
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = sprites.Count * timePerFrame;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        AnimationClip CreateEmptyAnimationClip(string name, bool loop)
        {
            var clip = new AnimationClip();
            clip.name = name;
            clip.frameRate = frameRate;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        AnimatorController CreateAnimatorController(AnimationClip idleClip, AnimationClip moveClip, string savePath)
        {
            // 使用 Unity API 创建 AnimatorController 并保存到磁盘
            // 这样可以确保 StateMachine 正确初始化 (Unity 6 兼容)
            var controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);

            // 添加参数
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            // 获取默认创建的 Base Layer (CreateAnimatorControllerAtPath 会自动创建)
            var rootStateMachine = controller.layers[0].stateMachine;

            // 添加 Idle 状态
            var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 50, 0));
            idleState.motion = idleClip;
            rootStateMachine.defaultState = idleState;

            // 添加 Move 状态
            var moveState = rootStateMachine.AddState("Move", new Vector3(300, 150, 0));
            moveState.motion = moveClip;

            // Idle -> Move 转换 (IsMoving = true)
            var toMove = idleState.AddTransition(moveState);
            toMove.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            toMove.hasExitTime = false;
            toMove.duration = 0f;

            // Move -> Idle 转换 (IsMoving = false)
            var toIdle = moveState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;

            // 保存更改
            EditorUtility.SetDirty(controller);

            return controller;
        }

        class HeroAnimationInfo
        {
            public string Faction;
            public string HeroId;
            public string HeroPath;
            public string AnimationPath;
            public string ArtAnimationPath;
            public string IdleFramesPath;
            public string MoveFramesPath;
            public int IdleFrameCount;
            public int MoveFrameCount;
            public bool HasAnimationFolder;
            public bool HasIdleAnim;
            public bool HasMoveAnim;
            public bool HasController;
            public bool Selected;

            public bool HasSourceFrames => IdleFrameCount > 0 || MoveFrameCount > 0;
            public bool HasAllAnimations => HasAnimationFolder && HasIdleAnim && HasMoveAnim && HasController;
        }
    }
}
#endif
