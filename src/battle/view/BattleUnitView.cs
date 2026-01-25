using UnityEngine;
using TMPro;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗单位视图
    /// 负责单位的视觉表现
    /// </summary>
    public class BattleUnitView : GameBehaviour
    {
        [Header("References")]
        [SerializeField] BattleUnit unit;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] SpriteRenderer selectionHighlight;

        [Header("Health Bar")]
        [SerializeField] Transform healthBarRoot;
        [SerializeField] SpriteRenderer healthBarFill;
        [SerializeField] TextMeshPro countText;

        [Header("Effects")]
        [SerializeField] GameObject defendingEffect;
        [SerializeField] GameObject waitingEffect;

        [Header("Colors")]
        [SerializeField] Color attackerColor = Color.blue;
        [SerializeField] Color defenderColor = Color.red;
        [SerializeField] Color selectedColor = Color.yellow;

        protected override void Start()
        {
            base.Start();

            if (unit == null)
            {
                unit = GetComponent<BattleUnit>();
            }

            if (unit != null)
            {
                BindToUnit();
            }
        }

        /// <summary>
        /// 初始化视图
        /// </summary>
        public void Initialize(BattleUnit battleUnit)
        {
            unit = battleUnit;
            BindToUnit();
        }

        void BindToUnit()
        {
            if (unit == null) return;

            // 设置阵营颜色
            if (spriteRenderer != null)
            {
                var baseColor = unit.Side == BattleSide.Attacker ? attackerColor : defenderColor;
                spriteRenderer.color = baseColor;
            }

            // 绑定数量显示
            ListenImmediate(unit.Count, UpdateCount);

            // 绑定血量显示
            ListenImmediate(unit.CurrentHealth, _ => UpdateHealthBar());
            ListenImmediate(unit.Count, _ => UpdateHealthBar());

            // 绑定状态效果
            ListenImmediate(unit.IsDefending, UpdateDefendingEffect);
            ListenImmediate(unit.IsWaiting, UpdateWaitingEffect);

            // 隐藏选中高亮
            ShowSelectionHighlight(false);
        }

        void UpdateCount(int count)
        {
            if (countText != null)
            {
                countText.text = count.ToString();
                countText.gameObject.SetActive(count > 0);
            }
        }

        void UpdateHealthBar()
        {
            if (healthBarFill == null || unit?.Config == null) return;

            float maxHp = unit.Config.Health;
            float currentHp = unit.CurrentHealth.Value;
            float fillAmount = currentHp / maxHp;

            // 更新填充
            healthBarFill.transform.localScale = new Vector3(fillAmount, 1f, 1f);

            // 根据血量设置颜色
            if (fillAmount > 0.5f)
            {
                healthBarFill.color = Color.green;
            }
            else if (fillAmount > 0.25f)
            {
                healthBarFill.color = Color.yellow;
            }
            else
            {
                healthBarFill.color = Color.red;
            }

            // 隐藏死亡单位的血条
            if (healthBarRoot != null)
            {
                healthBarRoot.gameObject.SetActive(unit.IsAlive);
            }
        }

        void UpdateDefendingEffect(bool isDefending)
        {
            if (defendingEffect != null)
            {
                defendingEffect.SetActive(isDefending);
            }
        }

        void UpdateWaitingEffect(bool isWaiting)
        {
            if (waitingEffect != null)
            {
                waitingEffect.SetActive(isWaiting);
            }
        }

        /// <summary>
        /// 显示/隐藏选中高亮
        /// </summary>
        public void ShowSelectionHighlight(bool show)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(show);
                if (show)
                {
                    selectionHighlight.color = selectedColor;
                }
            }
        }

        /// <summary>
        /// 播放伤害数字
        /// </summary>
        public void ShowDamagePopup(int damage, bool isCritical = false)
        {
            // TODO: 实例化伤害数字预制体
            Debug.Log($"[BattleUnitView] Damage: {damage}{(isCritical ? " CRITICAL!" : "")}");
        }

        /// <summary>
        /// 播放治疗数字
        /// </summary>
        public void ShowHealPopup(int amount)
        {
            // TODO: 实例化治疗数字预制体
            Debug.Log($"[BattleUnitView] Heal: +{amount}");
        }
    }
}
