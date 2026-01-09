using System.Collections.Generic;
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

        // 运行时英雄引用（场景中的 Hero 组件）
        readonly List<Hero> runtimeHeroes = new();

        // 便捷访问
        public string PlayerName => Data.PlayerName;
        public int CurrentDay => Data.Day.Value;
        public int CurrentWeek => (CurrentDay - 1) / 7 + 1;
        public int CurrentMonth => (CurrentDay - 1) / 28 + 1;
        public PlayerResources Resources => Data.Resources;
        public ReactiveList<TownData> Towns => Data.Towns;

        /// <summary>
        /// 运行时英雄列表（场景中的 Hero 组件）
        /// </summary>
        public IReadOnlyList<Hero> Heroes => runtimeHeroes;

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

            // 保存前收集英雄数据
            CollectHeroSaveData();

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

        #region Hero Management

        /// <summary>
        /// 注册运行时英雄（由 Hero 组件在 Awake/Start 时调用）
        /// </summary>
        public void RegisterHero(Hero hero)
        {
            if (hero != null && !runtimeHeroes.Contains(hero))
            {
                runtimeHeroes.Add(hero);
                Debug.Log($"[Session] 注册英雄: {hero.HeroName}");
            }
        }

        /// <summary>
        /// 注销运行时英雄（由 Hero 组件在 OnDestroy 时调用）
        /// </summary>
        public void UnregisterHero(Hero hero)
        {
            if (hero != null && runtimeHeroes.Remove(hero))
            {
                Debug.Log($"[Session] 注销英雄: {hero.HeroName}");
            }
        }

        /// <summary>
        /// 获取指定玩家的英雄
        /// </summary>
        public List<Hero> GetHeroesForPlayer(int playerId)
        {
            var result = new List<Hero>();
            foreach (var hero in runtimeHeroes)
            {
                if (hero.OwnerPlayerId == playerId)
                    result.Add(hero);
            }
            return result;
        }

        /// <summary>
        /// 根据 ID 查找英雄
        /// </summary>
        public Hero GetHeroById(string heroId)
        {
            foreach (var hero in runtimeHeroes)
            {
                if (hero.HeroId == heroId)
                    return hero;
            }
            return null;
        }

        /// <summary>
        /// 保存前收集所有英雄数据
        /// </summary>
        public void CollectHeroSaveData()
        {
            Data.HeroSaveDataList.Clear();
            foreach (var hero in runtimeHeroes)
            {
                Data.HeroSaveDataList.Add(hero.ToSaveData());
            }
        }

        #endregion
    }

    // 存档数据结构（所有公共字段会被 ES3 自动序列化）
    public class SessionData
    {
        public string PlayerName;
        public Reactive<int> Day = new(1);
        public PlayerResources Resources = new();
        public ReactiveList<TownData> Towns = new();

        // 英雄存档数据（用于序列化）
        public List<HeroSaveData> HeroSaveDataList = new();
    }
}
