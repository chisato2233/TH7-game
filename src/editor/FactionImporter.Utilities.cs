#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace TH7.Editor
{
    /// <summary>
    /// 文明资产导入工具 - 工具方法
    /// </summary>
    public partial class FactionImporter
    {
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
    }
}
#endif
