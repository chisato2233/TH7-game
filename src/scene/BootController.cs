using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework;

namespace TH7
{
    // 启动场景控制器，初始化核心系统后跳转主菜单
    public class BootController : MonoBehaviour
    {
        [SerializeField] string mainMenuScene = "MainMenuScene";

        void Start()
        {
            InitializeSystems();
            SceneManager.LoadScene(mainMenuScene);
        }

        void InitializeSystems()
        {
            var entry = GameEntry.Instance;
            entry.RegisterSystem(new EventSystem());
            entry.RegisterSystem(new ContextSystem());
            entry.InitAllSystems();
            Debug.Log("[Boot] 系统初始化完成");
        }
    }
}
