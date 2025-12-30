using UnityEngine;
using GameFramework;

namespace TH7
{
    // 存档会话上下文，生命周期：加载存档 -> 退出存档
    public class SessionContext : GameContext
    {
        const string SAVE_DIR = "saves/";

        // 存档数据（公共字段会被 ES3 自动序列化）
        public SessionData Data = new();

        // 运行时状态（不存档）
        string saveSlotId;

        // 便捷访问
        public string PlayerName => Data.PlayerName;
        public int CurrentDay => Data.Day.Value;
        public int CurrentWeek => (CurrentDay - 1) / 7 + 1;
        public int CurrentMonth => (CurrentDay - 1) / 28 + 1;
        public PlayerResources Resources => Data.Resources;
        public ReactiveList<TownData> Towns => Data.Towns;
        public ReactiveList<HeroData> Heroes => Data.Heroes;

        public void StartNewSession(string playerName)
        {
            Data = new SessionData { PlayerName = playerName };
            Data.Resources.SetStartingResources();
            saveSlotId = null;
        }

        public void LoadSession(string slotId)
        {
            saveSlotId = slotId;
            string path = $"{SAVE_DIR}{slotId}.es3";

            if (ES3.FileExists(path))
            {
                Data = ES3.Load<SessionData>("session", path);
                Debug.Log($"[Session] 加载存档: {slotId}");
            }
        }

        public void SaveSession(string slotId = null)
        {
            slotId ??= saveSlotId ?? "autosave";
            saveSlotId = slotId;
            string path = $"{SAVE_DIR}{slotId}.es3";

            ES3.Save("session", Data, path);
            Debug.Log($"[Session] 保存存档: {slotId}");
        }

        protected override void OnInitialize()
        {
            Debug.Log($"[Session] 开始会话: {PlayerName}");
        }

        protected override void OnDispose()
        {
            Debug.Log($"[Session] 存档会话结束，当前天数: {CurrentDay}");
        }

        public void AdvanceDay()
        {
            Data.Day.Value++;
            Debug.Log($"[Session] 新的一天: 第{CurrentMonth}月第{CurrentWeek}周第{CurrentDay}天");
        }

        public WorldContext StartExploration() =>
            HasChild<WorldContext>() ? GetChild<WorldContext>() : CreateChild<WorldContext>();

        public void EndExploration() => DisposeChild<WorldContext>();
    }

    // 存档数据结构（所有公共字段会被 ES3 自动序列化）
    public class SessionData
    {
        public string PlayerName;
        public Reactive<int> Day = new(1);
        public PlayerResources Resources = new();
        public ReactiveList<TownData> Towns = new();
        public ReactiveList<HeroData> Heroes = new();
    }
}
