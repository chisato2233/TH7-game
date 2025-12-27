using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework;

namespace TH7
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string worldScene = "WorldScene";

        public void StartNewGame()
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            // 清理旧的 Session（如果存在）
            if (contextSystem.Root.HasChild<SessionContext>())
                contextSystem.Root.DisposeChild<SessionContext>();

            var session = contextSystem.Root.CreateChild<SessionContext>();
            session.StartNewSession("Player");

            Debug.Log("[MainMenu] 创建新会话，加载世界场景");
            SceneManager.LoadScene(worldScene);
        }

        public void ContinueGame(string saveId)
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            var session = contextSystem.Root.CreateChild<SessionContext>();
            session.LoadSession(saveId);

            SceneManager.LoadScene(worldScene);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
