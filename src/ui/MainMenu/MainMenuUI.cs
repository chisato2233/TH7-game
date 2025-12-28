using UnityEngine;
using TMPro;
using Michsky.MUIP;
using GameFramework;

namespace TH7.UI
{
    public class MainMenuUI : UIBehaviour
    {
        [SerializeField] MainMenuController controller;

        [Header("Title")]
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI subtitleText;
        [SerializeField] TextMeshProUGUI versionText;

        [Header("Buttons")]
        [SerializeField] ButtonManager newGameButton;
        [SerializeField] ButtonManager continueButton;
        [SerializeField] ButtonManager loadGameButton;
        [SerializeField] ButtonManager settingsButton;
        [SerializeField] ButtonManager quitButton;

        [Header("Panels")]
        [SerializeField] SaveSlotWindow saveSlotWindow;

        protected override void Start()
        {
            base.Start();

            BindButtons();

            if (versionText != null)
                versionText.text = $"v{Application.version}";

            // 监听存档列表数量变化，自动更新按钮状态
            if (controller != null)
                ListenCountImmediate(controller.SaveSlots, OnSaveCountChanged);
        }

        protected override void OnDestroy()
        {
            UnbindButtons();
            base.OnDestroy();
        }

        void BindButtons()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGameClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (loadGameButton != null)
                loadGameButton.onClick.AddListener(OnLoadGameClicked);

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

            if (loadGameButton != null)
                loadGameButton.onClick.RemoveListener(OnLoadGameClicked);

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

            // 快速继续：加载最新存档
            var latestSave = controller.GetLatestSave();
            if (latestSave != null)
                controller.ContinueGame(latestSave.SlotId);
        }

        void OnLoadGameClicked()
        {
            if (controller == null || saveSlotWindow == null) return;

            // 打开存档选择弹窗（绑定 ReactiveList）
            saveSlotWindow.Show(controller.SaveSlots, OnSaveSlotSelected);
        }

        void OnSaveSlotSelected(SaveSlotInfo slot)
        {
            if (controller == null || slot == null) return;
            controller.ContinueGame(slot.SlotId);
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

        void OnSaveCountChanged(int count)
        {
            bool hasSave = count > 0;

            // Continue 按钮：有存档时启用
            if (continueButton != null)
                continueButton.Interactable(hasSave);

            // Load Game 按钮：有存档时启用
            if (loadGameButton != null)
                loadGameButton.Interactable(hasSave);
        }
    }
}
