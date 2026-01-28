using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 玩家输入状态
    /// </summary>
    public enum PlayerInputState
    {
        Disabled,           // 输入禁用
        Idle,               // 空闲，等待选择英雄
        HeroSelected,       // 已选中英雄，等待点击目标（可右键取消）
        Executing           // 正在执行行动
    }

    /// <summary>
    /// 玩家行动提供者
    /// 将玩家输入转换为 HeroAction
    ///
    /// 交互流程：
    /// 1. Idle - 等待玩家点击英雄选中
    /// 2. HeroSelected - 选中后显示路径预览，等待点击目标或右键取消
    /// 3. Executing - 执行行动中
    ///
    /// 发布的事件（通过 EventSystem）：
    /// - SelectableHeroesChangedEvent: 可选英雄列表变化
    /// - HeroSelectedEvent: 英雄被选中
    /// - HeroDeselectedEvent: 英雄取消选中
    /// - PathPreviewUpdatedEvent: 路径预览更新
    /// </summary>
    public class PlayerActionProvider : IActionProvider
    {
        public bool RequiresInput => true;
        public bool IsWaiting => state == PlayerInputState.HeroSelected || state == PlayerInputState.Idle;
        public PlayerInputState State => state;

        // ============================================
        // Reactive 数据（供 UI 绑定）
        // ============================================

        /// <summary>
        /// 当前选中的英雄（Reactive，UI 可绑定显示英雄信息）
        /// </summary>
        public Reactive<Hero> SelectedHero { get; } = new();

        /// <summary>
        /// 当前输入状态（Reactive，UI 可绑定显示状态）
        /// </summary>
        public Reactive<PlayerInputState> InputState { get; } = new(PlayerInputState.Disabled);

        // 内部状态（同步到 Reactive）
        PlayerInputState state = PlayerInputState.Disabled;
        Hero currentHero;
        WorldContext context;
        Action<HeroAction> onActionReady;

        // 可选择的英雄列表（当前回合可行动的英雄）
        List<Hero> selectableHeroes = new();

        // 依赖
        readonly MapManager mapManager;
        readonly Camera mainCamera;
        readonly IPathfinder pathfinder;
        EventSystem eventSystem;

        // 输入引用
        InputAction clickAction;
        InputAction rightClickAction;
        InputAction endTurnAction;

        // 路径预览
        PathPreview pathPreview;
        HeroSelectionIndicator selectionIndicator;
        Vector3Int lastHoverCell = new(int.MinValue, int.MinValue, int.MinValue);
        List<Vector3Int> previewPath;

        public PlayerActionProvider(MapManager mapManager, Camera camera, IPathfinder pathfinder)
        {
            this.mapManager = mapManager;
            this.mainCamera = camera ?? Camera.main;
            this.pathfinder = pathfinder;
            this.eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
        }

        /// <summary>
        /// 设置路径预览组件
        /// </summary>
        public void SetPathPreview(PathPreview preview)
        {
            pathPreview = preview;
            pathPreview?.Initialize(mapManager);
        }

        /// <summary>
        /// 设置选择指示器
        /// </summary>
        public void SetSelectionIndicator(HeroSelectionIndicator indicator)
        {
            selectionIndicator = indicator;
        }

        /// <summary>
        /// 绑定 Input Actions
        /// </summary>
        public void BindInputActions(InputAction click, InputAction rightClick, InputAction endTurn)
        {
            // 解绑旧的
            UnbindInputActions();

            clickAction = click;
            rightClickAction = rightClick;
            endTurnAction = endTurn;

            if (clickAction != null)
            {
                clickAction.performed += OnClick;
                clickAction.Enable();
            }

            if (rightClickAction != null)
            {
                rightClickAction.performed += OnRightClick;
                rightClickAction.Enable();
            }

            if (endTurnAction != null)
            {
                endTurnAction.performed += OnEndTurn;
                endTurnAction.Enable();
            }
        }

        void UnbindInputActions()
        {
            if (clickAction != null)
            {
                clickAction.performed -= OnClick;
            }
            if (rightClickAction != null)
            {
                rightClickAction.performed -= OnRightClick;
            }
            if (endTurnAction != null)
            {
                endTurnAction.performed -= OnEndTurn;
            }
        }

        /// <summary>
        /// 请求行动 - 进入 Idle 状态，等待玩家选择英雄
        /// </summary>
        public void RequestAction(Hero hero, WorldContext ctx, Action<HeroAction> callback)
        {
            context = ctx;
            onActionReady = callback;

            // 获取当前可行动的英雄列表
            UpdateSelectableHeroes();

            // 进入空闲状态，等待玩家点击选择英雄
            state = PlayerInputState.Idle;
            currentHero = null;

            Debug.Log($"[PlayerInput] 等待玩家选择英雄... (可选: {selectableHeroes.Count} 个)");
        }

        /// <summary>
        /// 更新可选择的英雄列表
        /// </summary>
        void UpdateSelectableHeroes()
        {
            selectableHeroes.Clear();

            var session = context?.GetParent<SessionContext>();
            if (session != null)
            {
                // 获取玩家控制且还能行动的英雄
                foreach (var hero in session.Heroes)
                {
                    if (hero.IsPlayerControlled && hero.CanAct)
                    {
                        selectableHeroes.Add(hero);
                    }
                }
            }

            // 通过 EventSystem 发布事件
            eventSystem?.Publish(new SelectableHeroesChangedEvent { Heroes = selectableHeroes });
        }

        /// <summary>
        /// 选中英雄（玩家点击英雄后调用）
        /// </summary>
        void SelectHero(Hero hero)
        {
            if (hero == null || !selectableHeroes.Contains(hero)) return;

            currentHero = hero;
            state = PlayerInputState.HeroSelected;

            // 同步到 Reactive
            SelectedHero.Value = hero;
            InputState.Value = state;

            // 显示选择指示器
            selectionIndicator?.Select(hero);

            // 设置路径预览的英雄引用
            pathPreview?.SetHero(hero);

            // 通过 EventSystem 发布事件
            eventSystem?.Publish(new HeroSelectedEvent { Hero = hero });

            Debug.Log($"[PlayerInput] 选中英雄: {hero.HeroName}");
        }

        /// <summary>
        /// 取消选中英雄（右键取消）
        /// </summary>
        void DeselectHero()
        {
            if (state != PlayerInputState.HeroSelected) return;

            currentHero = null;
            state = PlayerInputState.Idle;

            // 同步到 Reactive
            SelectedHero.Value = null;
            InputState.Value = state;

            HidePreview();
            selectionIndicator?.Hide();

            // 通过 EventSystem 发布事件
            eventSystem?.Publish(new HeroDeselectedEvent());

            Debug.Log("[PlayerInput] 取消选中英雄，返回 Idle 状态");
        }

        public void CancelRequest()
        {
            if (state == PlayerInputState.HeroSelected || state == PlayerInputState.Idle)
            {
                state = PlayerInputState.Disabled;
                onActionReady = null;
                currentHero = null;
                selectableHeroes.Clear();

                // 同步到 Reactive
                SelectedHero.Value = null;
                InputState.Value = state;

                HidePreview();
                selectionIndicator?.Hide();
                eventSystem?.Publish(new HeroDeselectedEvent());
                Debug.Log("[PlayerInput] 行动请求已取消");
            }
        }

        public void SetEnabled(bool enabled)
        {
            state = enabled ? PlayerInputState.Idle : PlayerInputState.Disabled;

            // 同步到 Reactive
            InputState.Value = state;

            if (!enabled)
            {
                onActionReady = null;
                currentHero = null;
                SelectedHero.Value = null;
                HidePreview();
                selectionIndicator?.Hide();
                eventSystem?.Publish(new HeroDeselectedEvent());
            }
        }

        /// <summary>
        /// 每帧更新（用于鼠标悬停预览）
        /// </summary>
        public void UpdateHover()
        {
            // 只有选中英雄后才显示路径预览
            if (state != PlayerInputState.HeroSelected || currentHero == null) return;

            var mousePos = Mouse.current.position.ReadValue();
            // 对于正交相机，Z 应该是相机到 tilemap 的距离
            float cameraZ = mainCamera.transform.position.z;
            var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cameraZ));
            var cellPos = mapManager.WorldToCell(worldPos);

            // 只有格子变化时才更新（只比较 x, y）
            if (cellPos.x == lastHoverCell.x && cellPos.y == lastHoverCell.y) return;
            lastHoverCell = cellPos;

            UpdatePathPreview(new Vector3Int(cellPos.x, cellPos.y, 0));
        }

        void UpdatePathPreview(Vector3Int targetCell)
        {
            if (currentHero == null || context == null)
            {
                HidePreview();
                return;
            }

            // 分析点击目标
            var clickResult = AnalyzeClick(targetCell);

            // 无效目标不显示路径
            if (clickResult.Type == ClickResultType.Invalid)
            {
                HidePreview();
                return;
            }

            // 计算路径
            if (pathfinder != null)
            {
                previewPath = pathfinder.FindPath(currentHero.CellPosition.Value, targetCell, context);
            }
            else
            {
                previewPath = new List<Vector3Int> { targetCell };
            }

            if (previewPath == null || previewPath.Count == 0)
            {
                HidePreview();
                return;
            }

            // 计算移动力消耗
            int cost = CalculatePathCost(previewPath);
            bool canReach = currentHero.MovementPoints.Value >= cost;

            // 根据目标类型选择路径预览样式
            PathTargetType targetType = clickResult.Type switch
            {
                ClickResultType.Enemy => PathTargetType.Attack,
                ClickResultType.Resource => PathTargetType.PickUp,
                ClickResultType.Town => PathTargetType.EnterTown,
                _ => PathTargetType.Move
            };

            // 显示路径预览
            pathPreview?.ShowPath(previewPath, canReach, targetType);

            // 通过 EventSystem 发布事件
            eventSystem?.Publish(new PathPreviewUpdatedEvent { Path = previewPath, CanReach = canReach });
        }

        int CalculatePathCost(List<Vector3Int> path)
        {
            if (path == null || context?.Map == null) return 0;

            int total = 0;
            foreach (var cell in path)
            {
                total += context.Map.GetMovementCost(context.Map.CellToWorld(cell));
            }
            return total;
        }

        void HidePreview()
        {
            pathPreview?.Hide();
            previewPath = null;
            lastHoverCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }

        void OnClick(InputAction.CallbackContext ctx)
        {
            if (state == PlayerInputState.Disabled || state == PlayerInputState.Executing) return;

            var mousePos = Mouse.current.position.ReadValue();
            // 对于正交相机，Z 应该是相机到 tilemap 的距离
            float cameraZ = mainCamera.transform.position.z;
            var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cameraZ));
            var cellPos = mapManager.WorldToCell(worldPos);
            var cellPos2D = new Vector3Int(cellPos.x, cellPos.y, 0);

            // Idle 状态：尝试选择英雄
            if (state == PlayerInputState.Idle)
            {
                var clickedHero = GetHeroAtCell(cellPos2D);
                if (clickedHero != null && selectableHeroes.Contains(clickedHero))
                {
                    SelectHero(clickedHero);
                }
                return;
            }

            // HeroSelected 状态：处理移动/交互目标
            if (state == PlayerInputState.HeroSelected)
            {
                // 检查是否点击了其他可选英雄（切换选择）
                var clickedHero = GetHeroAtCell(cellPos2D);
                if (clickedHero != null && clickedHero != currentHero && selectableHeroes.Contains(clickedHero))
                {
                    // 切换到另一个英雄
                    HidePreview();
                    SelectHero(clickedHero);
                    return;
                }

                ProcessClick(cellPos2D);
            }
        }

        void OnRightClick(InputAction.CallbackContext ctx)
        {
            // HeroSelected 状态：右键取消选择，返回 Idle
            if (state == PlayerInputState.HeroSelected)
            {
                DeselectHero();
                return;
            }

            // Idle 状态：右键可以用来查看信息（可选）
            if (state == PlayerInputState.Idle)
            {
                Debug.Log("[PlayerInput] 右键点击 - 查看信息");
            }
        }

        /// <summary>
        /// 获取指定格子上的英雄
        /// </summary>
        Hero GetHeroAtCell(Vector3Int cell)
        {
            var session = context?.GetParent<SessionContext>();
            if (session == null) return null;

            foreach (var hero in session.Heroes)
            {
                if (hero.CellPosition.Value == cell)
                {
                    return hero;
                }
            }
            return null;
        }

        void OnEndTurn(InputAction.CallbackContext ctx)
        {
            RequestEndTurn();
        }

        /// <summary>
        /// 请求结束回合（供外部调用，如 UI 按钮）
        /// </summary>
        public void RequestEndTurn()
        {
            if (state == PlayerInputState.Disabled || state == PlayerInputState.Executing) return;

            // 结束回合需要选择一个英雄来提交（或者用第一个可选英雄）
            var heroForAction = currentHero ?? (selectableHeroes.Count > 0 ? selectableHeroes[0] : null);
            if (heroForAction != null)
            {
                SubmitAction(new EndTurnAction(heroForAction));
            }
        }

        void ProcessClick(Vector3Int cellPos)
        {
            if (currentHero == null || context == null) return;

            // 检查点击的是什么
            var clickResult = AnalyzeClick(cellPos);

            // 无效点击不处理
            if (clickResult.Type == ClickResultType.Invalid)
            {
                Debug.Log($"[PlayerInput] 无效点击位置: {cellPos}");
                return;
            }

            // 所有有效目标都通过 MoveAction 处理
            // 移动到目标位置后，ActionExecutor 会自动处理交互
            var moveAction = CreateMoveAction(cellPos);

            if (moveAction != null && moveAction.CanExecute(context))
            {
                SubmitAction(moveAction);
            }
            else
            {
                Debug.Log($"[PlayerInput] 无法移动到目标: {clickResult.Type}");
            }
        }

        ClickResult AnalyzeClick(Vector3Int cellPos)
        {
            // 检查是否点击城镇
            var session = context.GetParent<SessionContext>();
            if (session != null)
            {
                foreach (var town in session.Towns)
                {
                    if (town.Position == cellPos)
                        return ClickResult.ClickedTown(town);
                }
            }

            // 检查是否点击了地图物件
            var mapObject = context.GetMapObjectAt(cellPos);
            if (mapObject != null)
            {
                // 野怪堆 -> 敌人
                if (mapObject is CreatureStack creatureStack)
                {
                    Debug.Log($"[PlayerInput] 点击野怪: {creatureStack.UnitConfig?.DisplayName} x{creatureStack.Count}");
                    return ClickResult.ClickedEnemy(creatureStack);
                }

                // 资源堆、矿等 -> 可交互物件
                Debug.Log($"[PlayerInput] 点击地图物件: {mapObject.name}");
                return ClickResult.ClickedMapObject(mapObject);
            }

            // 检查地图数据是否存在
            if (context.Map?.Data == null)
            {
                // 没有地图数据时，允许自由移动（测试用）
                Debug.LogWarning("[PlayerInput] 地图数据未初始化，允许自由移动");
                return ClickResult.ClickedEmpty();
            }

            // 检查地形是否可通行
            var tile = context.Map.Data.GetTileAtCell(cellPos);

            // Void 表示超出地图范围或未初始化
            if (tile.Ground == GroundType.Void)
            {
                Debug.Log($"[PlayerInput] 点击位置 {cellPos} 超出地图范围或为空");
                return ClickResult.Invalid();
            }

            if (tile.Surface == SurfaceType.Mountain)
            {
                Debug.Log($"[PlayerInput] 点击位置 {cellPos} 是山脉，无法通行");
                return ClickResult.Invalid();
            }

            // 默认为空地，可以移动
            return ClickResult.ClickedEmpty();
        }

        HeroAction CreateMoveAction(Vector3Int targetCell)
        {
            if (pathfinder == null)
            {
                // 没有寻路器，直接移动（简化）
                var simplePath = new List<Vector3Int> { targetCell };
                return new MoveAction(currentHero, simplePath);
            }

            var path = pathfinder.FindPath(currentHero.CellPosition.Value, targetCell, context);
            if (path == null || path.Count == 0)
            {
                Debug.Log("[PlayerInput] 无法找到路径");
                return null;
            }

            return new MoveAction(currentHero, path);
        }

        void SubmitAction(HeroAction action)
        {
            state = PlayerInputState.Executing;

            // 同步到 Reactive
            InputState.Value = state;
            SelectedHero.Value = null;

            HidePreview();
            selectionIndicator?.Hide();

            // 通过 EventSystem 发布事件
            eventSystem?.Publish(new HeroDeselectedEvent());

            var callback = onActionReady;
            onActionReady = null;
            callback?.Invoke(action);
        }

        public void Dispose()
        {
            UnbindInputActions();
            HidePreview();
            selectionIndicator?.Hide();
        }
    }

    /// <summary>
    /// 点击结果类型
    /// </summary>
    public enum ClickResultType
    {
        Invalid,
        EmptyTile,
        Town,
        Enemy,
        Resource,
        Artifact
    }

    /// <summary>
    /// 点击分析结果
    /// </summary>
    public struct ClickResult
    {
        public ClickResultType Type;
        public TownData Town;
        public object Target;
        public MapObject MapObject;

        public static ClickResult Invalid() => new() { Type = ClickResultType.Invalid };
        public static ClickResult ClickedEmpty() => new() { Type = ClickResultType.EmptyTile };
        public static ClickResult ClickedTown(TownData town) => new() { Type = ClickResultType.Town, Town = town };
        public static ClickResult ClickedEnemy(object enemy) => new() { Type = ClickResultType.Enemy, Target = enemy };
        public static ClickResult ClickedMapObject(MapObject obj) => new() { Type = ClickResultType.Resource, MapObject = obj };
    }
}
