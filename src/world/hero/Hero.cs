using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 英雄属性名称常量
    /// </summary>
    public static class HeroAttributes
    {
        public const string MovementPoints = "MovementPoints";
        public const string MaxMovementPoints = "MaxMovementPoints";
        public const string Attack = "Attack";
        public const string Defense = "Defense";
        public const string SpellPower = "SpellPower";
        public const string Knowledge = "Knowledge";
        public const string Morale = "Morale";
        public const string Luck = "Luck";
    }

    /// <summary>
    /// 英雄组件 - 统一管理数据、逻辑和视觉表现
    /// </summary>
    public class Hero : GameStateMachineBehaviour<HeroState, Hero>
    {
        #region Identity

        [Header("Identity")]
        [SerializeField] string heroId;
        [SerializeField] string heroName;
        [SerializeField] int ownerPlayerId;
        [SerializeField] HeroConfig config;

        public string HeroId => heroId;
        public string HeroName => heroName;
        public int OwnerPlayerId => ownerPlayerId;
        public bool IsPlayerControlled => ownerPlayerId == 0;
        public HeroConfig Config => config;

        #endregion

        #region State Machine

        // GameStateMachineBehaviour 实现
        protected override Hero GetOwner() => this;
        protected override HeroState GetInitialState() => HeroState.Idle;

        public bool IsIdle => IsInState(HeroState.Idle);
        public bool IsMoving => IsInState(HeroState.Moving);

        #endregion

        #region Position

        [Header("Position")]
        [SerializeField] Vector3Int cellPosition;

        /// <summary>
        /// 格子坐标（响应式）
        /// </summary>
        public Reactive<Vector3Int> CellPosition { get; } = new();

        #endregion

        #region Movement

        [Header("Movement")]
        [SerializeField] int maxMovementPoints = 20;

        /// <summary>
        /// 当前移动力（响应式）
        /// </summary>
        public Reactive<int> MovementPoints { get; } = new(20);

        /// <summary>
        /// 最大移动力
        /// </summary>
        public int MaxMovementPoints => maxMovementPoints;

        /// <summary>
        /// 是否还能行动
        /// </summary>
        public bool CanAct => MovementPoints.Value > 0;

        #endregion

        #region Army

        /// <summary>
        /// 军队槽位（7个）
        /// </summary>
        UnitStack[] army = new UnitStack[7];

        public UnitStack[] Army => army;

        #endregion

        #region Ability System

        AbilitySystemComponent abilitySystem;

        /// <summary>
        /// 技能系统组件（延迟初始化）
        /// </summary>
        public AbilitySystemComponent AbilitySystem
        {
            get
            {
                if (abilitySystem == null)
                {
                    InitializeAbilitySystem();
                }
                return abilitySystem;
            }
        }

        #endregion

        #region View References

        [Header("View")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Animator animator;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Animator Animator => animator;

        #endregion

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // 同步初始位置
            CellPosition.Value = cellPosition;

            // 初始化移动力
            MovementPoints.Value = maxMovementPoints;
        }

        protected override void Start()
        {
            base.Start();

            // 自动注册到 SessionContext
            AutoRegisterToSession();

            // 监听位置变化，更新 Transform
            ListenImmediate(CellPosition, OnCellPositionChanged);
        }

        /// <summary>
        /// 自动注册到 SessionContext（场景中预放置的 Hero 使用）
        /// </summary>
        void AutoRegisterToSession()
        {
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            var session = contextSystem?.Root?.GetChild<SessionContext>();
            session?.RegisterHero(this);
        }

        protected override void OnDestroy()
        {
            // 自动注销
            var contextSystem = GameEntry.Instance?.GetSystem<ContextSystem>();
            var session = contextSystem?.Root?.GetChild<SessionContext>();
            session?.UnregisterHero(this);

            base.OnDestroy();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 运行时初始化
        /// </summary>
        public void Initialize(string id, string name, Vector3Int position, int ownerId = 0)
        {
            heroId = id;
            heroName = name;
            ownerPlayerId = ownerId;
            CellPosition.Value = position;
            MovementPoints.Value = maxMovementPoints;

            InitializeAbilitySystem();
        }

        /// <summary>
        /// 从配置初始化
        /// </summary>
        public void InitializeFromConfig(HeroConfig heroConfig, Vector3Int position, int ownerId = 0)
        {
            config = heroConfig;
            heroId = heroConfig.HeroId;
            heroName = heroConfig.DisplayName;
            ownerPlayerId = ownerId;
            maxMovementPoints = heroConfig.BaseMovementPoints;
            CellPosition.Value = position;
            MovementPoints.Value = maxMovementPoints;

            // 初始化军队
            if (heroConfig.StartingArmy != null)
            {
                for (int i = 0; i < Mathf.Min(heroConfig.StartingArmy.Length, army.Length); i++)
                {
                    var unit = heroConfig.StartingArmy[i];
                    army[i] = new UnitStack { UnitId = unit.UnitId, Count = unit.Count };
                }
            }

            InitializeAbilitySystem();

            // 设置视觉组件
            if (heroConfig.AnimatorController != null && animator != null)
            {
                animator.runtimeAnimatorController = heroConfig.AnimatorController;
            }
        }

        /// <summary>
        /// 从存档数据初始化
        /// </summary>
        public void LoadFromSaveData(HeroSaveData data)
        {
            heroId = data.HeroId;
            heroName = data.HeroName;
            ownerPlayerId = data.OwnerPlayerId;
            maxMovementPoints = data.MaxMovementPoints;
            CellPosition.Value = data.CellPosition;
            MovementPoints.Value = data.MovementPoints;

            // 加载军队
            if (data.Army != null)
            {
                for (int i = 0; i < Mathf.Min(data.Army.Length, army.Length); i++)
                {
                    army[i] = data.Army[i];
                }
            }

            InitializeAbilitySystem();
        }

        /// <summary>
        /// 导出存档数据
        /// </summary>
        public HeroSaveData ToSaveData()
        {
            return new HeroSaveData
            {
                HeroId = heroId,
                HeroName = heroName,
                OwnerPlayerId = ownerPlayerId,
                CellPosition = CellPosition.Value,
                MovementPoints = MovementPoints.Value,
                MaxMovementPoints = maxMovementPoints,
                Army = (UnitStack[])army.Clone()
            };
        }

        void InitializeAbilitySystem()
        {
            abilitySystem = new AbilitySystemComponent();

            // 初始化基础属性
            int attack = config != null ? config.Attack : 1;
            int defense = config != null ? config.Defense : 1;
            int spellPower = config != null ? config.SpellPower : 1;
            int knowledge = config != null ? config.Knowledge : 1;

            abilitySystem.InitializeFromConfig(
                (HeroAttributes.MaxMovementPoints, maxMovementPoints),
                (HeroAttributes.MovementPoints, MovementPoints.Value),
                (HeroAttributes.Attack, attack),
                (HeroAttributes.Defense, defense),
                (HeroAttributes.SpellPower, spellPower),
                (HeroAttributes.Knowledge, knowledge)
            );

            // 添加英雄标签
            abilitySystem.AddTag("Unit.Hero");
            abilitySystem.AddTag(IsPlayerControlled ? "Team.Player" : "Team.Enemy");

            // 监听属性变化，同步到本地字段
            Listen(abilitySystem.Attributes, HeroAttributes.MovementPoints,
                v => MovementPoints.Value = Mathf.RoundToInt(v));
        }

        #endregion

        #region Movement Logic

        /// <summary>
        /// 消耗移动力
        /// </summary>
        public bool ConsumeMovement(int cost)
        {
            if (MovementPoints.Value < cost) return false;

            MovementPoints.Value -= cost;

            // 同步到技能系统
            abilitySystem?.Attributes.SetBaseValue(
                HeroAttributes.MovementPoints,
                MovementPoints.Value
            );

            return true;
        }

        /// <summary>
        /// 重置移动力（新回合）
        /// </summary>
        public void ResetMovement()
        {
            // 从技能系统获取最大移动力（可能被效果修改）
            if (abilitySystem != null)
            {
                int maxMp = abilitySystem.Attributes.GetCurrentValueInt(HeroAttributes.MaxMovementPoints);
                MovementPoints.Value = maxMp;
                abilitySystem.Attributes.SetBaseValue(HeroAttributes.MovementPoints, maxMp);
            }
            else
            {
                MovementPoints.Value = maxMovementPoints;
            }
        }

        /// <summary>
        /// 移动到指定格子
        /// </summary>
        public void MoveTo(Vector3Int targetCell)
        {
            CellPosition.Value = targetCell;
        }

        #endregion

        #region Turn Logic

        /// <summary>
        /// 回合开始处理
        /// </summary>
        public void OnTurnStart()
        {
            ResetMovement();
            abilitySystem?.OnTurnStart();
            ChangeState(HeroState.Idle);
        }

        /// <summary>
        /// 回合结束处理
        /// </summary>
        public void OnTurnEnd()
        {
            abilitySystem?.OnTurnEnd();
            ChangeState(HeroState.Disabled);
        }

        #endregion

        #region State Machine Control

        /// <summary>
        /// 开始移动
        /// </summary>
        public void StartMoving()
        {
            ChangeState(HeroState.Moving);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMoving()
        {
            ChangeState(CanAct ? HeroState.Idle : HeroState.Disabled);
        }

        /// <summary>
        /// 开始交互（城镇、战斗等）
        /// </summary>
        public void StartInteraction()
        {
            ChangeState(HeroState.Interacting);
        }

        /// <summary>
        /// 结束交互
        /// </summary>
        public void EndInteraction()
        {
            ChangeState(CanAct ? HeroState.Idle : HeroState.Disabled);
        }

        /// <summary>
        /// 禁用英雄
        /// </summary>
        public void Disable()
        {
            ChangeState(HeroState.Disabled);
        }

        /// <summary>
        /// 启用英雄
        /// </summary>
        public void Enable()
        {
            ChangeState(HeroState.Idle);
        }

        #endregion

        #region Ability & Effect

        /// <summary>
        /// 授予技能
        /// </summary>
        public AbilityInstance GrantAbility(GameplayAbility ability)
        {
            return AbilitySystem.GrantAbility(ability);
        }

        /// <summary>
        /// 应用效果
        /// </summary>
        public EffectInstance ApplyEffect(GameplayEffect effect, object source = null)
        {
            return AbilitySystem.ApplyEffectToSelf(effect, source);
        }

        #endregion

        #region View Updates

        void OnCellPositionChanged(Vector3Int newCell)
        {
            // 更新 Transform 位置（需要 MapManager 转换坐标）
            // 这里暂时直接使用格子坐标，实际应该通过 MapManager 转换
            transform.position = new Vector3(newCell.x, newCell.y, 0);
        }

        /// <summary>
        /// 设置世界坐标转换器
        /// </summary>
        public void SetPositionConverter(Func<Vector3Int, Vector3> cellToWorld)
        {
            if (cellToWorld != null)
            {
                // 重新订阅，使用真正的坐标转换
                ListenImmediate(CellPosition, cell =>
                {
                    transform.position = cellToWorld(cell);
                });
            }
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (animator != null)
            {
                animator.Play(animationName);
            }
        }

        /// <summary>
        /// 设置移动状态（控制 Idle/Move 动画切换）
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (animator != null && HasAnimatorParameter("IsMoving"))
            {
                animator.SetBool("IsMoving", isMoving);
            }
        }

        bool HasAnimatorParameter(string paramName)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return false;
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }

        /// <summary>
        /// 设置朝向
        /// </summary>
        public void SetFacing(Vector3Int direction)
        {
            if (spriteRenderer != null && direction.x != 0)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        #endregion

        #region Debug

        void OnDrawGizmosSelected()
        {
            // 绘制当前位置
            Gizmos.color = IsPlayerControlled ? Color.blue : Color.red;
            Vector3 pos = Application.isPlaying
                ? new Vector3(CellPosition.Value.x, CellPosition.Value.y, 0)
                : new Vector3(cellPosition.x, cellPosition.y, 0);
            Gizmos.DrawWireSphere(pos, 0.3f);
        }

        #endregion
    }
}
