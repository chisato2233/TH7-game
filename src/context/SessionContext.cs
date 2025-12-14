using UnityEngine;
using GameFramework;

namespace TH7
{
    // 存档会话上下文，生命周期：加载存档 -> 退出存档
    public class SessionContext : GameContext
    {
        public string PlayerName { get; private set; }
        public string SaveFileName { get; private set; }
        public int CurrentDay { get; private set; } = 1;
        public int CurrentWeek => (CurrentDay - 1) / 7 + 1;
        public int CurrentMonth => (CurrentDay - 1) / 28 + 1;

        public void StartNewSession(string playerName)
        {
            PlayerName = playerName;
            SaveFileName = null;
            CurrentDay = 1;
        }

        public void LoadSession(string saveFile)
        {
            SaveFileName = saveFile;
            // TODO: 从存档加载数据
        }

        protected override void OnInitialize()
        {
            Debug.Log($"[Session] 开始会话: {PlayerName ?? SaveFileName ?? "未知"}");
        }

        protected override void OnDispose()
        {
            Debug.Log($"[Session] 存档会话结束，当前天数: {CurrentDay}");
        }

        public void AdvanceDay()
        {
            CurrentDay++;
            Debug.Log($"[Session] 新的一天: 第{CurrentMonth}月第{CurrentWeek}周第{CurrentDay}天");
        }

        public WorldContext StartExploration() =>
            HasChild<WorldContext>() ? GetChild<WorldContext>() : CreateChild<WorldContext>();

        public void EndExploration() => DisposeChild<WorldContext>();
    }
}
