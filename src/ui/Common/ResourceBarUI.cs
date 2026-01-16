using UnityEngine;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 资源条 - 显示所有资源
    /// </summary>
    public class ResourceBarUI : UIBehaviour
    {
        [Header("Resource Displays")]
        [SerializeField] ResourceDisplayUI goldDisplay;
        [SerializeField] ResourceDisplayUI woodDisplay;
        [SerializeField] ResourceDisplayUI oreDisplay;
        [SerializeField] ResourceDisplayUI crystalDisplay;

        [Header("Settings")]
        [SerializeField] bool autoBindOnStart = true;

        protected override void Start()
        {
            base.Start();

            if (autoBindOnStart)
                TryBindToSession();
        }

        void TryBindToSession()
        {
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            var session = contextSystem?.Root?.GetChild<SessionContext>();

            if (session?.Resources != null)
                BindToResources(session.Resources);
        }

        /// <summary>
        /// 绑定到玩家资源
        /// </summary>
        public void BindToResources(PlayerResources resources)
        {
            if (resources == null) return;

            BindDisplay(goldDisplay, ResourceType.Gold, resources);
            BindDisplay(woodDisplay, ResourceType.Wood, resources);
            BindDisplay(oreDisplay, ResourceType.Ore, resources);
            BindDisplay(crystalDisplay, ResourceType.Crystal, resources);
        }

        void BindDisplay(ResourceDisplayUI display, ResourceType type, PlayerResources resources)
        {
            if (display != null)
                display.SetResourceType(type, resources);
        }
    }
}
