using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 存档会话上下文
    /// 生命周期：加载存档 -> 退出存档
    /// 包含：玩家数据、地图状态、资源、英雄列表等
    /// </summary>
    public class SessionContext : GameContext
    {
        // 存档数据（示例）
        public string SaveFileName { get; set; }
        public int CurrentDay { get; private set; } = 1;
        public int CurrentWeek => (CurrentDay - 1) / 7 + 1;
        public int CurrentMonth => (CurrentDay - 1) / 28 + 1;

        // 可以在这里添加更多存档相关数据
        // public PlayerData Player { get; private set; }
        // public List<HeroData> Heroes { get; private set; }
        // public MapData Map { get; private set; }
        // public ResourceData Resources { get; private set; }

        protected override void OnInitialize()
        {
            Debug.Log($"[Session] 加载存档: {SaveFileName ?? "新游戏"}");

            // 这里可以加载存档数据
            // if (!string.IsNullOrEmpty(SaveFileName))
            //     LoadFromFile(SaveFileName);
        }

        protected override void OnDispose()
        {
            Debug.Log($"[Session] 存档会话结束，当前天数: {CurrentDay}");

            // 可以在这里自动保存
            // AutoSave();
        }

        /// <summary>
        /// 推进到下一天
        /// </summary>
        public void AdvanceDay()
        {
            CurrentDay++;
            Debug.Log($"[Session] 新的一天: 第{CurrentMonth}月第{CurrentWeek}周第{CurrentDay}天");

            // 触发每日事件
            // GameEntry.GetSystem<EventSystem>()?.Publish(new DayChangedEvent { Day = CurrentDay });
        }

        /// <summary>
        /// 开始探索（创建探索上下文）
        /// </summary>
        public WorldContext StartExploration()
        {
            if (HasChild<WorldContext>())
            {
                Debug.LogWarning("[Session] 探索上下文已存在");
                return GetChild<WorldContext>();
            }

            return CreateChild<WorldContext>();
        }

        /// <summary>
        /// 结束探索
        /// </summary>
        public void EndExploration()
        {
            DisposeChild<WorldContext>();
        }
    }
}
