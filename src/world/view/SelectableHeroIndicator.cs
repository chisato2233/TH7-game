using System.Collections.Generic;
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 可选择英雄指示器
    /// 高亮显示当前回合可行动的英雄
    /// </summary>
    public class SelectableHeroIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [SerializeField] Color indicatorColor = new(0.3f, 0.8f, 1f, 0.6f);
        [SerializeField] float indicatorSize = 0.8f;
        [SerializeField] float pulseSpeed = 2f;
        [SerializeField] float pulseAmount = 0.15f;
        [SerializeField] Vector3 offset = new(0, 0.1f, 0);

        readonly Dictionary<Hero, SpriteRenderer> indicators = new();
        Sprite indicatorSprite;

        void Awake()
        {
            indicatorSprite = CreateIndicatorSprite(128);
        }

        /// <summary>
        /// 创建指示器 Sprite（向上箭头或光圈）
        /// </summary>
        Sprite CreateIndicatorSprite(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = resolution / 2f;
            float outerRadius = resolution / 2f - 4;
            float innerRadius = outerRadius * 0.6f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = 0f;

                    // 外圈渐变
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        float t = (dist - innerRadius) / (outerRadius - innerRadius);
                        alpha = Mathf.Clamp01(1f - t * t) * 0.8f;
                    }
                    // 内圈淡化
                    else if (dist < innerRadius)
                    {
                        float t = dist / innerRadius;
                        alpha = Mathf.Clamp01(t * 0.3f);
                    }

                    texture.SetPixel(x, y, alpha > 0 ? new Color(1, 1, 1, alpha) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
        }

        /// <summary>
        /// 更新可选择英雄列表
        /// </summary>
        public void UpdateSelectableHeroes(List<Hero> heroes)
        {
            // 清除不在列表中的指示器
            var toRemove = new List<Hero>();
            foreach (var kvp in indicators)
            {
                if (heroes == null || !heroes.Contains(kvp.Key))
                {
                    if (kvp.Value != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var hero in toRemove)
            {
                indicators.Remove(hero);
            }

            if (heroes == null) return;

            // 为新英雄创建指示器
            foreach (var hero in heroes)
            {
                if (!indicators.ContainsKey(hero))
                {
                    CreateIndicatorForHero(hero);
                }
            }
        }

        void CreateIndicatorForHero(Hero hero)
        {
            var go = new GameObject($"SelectableIndicator_{hero.HeroName}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = indicatorSprite;
            sr.color = indicatorColor;
            sr.sortingOrder = -3;
            sr.transform.localScale = Vector3.one * indicatorSize;
            indicators[hero] = sr;
        }

        void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

            foreach (var kvp in indicators)
            {
                var hero = kvp.Key;
                var indicator = kvp.Value;

                if (hero == null || indicator == null) continue;

                // 跟随英雄位置
                indicator.transform.position = hero.transform.position + offset;
                indicator.transform.localScale = Vector3.one * indicatorSize * pulse;
            }
        }

        /// <summary>
        /// 隐藏所有指示器
        /// </summary>
        public void HideAll()
        {
            foreach (var kvp in indicators)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            indicators.Clear();
        }

        void OnDestroy()
        {
            HideAll();
        }
    }
}
