using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗回合管理器
    /// 负责管理战斗回合流程，类似于 WorldTurnManager
    /// </summary>
    public class BattleTurnManager : GameStateMachineBehaviour<BattleTurnState, BattleTurnManager>
    {
        [Header("Config")]
        [SerializeField] int maxRounds = 100;

        BattleContext battleContext;
        BattleActionExecutor actionExecutor;

        readonly System.Collections.Generic.Dictionary<BattleSide, IBattleActionProvider> providers = new();

        // 事件系统
        EventSystem eventSystem;

        // 当前状态
        BattleUnit currentUnit;
        BattleAction pendingAction;

        /// <summary>
        /// 当前回合数（响应式）
        /// </summary>
        public Reactive<int> CurrentRound { get; } = new(0);

        /// <summary>
        /// 当前行动单位（响应式）
        /// </summary>
        public Reactive<BattleUnit> ActiveUnit { get; } = new();

        /// <summary>
        /// 当前回合状态（响应式）
        /// </summary>
        public Reactive<BattleTurnState> TurnState { get; } = new(BattleTurnState.Idle);

        #region State Machine

        protected override BattleTurnManager GetOwner() => this;
        protected override BattleTurnState GetInitialState() => BattleTurnState.Idle;
        protected override bool AutoInitialize => false;

        #endregion

        /// <summary>
        /// 初始化回合管理器
        /// </summary>
        public void Initialize(BattleContext context, BattleActionExecutor executor)
        {
            battleContext = context;
            actionExecutor = executor;
            eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();

            if (actionExecutor != null)
            {
                actionExecutor.OnActionStarted += OnActionStarted;
                actionExecutor.OnActionCompleted += OnActionCompleted;
            }

            InitializeStateMachine();

            Debug.Log("[BattleTurnManager] Initialized");
        }

        /// <summary>
        /// 注册行动提供者
        /// </summary>
        public void RegisterProvider(BattleSide side, IBattleActionProvider provider)
        {
            providers[side] = provider;
            Debug.Log($"[BattleTurnManager] Registered provider for {side}");
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle()
        {
            if (battleContext == null)
            {
                Debug.LogError("[BattleTurnManager] Cannot start battle: context is null");
                return;
            }

            Debug.Log("[BattleTurnManager] Starting battle");
            ChangeState(BattleTurnState.RoundStart);
        }

        #region State Handlers

        /// <summary>
        /// 处理回合开始
        /// </summary>
        public void ProcessRoundStart()
        {
            CurrentRound.Value++;
            TurnState.Value = BattleTurnState.RoundStart;

            // 通知上下文
            battleContext.StartNewRound();

            eventSystem?.Publish(new BattleRoundStartedEvent { Round = CurrentRound.Value });

            Debug.Log($"[BattleTurnManager] Round {CurrentRound.Value} started");

            // 进入选择单位状态
            ChangeState(BattleTurnState.SelectUnit);
        }

        /// <summary>
        /// 选择下一个行动单位
        /// </summary>
        public void SelectNextUnit()
        {
            TurnState.Value = BattleTurnState.SelectUnit;

            currentUnit = battleContext.Initiative.GetNextUnit();
            ActiveUnit.Value = currentUnit;

            if (currentUnit == null)
            {
                // 本回合结束
                Debug.Log("[BattleTurnManager] No more units, round ending");
                ChangeState(BattleTurnState.RoundEnd);
                return;
            }

            Debug.Log($"[BattleTurnManager] Selected unit: {currentUnit.DisplayName}");

            eventSystem?.Publish(new UnitTurnStartedEvent { Unit = currentUnit });

            // 进入等待行动状态
            ChangeState(BattleTurnState.WaitingForAction);
        }

        /// <summary>
        /// 请求行动
        /// </summary>
        public void RequestAction()
        {
            TurnState.Value = BattleTurnState.WaitingForAction;

            if (currentUnit == null)
            {
                Debug.LogError("[BattleTurnManager] No current unit");
                ChangeState(BattleTurnState.SelectUnit);
                return;
            }

            if (!providers.TryGetValue(currentUnit.Side, out var provider))
            {
                Debug.LogError($"[BattleTurnManager] No provider for {currentUnit.Side}");
                return;
            }

            // 选中当前单位
            currentUnit.Select();

            Debug.Log($"[BattleTurnManager] Requesting action from {currentUnit.Side} for {currentUnit.DisplayName}");

            provider.RequestAction(currentUnit, battleContext, OnActionReceived);
        }

        void OnActionReceived(BattleAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[BattleTurnManager] Received null action");
                return;
            }

            pendingAction = action;

            // 取消选中
            currentUnit?.Deselect();

            Debug.Log($"[BattleTurnManager] Received action: {action}");

            // 切换到执行状态
            ChangeState(BattleTurnState.ExecutingAction);

            // 执行行动
            actionExecutor.Execute(action, OnActionExecuted);
        }

        void OnActionStarted(BattleAction action)
        {
            Debug.Log($"[BattleTurnManager] Action started: {action}");
        }

        void OnActionCompleted(BattleAction action, BattleActionResult result)
        {
            Debug.Log($"[BattleTurnManager] Action completed: {action}, Success: {result.IsSuccess}");
        }

        void OnActionExecuted(BattleActionResult result)
        {
            TurnState.Value = BattleTurnState.ExecutingAction;

            // 等待行动不标记为已行动
            if (pendingAction is not BattleWaitAction)
            {
                currentUnit?.OnActionComplete();
            }

            eventSystem?.Publish(new UnitTurnEndedEvent { Unit = currentUnit });

            // 检查战斗是否结束
            battleContext.CheckBattleEnd();

            if (battleContext.Phase == BattlePhase.BattleEnd)
            {
                ChangeState(BattleTurnState.BattleEnd);
                return;
            }

            // 继续选择下一个单位
            ChangeState(BattleTurnState.SelectUnit);
        }

        /// <summary>
        /// 处理回合结束
        /// </summary>
        public void ProcessRoundEnd()
        {
            TurnState.Value = BattleTurnState.RoundEnd;

            eventSystem?.Publish(new BattleRoundEndedEvent { Round = CurrentRound.Value });

            Debug.Log($"[BattleTurnManager] Round {CurrentRound.Value} ended");

            // 检查回合限制
            if (CurrentRound.Value >= maxRounds)
            {
                Debug.Log("[BattleTurnManager] Max rounds reached, battle ends in draw");
                battleContext.EndBattle(BattleResult.Draw);
                ChangeState(BattleTurnState.BattleEnd);
                return;
            }

            // 开始新回合
            ChangeState(BattleTurnState.RoundStart);
        }

        /// <summary>
        /// 处理战斗结束
        /// </summary>
        public void ProcessBattleEnd()
        {
            TurnState.Value = BattleTurnState.BattleEnd;

            Debug.Log($"[BattleTurnManager] Battle ended: {battleContext.Result}");

            // 清理提供者
            foreach (var provider in providers.Values)
            {
                provider?.SetEnabled(false);
            }
        }

        #endregion

        #region Lifecycle

        protected override void OnDestroy()
        {
            if (actionExecutor != null)
            {
                actionExecutor.OnActionStarted -= OnActionStarted;
                actionExecutor.OnActionCompleted -= OnActionCompleted;
            }

            base.OnDestroy();
        }

        #endregion
    }
}
