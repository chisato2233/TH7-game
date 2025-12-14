using UnityEngine;
using GameFramework;

namespace TH7
{
    // 世界场景控制器，负责创建 WorldContext 并关联 MapManager
    public class WorldSceneController : MonoBehaviour
    {
        [SerializeField] MapManager mapManager;

        WorldContext worldContext;

        void Start()
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();
            var session = contextSystem.Root.GetChild<SessionContext>();

            if (session == null)
            {
                Debug.LogError("[WorldScene] SessionContext 不存在");
                return;
            }

            // 创建 WorldContext 并注入 MapManager
            worldContext = session.CreateChild<WorldContext>();
            worldContext.Setup(mapManager);

            Debug.Log("[WorldScene] WorldContext 已创建并关联 MapManager");
        }

        void OnDestroy()
        {
            if (worldContext != null)
            {
                var session = worldContext.GetParent<SessionContext>();
                session?.DisposeChild<WorldContext>();
                worldContext = null;
            }
        }
    }
}
