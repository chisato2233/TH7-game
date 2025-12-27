using UnityEngine;
using TMPro;
using Michsky.MUIP;

namespace TH7.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] MainMenuController controller;

        [Header("Title")]
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI subtitleText;
        [SerializeField] TextMeshProUGUI versionText;

        [Header("Buttons")]
        [SerializeField] ButtonManager newGameButton;
        [SerializeField] ButtonManager continueButton;
        [SerializeField] ButtonManager settingsButton;
        [SerializeField] ButtonManager quitButton;

        void Start()
        {
            BindButtons();
            UpdateContinueButtonState();

            if (versionText != null)
                versionText.text = $"v{Application.version}";
        }

        void OnDestroy()
        {
            UnbindButtons();
        }

        void BindButtons()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        void UnbindButtons()
        {
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
            if (controller == null) return;
            controller.StartNewGame();
        }

        void OnContinueClicked()
        {
            if (controller == null) return;
            controller.ContinueGame("latest");
        }

        void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings clicked");
            // TODO: 打开设置面板
        }

        void OnQuitClicked()
        {
            if (controller == null) return;
            controller.QuitGame();
        }

        void UpdateContinueButtonState()
        {
            // TODO: 检查存档文件
            if (continueButton != null)
                continueButton.Interactable(false);
        }

        public void SetContinueButtonEnabled(bool enabled)
        {
            if (continueButton != null)
                continueButton.Interactable(enabled);
        }
    }
}
