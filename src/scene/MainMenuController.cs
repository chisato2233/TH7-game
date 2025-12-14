using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework;

namespace TH7
{
    // 主菜单控制器
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] string worldScene = "WorldScene";

        // 开始新游戏
        public void StartNewGame()
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            // 创建会话上下文
            var session = contextSystem.Root.CreateChild<SessionContext>();
            session.StartNewSession("Player");

            Debug.Log("[MainMenu] 创建新会话，加载世界场景");
            SceneManager.LoadScene(worldScene);
        }

        // 继续游戏（加载存档）
        public void ContinueGame(string saveId)
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            var session = contextSystem.Root.CreateChild<SessionContext>();
            // TODO: 从存档加载数据

            SceneManager.LoadScene(worldScene);
        }

        // 退出游戏
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
