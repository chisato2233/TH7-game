using System.Collections.Generic;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 世界回合管理器
    /// 管理回合流程、英雄行动顺序
    /// </summary>
    public class WorldTurnManager : GameBehaviour
    {
        [Header("References")]
        [SerializeField] WorldSceneController sceneController;

        [Header("Debug")]
        [SerializeField] bool debugMode;

        // 依赖
        WorldContext worldContext;
        ActionExecutor actionExecutor;
        readonly Dictionary<int, IActionProvider> providers = new();

        // 状态
        TurnPhase phase = TurnPhase.Idle;
        int currentPlayerIndex;
        int currentHeroIndex;
        Hero currentHero;

        // 事件系统
        EventSystem eventSystem;

        public TurnPhase Phase => phase;
        public Hero CurrentHero => currentHero;
        public bool IsPlayerTurn => phase == TurnPhase.PlayerTurn;

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
            if (debugMode)
                Debug.Log("[TurnManager] 新的一天开始");

            // 重置所有英雄移动力
            var session = worldContext?.GetParent<SessionContext>();
            if (session != null)
            {
                foreach (var hero in session.Heroes)
                    hero.ResetMovement();
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
                // 该玩家没有英雄，跳到下一个
                NextPlayer();
                return;
            }

            var previousPhase = phase;
            phase = playerId == 0 ? TurnPhase.PlayerTurn : TurnPhase.AITurn;

            eventSystem?.Publish(new TurnPhaseChangedEvent
            {
                Phase = phase,
                PreviousPhase = previousPhase
            });

            currentHeroIndex = 0;
            StartHeroTurn(playerHeroes[0]);
        }

        /// <summary>
        /// 开始英雄回合
        /// </summary>
        void StartHeroTurn(Hero hero)
        {
            currentHero = hero;

            eventSystem?.Publish(new HeroTurnStartedEvent { Hero = hero });

            if (!providers.TryGetValue(hero.OwnerPlayerId, out var provider))
            {
                Debug.LogError($"[TurnManager] 找不到玩家 {hero.OwnerPlayerId} 的 ActionProvider");
                EndHeroTurn();
                return;
            }

            // 请求行动
            provider.RequestAction(hero, worldContext, OnActionReceived);
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

            // 结束回合特殊处理
            if (action.Type == HeroActionType.EndTurn)
            {
                EndDay();
                return;
            }

            // 等待特殊处理
            if (action.Type == HeroActionType.Wait)
            {
                EndHeroTurn();
                return;
            }

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
            // 根据结果处理
            switch (result.Type)
            {
                case ActionResultType.EnterTown:
                    var town = result.Data as TownData;
                    if (town != null && currentHero != null)
                    {
                        eventSystem?.Publish(new EnterTownRequestedEvent
                        {
                            Hero = currentHero,
                            Town = town
                        });
                    }
                    // 不自动继续，等待城镇关闭后调用 Resume()
                    return;

                case ActionResultType.TriggerBattle:
                    eventSystem?.Publish(new BattleRequestedEvent
                    {
                        Hero = currentHero,
                        Enemy = result.Data
                    });
                    // 不自动继续，等待战斗结束后调用 Resume()
                    return;

                case ActionResultType.TurnEnded:
                    EndDay();
                    return;
            }

            // 检查当前英雄是否还能行动
            if (currentHero != null && currentHero.CanAct)
            {
                ContinueHeroTurn();
            }
            else
            {
                EndHeroTurn();
            }
        }

        /// <summary>
        /// 继续当前英雄回合
        /// </summary>
        void ContinueHeroTurn()
        {
            if (!providers.TryGetValue(currentHero.OwnerPlayerId, out var provider))
                return;

            provider.RequestAction(currentHero, worldContext, OnActionReceived);
        }

        /// <summary>
        /// 结束当前英雄回合
        /// </summary>
        void EndHeroTurn()
        {
            if (currentHero != null)
            {
                eventSystem?.Publish(new HeroTurnEndedEvent { Hero = currentHero });
            }

            var session = worldContext?.GetParent<SessionContext>();
            var playerHeroes = session?.GetHeroesForPlayer(currentPlayerIndex) ?? new List<Hero>();

            currentHeroIndex++;
            if (currentHeroIndex < playerHeroes.Count)
            {
                StartHeroTurn(playerHeroes[currentHeroIndex]);
            }
            else
            {
                NextPlayer();
            }
        }

        /// <summary>
        /// 下一个玩家
        /// </summary>
        void NextPlayer()
        {
            currentPlayerIndex++;

            // 简化：暂时只有人类玩家
            if (currentPlayerIndex > 0)
            {
                EndDay();
            }
            else
            {
                StartPlayerTurn(currentPlayerIndex);
            }
        }

        /// <summary>
        /// 结束一天
        /// </summary>
        void EndDay()
        {
            var previousPhase = phase;
            phase = TurnPhase.TurnEnd;

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
            StartDay();
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
            if (currentHero != null && currentHero.CanAct)
            {
                ContinueHeroTurn();
            }
            else
            {
                EndHeroTurn();
            }
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
