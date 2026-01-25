using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 战斗单位工厂
    /// 从 UnitStack 创建 BattleUnit 实例
    /// </summary>
    public class BattleUnitFactory
    {
        readonly UnitConfigDatabase unitDatabase;
        readonly GameObject unitPrefab;
        readonly Transform container;

        public BattleUnitFactory(UnitConfigDatabase database, GameObject prefab, Transform parent = null)
        {
            unitDatabase = database;
            unitPrefab = prefab;
            container = parent;
        }

        /// <summary>
        /// 从 UnitStack 创建战斗单位
        /// </summary>
        public BattleUnit Create(UnitStack stack, BattleSide side, Vector2Int position)
        {
            if (stack == null || stack.Count <= 0)
            {
                Debug.LogWarning("[BattleUnitFactory] Invalid unit stack");
                return null;
            }

            // 获取单位配置
            var config = unitDatabase?.GetUnit(stack.UnitId);
            if (config == null)
            {
                Debug.LogError($"[BattleUnitFactory] UnitConfig not found: {stack.UnitId}");
                return null;
            }

            return Create(config, stack.Count, side, position);
        }

        /// <summary>
        /// 从 UnitConfig 创建战斗单位
        /// </summary>
        public BattleUnit Create(UnitConfig config, int count, BattleSide side, Vector2Int position)
        {
            if (config == null)
            {
                Debug.LogError("[BattleUnitFactory] UnitConfig is null");
                return null;
            }

            // 优先使用配置中的 Prefab
            var prefabToUse = config.Prefab != null ? config.Prefab : unitPrefab;

            BattleUnit unit;
            if (prefabToUse != null)
            {
                var go = Object.Instantiate(prefabToUse, container);
                unit = go.GetComponent<BattleUnit>();
                if (unit == null)
                {
                    unit = go.AddComponent<BattleUnit>();
                }
                go.name = $"BattleUnit_{config.DisplayName}";
            }
            else
            {
                var go = new GameObject($"BattleUnit_{config.DisplayName}");
                if (container != null)
                {
                    go.transform.SetParent(container);
                }
                unit = go.AddComponent<BattleUnit>();
            }

            // 初始化单位
            unit.Initialize(config, count, side, position);

            return unit;
        }

        /// <summary>
        /// 批量创建攻击方单位
        /// </summary>
        public BattleUnit[] CreateAttackerUnits(UnitStack[] army, BattleMap map)
        {
            return CreateUnits(army, BattleSide.Attacker, map, isAttacker: true);
        }

        /// <summary>
        /// 批量创建防守方单位
        /// </summary>
        public BattleUnit[] CreateDefenderUnits(UnitStack[] army, BattleMap map)
        {
            return CreateUnits(army, BattleSide.Defender, map, isAttacker: false);
        }

        BattleUnit[] CreateUnits(UnitStack[] army, BattleSide side, BattleMap map, bool isAttacker)
        {
            if (army == null || army.Length == 0)
            {
                return System.Array.Empty<BattleUnit>();
            }

            var units = new System.Collections.Generic.List<BattleUnit>();
            int index = 0;

            foreach (var stack in army)
            {
                if (stack == null || stack.Count <= 0) continue;

                // 获取起始位置
                var position = isAttacker
                    ? map.GetAttackerStartPosition(index)
                    : map.GetDefenderStartPosition(index);

                var unit = Create(stack, side, position);
                if (unit != null)
                {
                    // 将单位放置到地图上
                    map.PlaceUnit(unit, position);
                    units.Add(unit);
                    index++;
                }
            }

            return units.ToArray();
        }
    }
}
