using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TH7.UI
{
    // MainMenu UGUI 视图层,负责 UI 交互和显示
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] MainMenuController controller;

        [Header("UI Elements")]
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI subtitleText;
        [SerializeField] TextMeshProUGUI versionText;

        [Header("Buttons")]
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;

        void Start()
        {
            // 注册按钮事件
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // 检查是否有存档来决定 Continue 按钮是否可用
            UpdateContinueButtonState();
        }

        void OnDestroy()
        {
            // 取消按钮事件注册
            if (newGameButton != null)
                newGameButton.onClick.RemoveListener(OnNewGameClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        void OnNewGameClicked()
        {
            if (controller == null)
            {
                Debug.LogError("[MainMenuUI] Controller reference is null!");
                return;
            }

            controller.StartNewGame();
        }

        void OnContinueClicked()
        {
            if (controller == null)
            {
                Debug.LogError("[MainMenuUI] Controller reference is null!");
                return;
            }

            controller.ContinueGame("latest");
        }

        void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings clicked");
            // TODO: 打开设置面板
        }

        void OnQuitClicked()
        {
            if (controller == null)
            {
                Debug.LogError("[MainMenuUI] Controller reference is null!");
                return;
            }

            controller.QuitGame();
        }

        void UpdateContinueButtonState()
        {
            // TODO: 检查是否有存档文件
            // 暂时禁用 Continue 按钮
            if (continueButton != null)
            {
                continueButton.interactable = false;
                var colors = continueButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                continueButton.colors = colors;
            }
        }

        // 公开方法供 Controller 调用,用于更新 UI 状态
        public void SetContinueButtonEnabled(bool enabled)
        {
            if (continueButton != null)
            {
                continueButton.interactable = enabled;
            }
        }
    }
}
