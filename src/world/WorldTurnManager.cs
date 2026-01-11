using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 世界回合管理器
    /// 管理回合流程、英雄行动顺序
    /// </summary>
    public class WorldTurnManager : GameStateMachineBehaviour<TurnState, WorldTurnManager>
    {
        [Header("References")]
        [SerializeField] WorldSceneController sceneController;

        // 依赖
        WorldContext worldContext;
        ActionExecutor actionExecutor;
        readonly Dictionary<int, IActionProvider> providers = new();

        // ============================================
        // Reactive 数据（供 UI 绑定）
        // ============================================

        /// <summary>
        /// 当前回合阶段（Reactive，UI 可绑定显示"玩家回合"/"AI回合"等）
        /// </summary>
        public Reactive<TurnPhase> CurrentPhase { get; } = new(TurnPhase.Idle);

        /// <summary>
        /// 当前行动的英雄（Reactive，UI 可绑定）
        /// </summary>
        public Reactive<Hero> ActiveHero { get; } = new();

        // 状态数据（同步到 Reactive）
        TurnPhase phase = TurnPhase.Idle;
        int currentPlayerIndex;
        int currentHeroIndex;
        Hero currentHero;
        HeroAction pendingAction;

        // 事件系统
        EventSystem eventSystem;

        // 属性
        public TurnPhase Phase => phase;
        public Hero CurrentHero => currentHero;
        public bool IsPlayerTurn => phase == TurnPhase.PlayerTurn;
        public bool IsWaitingForAction => IsInState(TurnState.WaitingForAction);

        // StateMachineBehaviour 实现
        protected override WorldTurnManager GetOwner() => this;
        protected override TurnState GetInitialState() => TurnState.Idle;
        protected override bool AutoInitialize => false; // 手动初始化

        /// <summary>
        /// 初始化回合管理器
        /// </summary>
        public void Initialize(WorldContext context, ActionExecutor executor)
        {
            worldContext = context;
            actionExecutor = executor;
            eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();

            // 监听行动执行器事件
            if (actionExecutor != null)
            {
                actionExecutor.OnActionStarted += OnActionStarted;
                actionExecutor.OnActionCompleted += OnActionCompleted;
            }

            // 初始化状态机
            InitializeStateMachine();

            if (debugMode)
                Debug.Log("[TurnManager] Initialized");
        }

        /// <summary>
        /// 注册行动提供者
        /// </summary>
        public void RegisterProvider(int playerId, IActionProvider provider)
        {
            providers[playerId] = provider;
        }

        /// <summary>
        /// 开始新的一天
        /// </summary>
        public void StartDay()
        {
            ChangeState(TurnState.DayStart);
        }

        /// <summary>
        /// 处理日开始逻辑（由状态调用）
        /// </summary>
        public void ProcessDayStart()
        {
            if (debugMode)
                Debug.Log("[TurnManager] 新的一天开始");

            // 重置所有英雄移动力
            var session = worldContext?.GetParent<SessionContext>();
            if (session != null)
            {
                foreach (var hero in session.Heroes)
                {
                    hero.ResetMovement();
                    hero.OnTurnStart();
                }
            }

            // 从玩家回合开始
            currentPlayerIndex = 0;
            StartPlayerTurn(0);
        }

        /// <summary>
        /// 开始指定玩家的回合
        /// </summary>
        void StartPlayerTurn(int playerId)
        {
            var session = worldContext?.GetParent<SessionContext>();
            var playerHeroes = session?.GetHeroesForPlayer(playerId) ?? new List<Hero>();

            if (playerHeroes.Count == 0)
            {
                // 该玩家没有英雄，直接结束回合
                ChangeState(TurnState.DayEnd);
                return;
            }

            var previousPhase = phase;
            phase = playerId == 0 ? TurnPhase.PlayerTurn : TurnPhase.AITurn;

            // 同步到 Reactive
            CurrentPhase.Value = phase;

            eventSystem?.Publish(new TurnPhaseChangedEvent
            {
                Phase = phase,
                PreviousPhase = previousPhase
            });

            currentHeroIndex = 0;
            StartHeroTurn(playerHeroes[0]);
        }

        /// <summary>
        /// 开始玩家行动阶段（不再指定具体英雄，让玩家自由选择）
        /// </summary>
        void StartHeroTurn(Hero hero)
        {
            // 不再自动选中英雄，由玩家手动选择
            currentHero = null;

            // 发布事件通知 UI 可以高亮可行动的英雄
            eventSystem?.Publish(new HeroTurnStartedEvent { Hero = hero });

            if (!providers.TryGetValue(hero.OwnerPlayerId, out var provider))
            {
                Debug.LogError($"[TurnManager] 找不到玩家 {hero.OwnerPlayerId} 的 ActionProvider");
                EndHeroTurn();
                return;
            }

            // 切换到等待行动状态
            ChangeState(TurnState.WaitingForAction);
        }

        /// <summary>
        /// 请求下一个行动（由状态调用）
        /// </summary>
        public void RequestNextAction()
        {
            // 获取玩家 Provider（玩家 ID = 0）
            if (!providers.TryGetValue(0, out var provider))
            {
                Debug.LogError("[TurnManager] 找不到玩家 0 的 ActionProvider");
                ChangeState(TurnState.DayEnd);
                return;
            }

            // 传递 null，让 Provider 自己管理可选英雄列表
            provider.RequestAction(null, worldContext, OnActionReceived);
        }

        /// <summary>
        /// 收到行动
        /// </summary>
        void OnActionReceived(HeroAction action)
        {
            if (action == null)
            {
                Debug.LogWarning("[TurnManager] 收到空行动");
                return;
            }

            pendingAction = action;
            currentHero = action.Hero; // 从 action 中获取执行行动的英雄

            // 同步到 Reactive
            ActiveHero.Value = currentHero;

            // 结束回合特殊处理
            if (action.Type == HeroActionType.EndTurn)
            {
                ChangeState(TurnState.DayEnd);
                return;
            }

            // 等待特殊处理（跳过当前英雄）
            if (action.Type == HeroActionType.Wait)
            {
                // 标记当前英雄本回合不再行动
                currentHero?.Disable();
                ContinuePlayerTurn();
                return;
            }

            // 切换到执行状态
            ChangeState(TurnState.ExecutingAction);

            // 执行行动
            actionExecutor.Execute(action, OnActionExecuted);
        }

        void OnActionStarted(HeroAction action)
        {
            eventSystem?.Publish(new ActionStartedEvent { Action = action });
        }

        void OnActionCompleted(HeroAction action, ActionResult result)
        {
            eventSystem?.Publish(new ActionCompletedEvent
            {
                Action = action,
                Result = result
            });
        }

        /// <summary>
        /// 行动执行完成
        /// </summary>
        void OnActionExecuted(ActionResult result)
        {
            // 停止英雄移动
            currentHero?.StopMoving();

            // 根据结果处理
            switch (result.Type)
            {
                case ActionResultType.EnterTown:
                    var town = result.Data as TownData;
                    if (town != null && currentHero != null)
                    {
                        currentHero.StartInteraction();
                        ChangeState(TurnState.Interacting);
                        eventSystem?.Publish(new EnterTownRequestedEvent
                        {
                            Hero = currentHero,
                            Town = town
                        });
                    }
                    return;

                case ActionResultType.TriggerBattle:
                    currentHero?.StartInteraction();
                    ChangeState(TurnState.Interacting);
                    eventSystem?.Publish(new BattleRequestedEvent
                    {
                        Hero = currentHero,
                        Enemy = result.Data
                    });
                    return;

                case ActionResultType.TurnEnded:
                    ChangeState(TurnState.DayEnd);
                    return;
            }

            // 行动完成后，继续玩家回合（让玩家选择下一个英雄或继续当前英雄）
            ContinuePlayerTurn();
        }

        /// <summary>
        /// 继续玩家回合（检查是否还有可行动的英雄）
        /// </summary>
        void ContinuePlayerTurn()
        {
            var session = worldContext?.GetParent<SessionContext>();
            if (session == null)
            {
                ChangeState(TurnState.DayEnd);
                return;
            }

            // 检查是否还有可行动的英雄
            bool hasActableHero = false;
            foreach (var hero in session.Heroes)
            {
                if (hero.IsPlayerControlled && hero.CanAct)
                {
                    hasActableHero = true;
                    break;
                }
            }

            if (hasActableHero)
            {
                // 继续等待玩家行动
                ChangeState(TurnState.WaitingForAction);
            }
            else
            {
                // 所有英雄都行动完毕，结束回合
                ChangeState(TurnState.DayEnd);
            }
        }

        /// <summary>
        /// 结束当前英雄回合（用于 StartHeroTurn 中找不到 Provider 的情况）
        /// </summary>
        void EndHeroTurn()
        {
            // 简化：直接结束回合
            ChangeState(TurnState.DayEnd);
        }

        /// <summary>
        /// 处理日结束逻辑（由状态调用）
        /// </summary>
        public void ProcessDayEnd()
        {
            var previousPhase = phase;
            phase = TurnPhase.TurnEnd;

            // 同步到 Reactive
            CurrentPhase.Value = phase;
            ActiveHero.Value = null;

            eventSystem?.Publish(new TurnPhaseChangedEvent
            {
                Phase = phase,
                PreviousPhase = previousPhase
            });

            var session = worldContext?.GetParent<SessionContext>();
            if (session != null)
            {
                // 城镇产出
                ProcessTownProduction(session);

                // 推进日期
                session.AdvanceDay();

                // 发布日结束事件
                eventSystem?.Publish(new DayEndedEvent
                {
                    Day = session.CurrentDay,
                    Week = session.CurrentWeek,
                    Month = session.CurrentMonth
                });

                // 检查是否新的一周
                if (session.CurrentDay % 7 == 1)
                {
                    ProcessWeekStart(session);

                    eventSystem?.Publish(new WeekStartedEvent
                    {
                        Week = session.CurrentWeek,
                        Month = session.CurrentMonth
                    });
                }
            }

            // 自动开始新的一天
            phase = TurnPhase.Idle;
            CurrentPhase.Value = phase;
            ChangeState(TurnState.DayStart);
        }

        void ProcessTownProduction(SessionContext session)
        {
            foreach (var town in session.Towns)
            {
                int goldPerDay = town.GetDailyGoldProduction();
                if (goldPerDay > 0)
                    session.Resources.Add(ResourceType.Gold, goldPerDay);
            }
        }

        void ProcessWeekStart(SessionContext session)
        {
            if (debugMode)
                Debug.Log($"[TurnManager] 新的一周开始 (第{session.CurrentWeek}周)");

            foreach (var town in session.Towns)
            {
                town.RefreshWeeklyRecruits();
            }
        }

        /// <summary>
        /// 恢复回合（从城镇/战斗返回后调用）
        /// </summary>
        public void Resume()
        {
            // 结束英雄交互状态
            currentHero?.EndInteraction();

            // 继续玩家回合（检查是否还有可行动的英雄）
            ContinuePlayerTurn();
        }

        protected override void OnDestroy()
        {
            if (actionExecutor != null)
            {
                actionExecutor.OnActionStarted -= OnActionStarted;
                actionExecutor.OnActionCompleted -= OnActionCompleted;
            }

            base.OnDestroy();
        }
    }
}
