using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 路径目标类型
    /// </summary>
    public enum PathTargetType
    {
        Move,       // 普通移动
        Attack,     // 攻击敌人
        PickUp,     // 拾取物品
        EnterTown   // 进入城镇
    }

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
        [SerializeField] Color attackColor = new(0.95f, 0.3f, 0.3f, 0.9f);
        [SerializeField] Color pickupColor = new(0.95f, 0.85f, 0.3f, 0.9f);
        [SerializeField] Color townColor = new(0.3f, 0.7f, 0.95f, 0.9f);

        [Header("Destination Marker")]
        [SerializeField] SpriteRenderer destinationMarker;
        [SerializeField] float markerSize = 0.4f;

        MapManager mapManager;
        Hero currentHero; // 当前英雄引用
        readonly List<SpriteRenderer> dotPool = new();
        Sprite dotSprite;
        Sprite targetSprite;
        Sprite attackSprite;
        Sprite pickupSprite;
        Sprite townSprite;

        void Awake()
        {
            // 创建高质量圆点 Sprite
            dotSprite = CreateCircleSprite(64);
            targetSprite = CreateTargetSprite(64);
            attackSprite = CreateAttackSprite(64);
            pickupSprite = CreatePickupSprite(64);
            townSprite = CreateTownSprite(64);

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
        /// 显示路径预览（普通移动）
        /// </summary>
        public void ShowPath(List<Vector3Int> path, bool canReach)
        {
            ShowPath(path, canReach, PathTargetType.Move);
        }

        /// <summary>
        /// 显示路径预览（指定目标类型）
        /// </summary>
        public void ShowPath(List<Vector3Int> path, bool canReach, PathTargetType targetType)
        {
            // 先隐藏所有圆点
            HideDots();

            if (path == null || path.Count == 0 || mapManager == null)
            {
                Hide();
                return;
            }

            // 根据目标类型和是否可达选择颜色
            Color color = GetColorForTargetType(targetType, canReach);

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
                destinationMarker.sprite = GetSpriteForTargetType(targetType);
                destinationMarker.color = color;
                destinationMarker.enabled = true;
            }
        }

        /// <summary>
        /// 根据目标类型获取颜色
        /// </summary>
        Color GetColorForTargetType(PathTargetType targetType, bool canReach)
        {
            if (!canReach) return unreachableColor;

            return targetType switch
            {
                PathTargetType.Attack => attackColor,
                PathTargetType.PickUp => pickupColor,
                PathTargetType.EnterTown => townColor,
                _ => reachableColor
            };
        }

        /// <summary>
        /// 根据目标类型获取 Sprite
        /// </summary>
        Sprite GetSpriteForTargetType(PathTargetType targetType)
        {
            return targetType switch
            {
                PathTargetType.Attack => attackSprite,
                PathTargetType.PickUp => pickupSprite,
                PathTargetType.EnterTown => townSprite,
                _ => targetSprite
            };
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

        /// <summary>
        /// 创建攻击图标 Sprite（剑形）
        /// </summary>
        Sprite CreateAttackSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;

            // 清空
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                    texture.SetPixel(x, y, Color.clear);

            // 画两把交叉的剑
            float swordLength = resolution * 0.35f;
            float swordWidth = resolution * 0.08f;

            // 剑1：左上到右下 \
            DrawLine(texture, center - swordLength, center - swordLength,
                     center + swordLength, center + swordLength, swordWidth);

            // 剑2：右上到左下 /
            DrawLine(texture, center + swordLength, center - swordLength,
                     center - swordLength, center + swordLength, swordWidth);

            // 中心圆点
            float dotRadius = resolution * 0.12f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= dotRadius)
                    {
                        float alpha = Mathf.Clamp01((dotRadius - dist) * 2);
                        var existing = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(existing.a, alpha)));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// 创建拾取图标 Sprite（钻石/菱形）
        /// </summary>
        Sprite CreatePickupSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float size = resolution * 0.35f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // 菱形距离
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);
                    float diamondDist = dx + dy;

                    // 外环
                    float outerDist = size - diamondDist;
                    float innerDist = diamondDist - (size - 4);

                    if (outerDist >= -1 && innerDist >= -1)
                    {
                        float alpha = Mathf.Clamp01(Mathf.Min(outerDist + 1, innerDist + 1));
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            // 中心点
            float dotRadius = resolution * 0.1f;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= dotRadius)
                    {
                        float alpha = Mathf.Clamp01((dotRadius - dist) * 2 + 0.5f);
                        var existing = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(existing.a, alpha)));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// 创建城镇图标 Sprite（城门/拱门形状）
        /// </summary>
        Sprite CreateTownSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }

            // 城墙（长方形，底部有缺口作为城门）
            float wallWidth = resolution * 0.7f;
            float wallHeight = resolution * 0.6f;
            float gateWidth = resolution * 0.25f;
            float gateHeight = resolution * 0.35f;
            float wallThickness = 3f;

            float left = center - wallWidth / 2;
            float right = center + wallWidth / 2;
            float bottom = center - wallHeight / 2;
            float top = center + wallHeight / 2;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // 检查是否在城墙边框上
                    bool onWall = false;

                    // 左墙
                    if (x >= left - wallThickness && x <= left + wallThickness &&
                        y >= bottom && y <= top)
                        onWall = true;

                    // 右墙
                    if (x >= right - wallThickness && x <= right + wallThickness &&
                        y >= bottom && y <= top)
                        onWall = true;

                    // 顶墙
                    if (y >= top - wallThickness && y <= top + wallThickness &&
                        x >= left && x <= right)
                        onWall = true;

                    // 底墙（城门两侧）
                    if (y >= bottom - wallThickness && y <= bottom + wallThickness &&
                        x >= left && x <= right)
                    {
                        // 城门缺口
                        if (x < center - gateWidth / 2 || x > center + gateWidth / 2)
                            onWall = true;
                    }

                    // 城门拱顶
                    float gateTop = bottom + gateHeight;
                    if (y >= gateTop - wallThickness && y <= gateTop + wallThickness &&
                        x >= center - gateWidth / 2 && x <= center + gateWidth / 2)
                        onWall = true;

                    if (onWall)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// 画线辅助方法
        /// </summary>
        void DrawLine(Texture2D texture, float x1, float y1, float x2, float y2, float width)
        {
            int resolution = texture.width;
            Vector2 start = new Vector2(x1, y1);
            Vector2 end = new Vector2(x2, y2);
            Vector2 dir = (end - start).normalized;
            float length = Vector2.Distance(start, end);

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    Vector2 toP = p - start;

                    // 投影到线段上
                    float t = Mathf.Clamp01(Vector2.Dot(toP, dir) / length);
                    Vector2 closest = start + dir * (t * length);
                    float dist = Vector2.Distance(p, closest);

                    if (dist <= width)
                    {
                        float alpha = Mathf.Clamp01((width - dist) * 1.5f);
                        var existing = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(existing.a, alpha)));
                    }
                }
            }
        }
    }
}
