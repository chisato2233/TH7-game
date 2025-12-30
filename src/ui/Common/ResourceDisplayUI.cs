using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 单个资源显示组件
    /// </summary>
    public class ResourceDisplayUI : UIBehaviour
    {
        [Header("Display")]
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI amountText;

        [Header("Settings")]
        [SerializeField] ResourceType resourceType;
        [SerializeField] string format = "{0}";
        [SerializeField] bool autoBindToSession = true;

        Reactive<int> boundResource;

        protected override void Start()
        {
            base.Start();

            if (autoBindToSession)
                TryBindToSession();
        }

        void TryBindToSession()
        {
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            var session = contextSystem?.Root?.GetChild<SessionContext>();

            if (session?.Resources != null)
            {
                Bind(session.Resources.Get(resourceType));
            }
            else
            {
                // SessionContext 尚未创建，显示默认值（不输出警告，这是正常情况）
                boundResource = null;
                UpdateDisplay(0);
            }
        }

        /// <summary>
        /// 绑定到响应式资源值
        /// </summary>
        public void Bind(Reactive<int> resource)
        {
            boundResource = resource;
            if (resource != null)
                ListenImmediate(resource, UpdateDisplay);
        }

        /// <summary>
        /// 手动设置资源类型并绑定
        /// </summary>
        public void SetResourceType(ResourceType type, PlayerResources resources)
        {
            resourceType = type;
            Bind(resources.Get(type));
        }

        void UpdateDisplay(int amount)
        {
            if (amountText != null)
                amountText.text = string.Format(format, FormatAmount(amount));
        }

        string FormatAmount(int amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000f:0.#}M";
            if (amount >= 10000)
                return $"{amount / 1000f:0.#}K";
            return amount.ToString();
        }
    }
}
