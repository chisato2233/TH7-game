using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 路径预览显示 - 使用圆点标记路径
    /// </summary>
    public class PathPreview : MonoBehaviour
    {
        [Header("Path Dots")]
        [SerializeField] float dotSize = 0.12f;
        [SerializeField] float dotSpacing = 0.2f; // 圆点间距
        [SerializeField] int maxDots = 150;

        [Header("Colors")]
        [SerializeField] Color reachableColor = new(0.4f, 0.95f, 0.5f, 0.9f);
        [SerializeField] Color unreachableColor = new(0.95f, 0.4f, 0.35f, 0.9f);

        [Header("Destination Marker")]
        [SerializeField] SpriteRenderer destinationMarker;
        [SerializeField] float markerSize = 0.4f;

        MapManager mapManager;
        Hero currentHero; // 当前英雄引用
        readonly List<SpriteRenderer> dotPool = new();
        Sprite dotSprite;
        Sprite targetSprite;

        void Awake()
        {
            // 创建高质量圆点 Sprite
            dotSprite = CreateCircleSprite(64);
            targetSprite = CreateTargetSprite(64);

            // 创建目标点标记
            if (destinationMarker == null)
            {
                var markerGo = new GameObject("DestinationMarker");
                markerGo.transform.SetParent(transform);
                destinationMarker = markerGo.AddComponent<SpriteRenderer>();
                destinationMarker.sortingOrder = 12;
                destinationMarker.sprite = targetSprite;
            }
            destinationMarker.transform.localScale = Vector3.one * markerSize;

            Hide();
        }

        /// <summary>
        /// 设置当前英雄（用于路径起点）
        /// </summary>
        public void SetHero(Hero hero)
        {
            currentHero = hero;
        }

        /// <summary>
        /// 创建圆形 Sprite
        /// </summary>
        Sprite CreateCircleSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float radius = resolution / 2f - 1;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist <= radius)
                    {
                        // 边缘柔化
                        float alpha = Mathf.Clamp01((radius - dist) * 2);
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// 创建目标点 Sprite（圆环 + 中心点）
        /// </summary>
        Sprite CreateTargetSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float outerRadius = resolution / 2f - 2;
            float ringWidth = 2.5f;
            float innerRadius = outerRadius - ringWidth;
            float centerDotRadius = resolution * 0.15f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = 0f;

                    // 外圈圆环
                    float outerDist = outerRadius - dist;
                    float innerDist = dist - innerRadius;
                    if (outerDist >= -1 && innerDist >= -1)
                    {
                        float outerAlpha = Mathf.Clamp01(outerDist + 1);
                        float innerAlpha = Mathf.Clamp01(innerDist + 1);
                        alpha = Mathf.Max(alpha, Mathf.Min(outerAlpha, innerAlpha));
                    }

                    // 中心圆点
                    if (dist <= centerDotRadius)
                    {
                        float centerAlpha = Mathf.Clamp01((centerDotRadius - dist) * 0.8f + 0.6f);
                        alpha = Mathf.Max(alpha, centerAlpha);
                    }

                    texture.SetPixel(x, y, alpha > 0 ? new Color(1, 1, 1, alpha) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        public void Initialize(MapManager map)
        {
            mapManager = map;
        }

        /// <summary>
        /// 显示路径预览
        /// </summary>
        public void ShowPath(List<Vector3Int> path, bool canReach)
        {
            // 先隐藏所有圆点
            HideDots();

            if (path == null || path.Count == 0 || mapManager == null)
            {
                Hide();
                return;
            }

            Color color = canReach ? reachableColor : unreachableColor;

            // 转换路径点为世界坐标，从英雄当前位置开始
            var worldPoints = new List<Vector3>();

            // 添加英雄当前位置作为起点
            if (currentHero != null)
            {
                worldPoints.Add(currentHero.transform.position);
            }

            // 添加路径点
            foreach (var cell in path)
            {
                worldPoints.Add(mapManager.CellToWorld(cell));
            }

            // 沿路径放置圆点
            PlaceDotsAlongPath(worldPoints, color);

            // 显示目标点标记
            if (destinationMarker != null)
            {
                Vector3 destPos = mapManager.CellToWorld(path[^1]);
                destinationMarker.transform.position = destPos;
                destinationMarker.color = color;
                destinationMarker.enabled = true;
            }
        }

        /// <summary>
        /// 沿路径放置圆点
        /// </summary>
        void PlaceDotsAlongPath(List<Vector3> points, Color color)
        {
            if (points.Count < 2) return;

            int dotIndex = 0;
            float accumulatedDistance = 0f;

            for (int i = 0; i < points.Count - 1 && dotIndex < maxDots; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 direction = (end - start).normalized;
                float segmentLength = Vector3.Distance(start, end);
                float traveled = 0f;

                // 第一个点从起点开始
                if (i == 0)
                {
                    PlaceDot(dotIndex++, start, color);
                    accumulatedDistance = 0f;
                }

                while (traveled < segmentLength && dotIndex < maxDots)
                {
                    float nextDotDistance = dotSpacing - accumulatedDistance;

                    if (traveled + nextDotDistance <= segmentLength)
                    {
                        traveled += nextDotDistance;
                        Vector3 dotPos = start + direction * traveled;
                        PlaceDot(dotIndex++, dotPos, color);
                        accumulatedDistance = 0f;
                    }
                    else
                    {
                        accumulatedDistance += segmentLength - traveled;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 放置单个圆点
        /// </summary>
        void PlaceDot(int index, Vector3 position, Color color)
        {
            SpriteRenderer dot;

            if (index < dotPool.Count)
            {
                dot = dotPool[index];
            }
            else
            {
                var dotGo = new GameObject($"PathDot_{index}");
                dotGo.transform.SetParent(transform);
                dot = dotGo.AddComponent<SpriteRenderer>();
                dot.sprite = dotSprite;
                dot.sortingOrder = 10;
                dotPool.Add(dot);
            }

            dot.transform.position = position;
            dot.transform.localScale = Vector3.one * dotSize;
            dot.color = color;
            dot.enabled = true;
        }

        /// <summary>
        /// 隐藏所有圆点
        /// </summary>
        void HideDots()
        {
            foreach (var dot in dotPool)
            {
                if (dot != null)
                    dot.enabled = false;
            }
        }

        /// <summary>
        /// 隐藏路径预览
        /// </summary>
        public void Hide()
        {
            HideDots();

            if (destinationMarker != null)
                destinationMarker.enabled = false;
        }
    }
}
