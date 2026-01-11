using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 回合阶段显示 UI
    /// 绑定到 WorldTurnManager.CurrentPhase
    /// </summary>
    public class TurnPhaseUI : UIBehaviour
    {
        [Header("Display")]
        [SerializeField] TextMeshProUGUI phaseText;
        [SerializeField] Image phaseIcon;

        [Header("Phase Text")]
        [SerializeField] string idleText = "Idle";
        [SerializeField] string playerTurnText = "Your Turn";
        [SerializeField] string aiTurnText = "Enemy Turn";
        [SerializeField] string turnEndText = "Day Ending...";

        [Header("Phase Colors")]
        [SerializeField] Color idleColor = Color.gray;
        [SerializeField] Color playerTurnColor = Color.green;
        [SerializeField] Color aiTurnColor = Color.red;
        [SerializeField] Color turnEndColor = Color.yellow;

        WorldTurnManager turnManager;

        /// <summary>
        /// 绑定到 WorldTurnManager
        /// </summary>
        public void Bind(WorldTurnManager manager)
        {
            turnManager = manager;
            if (turnManager?.CurrentPhase != null)
                ListenImmediate(turnManager.CurrentPhase, UpdateDisplay);
        }

        void UpdateDisplay(TurnPhase phase)
        {
            string text = phase switch
            {
                TurnPhase.Idle => idleText,
                TurnPhase.PlayerTurn => playerTurnText,
                TurnPhase.AITurn => aiTurnText,
                TurnPhase.TurnEnd => turnEndText,
                _ => phase.ToString()
            };

            Color color = phase switch
            {
                TurnPhase.Idle => idleColor,
                TurnPhase.PlayerTurn => playerTurnColor,
                TurnPhase.AITurn => aiTurnColor,
                TurnPhase.TurnEnd => turnEndColor,
                _ => Color.white
            };

            if (phaseText != null)
            {
                phaseText.text = text;
                phaseText.color = color;
            }

            if (phaseIcon != null)
                phaseIcon.color = color;
        }
    }
}
