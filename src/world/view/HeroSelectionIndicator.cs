using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 英雄选择指示器 - 高亮英雄所在 tile + 小脉冲圆环
    /// </summary>
    public class HeroSelectionIndicator : MonoBehaviour
    {
        [Header("Tile Highlight")]
        [SerializeField] SpriteRenderer tileHighlight;
        [SerializeField] Color tileHighlightColor = new(1f, 0.95f, 0.5f, 0.25f);
        [SerializeField] float tileSize = 1.2f; // Isometric tile 大小

        [Header("Selection Ring")]
        [SerializeField] SpriteRenderer selectionRing;
        [SerializeField] Sprite ringSprite;
        [SerializeField] Color ringColor = new(1f, 0.85f, 0.1f, 0.8f);
        [SerializeField] float ringSize = 1.04f; // 小圆环
        [SerializeField] float pulseSpeed = 3f;
        [SerializeField] float pulseAmount = 0.08f;
        [SerializeField] Vector3 ringOffset = new(0, 0.05f, 0); // 脚下位置

        Hero currentHero;
        MapManager mapManager;

        void Awake()
        {
            mapManager = FindFirstObjectByType<MapManager>();

            // 创建 Tile 高亮
            if (tileHighlight == null)
            {
                var highlightGo = new GameObject("TileHighlight");
                highlightGo.transform.SetParent(transform);
                highlightGo.transform.localPosition = Vector3.zero;
                tileHighlight = highlightGo.AddComponent<SpriteRenderer>();
                tileHighlight.sortingOrder = -2;
                tileHighlight.sprite = CreateIsometricTileSprite(1024);
            }
            tileHighlight.transform.localScale = Vector3.one * tileSize;

            // 创建选择圆环
            if (selectionRing == null)
            {
                var ringGo = new GameObject("SelectionRing");
                ringGo.transform.SetParent(transform);
                ringGo.transform.localPosition = Vector3.zero;
                selectionRing = ringGo.AddComponent<SpriteRenderer>();
                selectionRing.sortingOrder = -1;
            }

            if (ringSprite != null)
            {
                selectionRing.sprite = ringSprite;
            }
            else
            {
                // 使用更高分辨率生成更平滑的圆环
                selectionRing.sprite = CreateRingSprite(1024);
            }
            selectionRing.transform.localScale = Vector3.one * ringSize;

            Hide();
        }

        /// <summary>
        /// 创建 Isometric 菱形 tile Sprite
        /// </summary>
        Sprite CreateIsometricTileSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float halfWidth = resolution / 2f - 2;
            float halfHeight = halfWidth * 0.5f; // Isometric 比例 2:1

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = Mathf.Abs(x - center) / halfWidth;
                    float dy = Mathf.Abs(y - center) / halfHeight;

                    if (dx + dy <= 1f)
                    {
                        // 边缘柔化
                        float edge = 1f - (dx + dy);
                        float alpha = Mathf.Clamp01(edge * 4f);
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
        /// 创建平滑细圆环 Sprite
        /// </summary>
        Sprite CreateRingSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float outerRadius = resolution / 2f - 2;
            float ringWidth = 1/20.0f * resolution; // 圆环宽度（像素）
            float innerRadius = outerRadius - ringWidth;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    // 使用平滑的距离场计算 alpha
                    float outerDist = outerRadius - dist;
                    float innerDist = dist - innerRadius;

                    if (outerDist >= -1 && innerDist >= -1)
                    {
                        // 平滑抗锯齿
                        float outerAlpha = Mathf.Clamp01(outerDist + 1);
                        float innerAlpha = Mathf.Clamp01(innerDist + 1);
                        float alpha = Mathf.Min(outerAlpha, innerAlpha);
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

        void Update()
        {
            if (currentHero == null) return;

            Vector3 heroPos = currentHero.transform.position;

            // Tile 高亮跟随（tile 中心）
            if (tileHighlight != null && tileHighlight.enabled)
            {
                tileHighlight.transform.position = heroPos;
            }

            // 圆环跟随（带偏移和脉冲）
            if (selectionRing != null && selectionRing.enabled)
            {
                selectionRing.transform.position = heroPos + ringOffset;
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                selectionRing.transform.localScale = Vector3.one * ringSize * pulse;
            }
        }

        /// <summary>
        /// 选中英雄
        /// </summary>
        public void Select(Hero hero)
        {
            currentHero = hero;

            if (hero != null)
            {
                Vector3 heroPos = hero.transform.position;

                // 显示 tile 高亮
                if (tileHighlight != null)
                {
                    tileHighlight.transform.position = heroPos;
                    tileHighlight.color = tileHighlightColor;
                    tileHighlight.enabled = true;
                }

                // 显示圆环
                if (selectionRing != null)
                {
                    selectionRing.transform.position = heroPos + ringOffset;
                    selectionRing.color = ringColor;
                    selectionRing.enabled = true;
                }
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void Hide()
        {
            currentHero = null;

            if (tileHighlight != null)
                tileHighlight.enabled = false;

            if (selectionRing != null)
                selectionRing.enabled = false;
        }
    }
}
