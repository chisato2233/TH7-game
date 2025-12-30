using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TH7
{
    /// <summary>
    /// 玩家输入状态
    /// </summary>
    public enum PlayerInputState
    {
        Disabled,           // 输入禁用
        Idle,               // 空闲，等待选择英雄
        WaitingForTarget,   // 已选中英雄，等待点击目标
        Executing           // 正在执行行动
    }

    /// <summary>
    /// 玩家行动提供者
    /// 将玩家输入转换为 HeroAction
    /// </summary>
    public class PlayerActionProvider : IActionProvider
    {
        public bool RequiresInput => true;
        public bool IsWaiting => state == PlayerInputState.WaitingForTarget;
        public PlayerInputState State => state;

        PlayerInputState state = PlayerInputState.Disabled;
        HeroData currentHero;
        WorldContext context;
        Action<HeroAction> onActionReady;

        // 依赖
        readonly MapManager mapManager;
        readonly Camera mainCamera;
        readonly IPathfinder pathfinder;

        // 输入引用
        InputAction clickAction;
        InputAction rightClickAction;
        InputAction endTurnAction;

        public PlayerActionProvider(MapManager mapManager, Camera camera, IPathfinder pathfinder)
        {
            this.mapManager = mapManager;
            this.mainCamera = camera ?? Camera.main;
            this.pathfinder = pathfinder;
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

        public void RequestAction(HeroData hero, WorldContext ctx, Action<HeroAction> callback)
        {
            currentHero = hero;
            context = ctx;
            onActionReady = callback;
            state = PlayerInputState.WaitingForTarget;

            Debug.Log($"[PlayerInput] 等待 {hero.HeroName} 的行动指令...");
        }

        public void CancelRequest()
        {
            if (state == PlayerInputState.WaitingForTarget)
            {
                state = PlayerInputState.Idle;
                onActionReady = null;
                Debug.Log("[PlayerInput] 行动请求已取消");
            }
        }

        public void SetEnabled(bool enabled)
        {
            state = enabled ? PlayerInputState.Idle : PlayerInputState.Disabled;
            if (!enabled)
            {
                onActionReady = null;
                currentHero = null;
            }
        }

        void OnClick(InputAction.CallbackContext ctx)
        {
            if (state != PlayerInputState.WaitingForTarget) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            var cellPos = mapManager.WorldToCell(worldPos);

            ProcessClick(cellPos);
        }

        void OnRightClick(InputAction.CallbackContext ctx)
        {
            if (state != PlayerInputState.WaitingForTarget) return;

            // 右键取消当前选择或查看信息
            Debug.Log("[PlayerInput] 右键点击 - 取消/查看");
        }

        void OnEndTurn(InputAction.CallbackContext ctx)
        {
            if (state == PlayerInputState.Disabled) return;

            if (currentHero != null)
            {
                SubmitAction(new EndTurnAction(currentHero));
            }
        }

        void ProcessClick(Vector3Int cellPos)
        {
            if (currentHero == null || context == null) return;

            // 检查点击的是什么
            var clickResult = AnalyzeClick(cellPos);

            HeroAction action = clickResult.Type switch
            {
                ClickResultType.EmptyTile => CreateMoveAction(cellPos),
                ClickResultType.Town => new EnterTownAction(currentHero, clickResult.Town),
                ClickResultType.Enemy => new AttackAction(currentHero, cellPos, clickResult.Target),
                ClickResultType.Resource => new PickUpAction(currentHero, cellPos, MapObjectType.Resource),
                _ => null
            };

            if (action != null && action.CanExecute(context))
            {
                SubmitAction(action);
            }
            else
            {
                Debug.Log($"[PlayerInput] 无法执行行动: {clickResult.Type}");
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

            // 检查地形是否可通行
            var tile = context.Map.Data.GetTileAtCell(cellPos);
            if (tile.Ground == GroundType.Void || tile.Surface == SurfaceType.Mountain)
                return ClickResult.Invalid();

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

            var path = pathfinder.FindPath(currentHero.CellPosition, targetCell, context);
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
            var callback = onActionReady;
            onActionReady = null;
            callback?.Invoke(action);
        }

        public void Dispose()
        {
            UnbindInputActions();
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

        public static ClickResult Invalid() => new() { Type = ClickResultType.Invalid };
        public static ClickResult ClickedEmpty() => new() { Type = ClickResultType.EmptyTile };
        public static ClickResult ClickedTown(TownData town) => new() { Type = ClickResultType.Town, Town = town };
        public static ClickResult ClickedEnemy(object enemy) => new() { Type = ClickResultType.Enemy, Target = enemy };
    }
}
