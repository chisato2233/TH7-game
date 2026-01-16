using System.Collections.Generic;
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

        // 地图物件管理
        readonly List<MapObject> mapObjects = new();
        readonly List<Mine> mines = new();

        public IReadOnlyList<MapObject> MapObjects => mapObjects;
        public IReadOnlyList<Mine> Mines => mines;

        public void Setup(MapManager mapManager)
        {
            Map = mapManager;
            if (Map == null)
                Debug.LogWarning("[World] MapManager 未设置");
            else
                Debug.Log($"[World] MapManager 已设置，地图大小: {Map.Data?.Width}x{Map.Data?.Height}");
        }

        #region Map Object Management

        public void RegisterMapObject(MapObject obj)
        {
            if (obj == null || mapObjects.Contains(obj)) return;

            mapObjects.Add(obj);
            if (obj is Mine mine)
                mines.Add(mine);

            // 设置坐标转换器
            if (Map != null)
                obj.SetCoordinateConverter(pos => Map.WorldToCell(pos));

            Debug.Log($"[World] Registered {obj.GetType().Name} at {obj.CellPosition}");
        }

        public void UnregisterMapObject(MapObject obj)
        {
            if (obj == null) return;
            mapObjects.Remove(obj);
            if (obj is Mine mine)
                mines.Remove(mine);
        }

        public MapObject GetMapObjectAt(Vector3Int cell)
        {
            foreach (var obj in mapObjects)
            {
                if (obj.CellPosition == cell && !obj.IsCollected)
                    return obj;
            }
            return null;
        }

        public void ProcessDailyMineOutput(SessionContext session)
        {
            foreach (var mine in mines)
                mine.ProduceDailyOutput(session);
        }

        #endregion

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
            mapObjects.Clear();
            mines.Clear();
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
