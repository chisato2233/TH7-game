using UnityEngine;
using TMPro;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 天数显示 UI
    /// 需要通过 Bind() 绑定到 SessionContext
    /// </summary>
    public class DayDisplayUI : UIBehaviour
    {
        [Header("Display")]
        [SerializeField] TextMeshProUGUI dayText;
        [SerializeField] TextMeshProUGUI weekText;
        [SerializeField] TextMeshProUGUI monthText;

        [Header("Format")]
        [SerializeField] string dayFormat = "Day {0}";
        [SerializeField] string weekFormat = "Week {0}";
        [SerializeField] string monthFormat = "Month {0}";

        /// <summary>
        /// 绑定到 SessionContext
        /// </summary>
        public void Bind(SessionContext sessionContext)
        {
            if (sessionContext?.Data?.Day != null)
                ListenImmediate(sessionContext.Data.Day, UpdateDisplay);
        }

        void UpdateDisplay(int day)
        {
            int week = (day - 1) / 7 + 1;
            int month = (day - 1) / 28 + 1;

            if (dayText != null)
                dayText.text = string.Format(dayFormat, day);

            if (weekText != null)
                weekText.text = string.Format(weekFormat, week);

            if (monthText != null)
                monthText.text = string.Format(monthFormat, month);
        }
    }
}
