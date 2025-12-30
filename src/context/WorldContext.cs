using UnityEngine;
using GameFramework;

namespace TH7
{
    // 探索上下文，生命周期：进入大地图 -> 离开大地图
    public class WorldContext : GameContext
    {
        public MapManager Map { get; private set; }
        public int CurrentHeroIndex { get; set; } = 0;
        public bool IsHeroMoving { get; private set; } = false;

        public void Setup(MapManager mapManager)
        {
            Map = mapManager;
            if (Map == null)
                Debug.LogWarning("[World] MapManager 未设置");
            else
                Debug.Log($"[World] MapManager 已设置，地图大小: {Map.Data?.Width}x{Map.Data?.Height}");
        }

        protected override void OnInitialize()
        {
            Debug.Log("[World] 进入探索阶段");
        }

        protected override void OnUpdate(float deltaTime)
        {
        }

        protected override void OnPause()
        {
            Debug.Log("[World] 探索暂停");
        }

        protected override void OnResume()
        {
            Debug.Log("[World] 探索恢复");
        }

        protected override void OnDispose()
        {
            Debug.Log("[World] 离开探索阶段");
            Map = null;
        }

        // 触发战斗（在 Session 层创建战斗上下文）
        public BattleContext TriggerBattle()
        {
            var session = GetParent<SessionContext>();
            if (session == null)
            {
                Debug.LogError("[World] 找不到 SessionContext");
                return null;
            }

            // 检查是否已在战斗中
            if (session.HasChild<BattleContext>())
            {
                Debug.LogWarning("[World] 已经在战斗中");
                return session.GetChild<BattleContext>();
            }

            // 暂停探索
            Pause();

            // 在 Session 层创建战斗上下文（与 World 并列）
            var battle = session.CreateChild<BattleContext>();
            return battle;
        }

        /// <summary>
        /// 结束当前回合，推进到下一天
        /// </summary>
        public void EndTurn()
        {
            var session = GetParent<SessionContext>();
            session?.AdvanceDay();

            // 执行回合结束逻辑
            // ProcessAITurns();
            // RegenerateResources();
        }
    }
}
