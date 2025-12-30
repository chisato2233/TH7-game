using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 资源消耗显示组件（用于建筑/招募成本显示）
    /// </summary>
    public class ResourceCostUI : UIBehaviour
    {
        [Header("Display")]
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI amountText;

        [Header("Colors")]
        [SerializeField] Color enoughColor = Color.white;
        [SerializeField] Color insufficientColor = Color.red;

        ResourceType resourceType;
        int requiredAmount;
        PlayerResources playerResources;

        /// <summary>
        /// 设置显示的资源消耗
        /// </summary>
        public void Setup(ResourceType type, int amount, PlayerResources resources, Sprite resourceIcon = null)
        {
            resourceType = type;
            requiredAmount = amount;
            playerResources = resources;

            if (icon != null && resourceIcon != null)
                icon.sprite = resourceIcon;

            UpdateDisplay();

            // 监听资源变化
            if (resources != null)
                ListenImmediate(resources.Get(type), _ => UpdateDisplay());
        }

        void UpdateDisplay()
        {
            if (amountText == null) return;

            amountText.text = requiredAmount.ToString();

            // 根据是否足够改变颜色
            bool hasEnough = playerResources == null || playerResources.GetValue(resourceType) >= requiredAmount;
            amountText.color = hasEnough ? enoughColor : insufficientColor;
        }

        /// <summary>
        /// 设置是否有足够资源（手动控制颜色）
        /// </summary>
        public void SetHasEnough(bool hasEnough)
        {
            if (amountText != null)
                amountText.color = hasEnough ? enoughColor : insufficientColor;
        }
    }
}
