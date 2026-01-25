using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗 HUD
    /// 显示回合信息、当前单位、行动按钮等
    /// </summary>
    public class BattleHUD : GameBehaviour
    {
        [Header("Round Info")]
        [SerializeField] TextMeshProUGUI roundText;

        [Header("Current Unit")]
        [SerializeField] GameObject unitInfoPanel;
        [SerializeField] Image unitIcon;
        [SerializeField] TextMeshProUGUI unitNameText;
        [SerializeField] TextMeshProUGUI unitCountText;
        [SerializeField] TextMeshProUGUI unitHealthText;
        [SerializeField] Slider healthSlider;

        [Header("Action Buttons")]
        [SerializeField] Button waitButton;
        [SerializeField] Button defendButton;
        [SerializeField] Button retreatButton;

        [Header("Initiative Queue")]
        [SerializeField] Transform initiativeQueueRoot;
        [SerializeField] GameObject initiativeItemPrefab;

        BattleContext context;
        BattleTurnManager turnManager;

        protected override void Start()
        {
            base.Start();

            // 绑定按钮事件
            if (waitButton != null)
                waitButton.onClick.AddListener(OnWaitClicked);
            if (defendButton != null)
                defendButton.onClick.AddListener(OnDefendClicked);
            if (retreatButton != null)
                retreatButton.onClick.AddListener(OnRetreatClicked);
        }

        /// <summary>
        /// 绑定到战斗上下文和回合管理器
        /// </summary>
        public void Bind(BattleContext battleContext, BattleTurnManager manager)
        {
            context = battleContext;
            turnManager = manager;

            if (context != null)
            {
                // 监听回合数
                ListenImmediate(context.Round, UpdateRound);

                // 监听当前单位
                ListenImmediate(context.ActiveUnit, UpdateActiveUnit);
            }

            if (turnManager != null)
            {
                // 监听回合状态
                ListenImmediate(turnManager.TurnState, UpdateButtonStates);
            }
        }

        void UpdateRound(int round)
        {
            if (roundText != null)
            {
                roundText.text = $"Round {round}";
            }
        }

        void UpdateActiveUnit(BattleUnit unit)
        {
            if (unitInfoPanel == null) return;

            if (unit == null || !unit.IsAlive)
            {
                unitInfoPanel.SetActive(false);
                return;
            }

            unitInfoPanel.SetActive(true);

            // 更新单位信息
            if (unitNameText != null)
                unitNameText.text = unit.DisplayName;

            if (unitIcon != null && unit.Config?.Icon != null)
                unitIcon.sprite = unit.Config.Icon;

            if (unitCountText != null)
                unitCountText.text = $"x{unit.Count.Value}";

            if (unitHealthText != null)
                unitHealthText.text = $"{unit.CurrentHealth.Value}/{unit.Config?.Health ?? 0}";

            if (healthSlider != null && unit.Config != null)
            {
                healthSlider.maxValue = unit.Config.Health;
                healthSlider.value = unit.CurrentHealth.Value;
            }

            // 监听单位数据变化
            Listen(unit.Count, count =>
            {
                if (unitCountText != null)
                    unitCountText.text = $"x{count}";
            });

            Listen(unit.CurrentHealth, hp =>
            {
                if (unitHealthText != null)
                    unitHealthText.text = $"{hp}/{unit.Config?.Health ?? 0}";
                if (healthSlider != null)
                    healthSlider.value = hp;
            });
        }

        void UpdateButtonStates(BattleTurnState state)
        {
            bool canAct = state == BattleTurnState.WaitingForAction;

            // 只有玩家回合时才能使用按钮
            bool isPlayerTurn = canAct && context?.ActiveUnit.Value?.Side == BattleSide.Attacker;

            if (waitButton != null)
                waitButton.interactable = isPlayerTurn;
            if (defendButton != null)
                defendButton.interactable = isPlayerTurn;
            if (retreatButton != null)
                retreatButton.interactable = true; // 撤退始终可用
        }

        void OnWaitClicked()
        {
            // 通过 BattleInputController 获取 BattleActionProvider
            var inputController = FindFirstObjectByType<BattleInputController>();
            inputController?.ActionProvider?.OnWaitRequested();
        }

        void OnDefendClicked()
        {
            var inputController = FindFirstObjectByType<BattleInputController>();
            inputController?.ActionProvider?.OnDefendRequested();
        }

        void OnRetreatClicked()
        {
            context?.Retreat();
        }

        /// <summary>
        /// 更新行动顺序显示
        /// </summary>
        public void UpdateInitiativeQueue()
        {
            if (initiativeQueueRoot == null || initiativeItemPrefab == null || context == null)
                return;

            // 清除现有项
            foreach (Transform child in initiativeQueueRoot)
            {
                Destroy(child.gameObject);
            }

            // 获取行动顺序
            var order = context.Initiative.GetRemainingOrder();

            // 创建显示项
            foreach (var unit in order)
            {
                var item = Instantiate(initiativeItemPrefab, initiativeQueueRoot);
                var image = item.GetComponent<Image>();
                if (image != null && unit.Config?.Icon != null)
                {
                    image.sprite = unit.Config.Icon;
                }

                // 设置阵营颜色边框
                var outline = item.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = unit.Side == BattleSide.Attacker ? Color.blue : Color.red;
                }
            }
        }

        protected override void OnDestroy()
        {
            if (waitButton != null)
                waitButton.onClick.RemoveListener(OnWaitClicked);
            if (defendButton != null)
                defendButton.onClick.RemoveListener(OnDefendClicked);
            if (retreatButton != null)
                retreatButton.onClick.RemoveListener(OnRetreatClicked);

            base.OnDestroy();
        }
    }
}
