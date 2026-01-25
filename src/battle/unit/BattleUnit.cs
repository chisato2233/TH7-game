using System;
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 战斗单位属性名称常量
    /// </summary>
    public static class BattleUnitAttributes
    {
        public const string Attack = "Attack";
        public const string Defense = "Defense";
        public const string MinDamage = "MinDamage";
        public const string MaxDamage = "MaxDamage";
        public const string Health = "Health";
        public const string MaxHealth = "MaxHealth";
        public const string Speed = "Speed";
        public const string Morale = "Morale";
        public const string Luck = "Luck";
        public const string Shots = "Shots";
    }

    /// <summary>
    /// 战斗单位
    /// 复用 UnitConfig 配置，管理战斗中的单位实例
    /// 属性全部取决于 UnitConfig
    /// </summary>
    public class BattleUnit : GameStateMachineBehaviour<BattleUnitState, BattleUnit>
    {
        #region Identity & Config

        [Header("Identity")]
        [SerializeField] string unitId;
        [SerializeField] UnitConfig config;
        [SerializeField] BattleSide side;

        /// <summary>
        /// 单位 ID
        /// </summary>
        public string UnitId => unitId;

        /// <summary>
        /// 单位配置
        /// </summary>
        public UnitConfig Config => config;

        /// <summary>
        /// 所属阵营
        /// </summary>
        public BattleSide Side => side;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => config?.DisplayName ?? unitId;

        #endregion

        #region State Machine

        protected override BattleUnit GetOwner() => this;
        protected override BattleUnitState GetInitialState() => BattleUnitState.Idle;

        public bool IsIdle => IsInState(BattleUnitState.Idle);
        public bool IsActing => IsInState(BattleUnitState.Acting);
        public bool IsDead => IsInState(BattleUnitState.Dead);

        #endregion

        #region Combat Stats (Reactive)

        /// <summary>
        /// 当前单位数量
        /// </summary>
        public Reactive<int> Count { get; } = new(1);

        /// <summary>
        /// 顶部单位当前生命值
        /// </summary>
        public Reactive<int> CurrentHealth { get; } = new();

        /// <summary>
        /// 是否已行动
        /// </summary>
        public Reactive<bool> HasActed { get; } = new(false);

        /// <summary>
        /// 是否在防御
        /// </summary>
        public Reactive<bool> IsDefending { get; } = new(false);

        /// <summary>
        /// 是否等待
        /// </summary>
        public Reactive<bool> IsWaiting { get; } = new(false);

        /// <summary>
        /// 剩余弹药（远程单位）
        /// </summary>
        public Reactive<int> Shots { get; } = new();

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => Count.Value > 0;

        /// <summary>
        /// 是否可以行动
        /// </summary>
        public bool CanAct => IsAlive && !HasActed.Value;

        /// <summary>
        /// 是否为远程单位
        /// </summary>
        public bool IsRanged => config?.IsRanged ?? false;

        /// <summary>
        /// 是否为飞行单位
        /// </summary>
        public bool IsFlying => config?.IsFlying ?? false;

        #endregion

        #region Position

        /// <summary>
        /// 格子位置（响应式）
        /// </summary>
        public Reactive<Vector2Int> CellPosition { get; } = new();

        #endregion

        #region Ability System

        AbilitySystemComponent abilitySystem;

        /// <summary>
        /// 技能系统组件
        /// </summary>
        public AbilitySystemComponent AbilitySystem
        {
            get
            {
                if (abilitySystem == null)
                    InitializeAbilitySystem();
                return abilitySystem;
            }
        }

        #endregion

        #region View

        [Header("View")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Animator animator;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public Animator Animator => animator;

        #endregion

        #region Combat State

        /// <summary>
        /// 本回合是否已反击
        /// </summary>
        bool hasRetaliatedThisTurn;

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化战斗单位（从 UnitStack 创建）
        /// </summary>
        public void Initialize(UnitConfig unitConfig, int count, BattleSide battleSide, Vector2Int position)
        {
            config = unitConfig;
            unitId = unitConfig.UnitId;
            side = battleSide;

            // 初始化数量和生命值（全部来自 UnitConfig）
            Count.Value = count;
            CurrentHealth.Value = unitConfig.Health;
            CellPosition.Value = position;

            // 初始化弹药（来自 UnitConfig）
            if (unitConfig.IsRanged)
            {
                Shots.Value = unitConfig.Shots > 0 ? unitConfig.Shots : int.MaxValue;
            }

            InitializeAbilitySystem();

            Debug.Log($"[BattleUnit] Initialized {DisplayName} x{count} at {position} ({battleSide})");
        }

        void InitializeAbilitySystem()
        {
            if (config == null) return;

            abilitySystem = new AbilitySystemComponent();

            // 所有属性都来自 UnitConfig
            abilitySystem.InitializeFromConfig(
                (BattleUnitAttributes.Attack, config.Attack),
                (BattleUnitAttributes.Defense, config.Defense),
                (BattleUnitAttributes.MinDamage, config.MinDamage),
                (BattleUnitAttributes.MaxDamage, config.MaxDamage),
                (BattleUnitAttributes.Health, config.Health),
                (BattleUnitAttributes.MaxHealth, config.Health),
                (BattleUnitAttributes.Speed, config.Speed),
                (BattleUnitAttributes.Morale, 1),
                (BattleUnitAttributes.Luck, 1)
            );

            // 添加标签
            abilitySystem.AddTag(side == BattleSide.Attacker ? "Team.Attacker" : "Team.Defender");
            abilitySystem.AddTag($"Unit.Tier{(int)config.Tier}");

            if (config.IsRanged)
                abilitySystem.AddTag("Unit.Ranged");
            if (config.IsFlying)
                abilitySystem.AddTag("Unit.Flying");
        }

        #endregion

        #region Combat Actions

        /// <summary>
        /// 获取速度值（用于行动顺序）
        /// </summary>
        public int GetSpeed()
        {
            return abilitySystem?.Attributes?.GetCurrentValueInt(BattleUnitAttributes.Speed) ?? config?.Speed ?? 0;
        }

        /// <summary>
        /// 获取移动范围
        /// </summary>
        public int GetMovementRange()
        {
            int speed = GetSpeed();
            // 飞行单位移动范围更大
            return IsFlying ? speed * 2 : speed;
        }

        /// <summary>
        /// 投掷伤害（计算本次攻击的基础伤害）
        /// </summary>
        public int RollDamage()
        {
            int min = abilitySystem?.Attributes?.GetCurrentValueInt(BattleUnitAttributes.MinDamage) ?? config?.MinDamage ?? 1;
            int max = abilitySystem?.Attributes?.GetCurrentValueInt(BattleUnitAttributes.MaxDamage) ?? config?.MaxDamage ?? 1;

            // 每个单位都造成伤害
            return UnityEngine.Random.Range(min, max + 1) * Count.Value;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public DamageResult TakeDamage(int damage, BattleUnit source)
        {
            var result = new DamageResult();

            // 计算攻防修正
            int defense = abilitySystem?.Attributes?.GetCurrentValueInt(BattleUnitAttributes.Defense) ?? config?.Defense ?? 0;
            int sourceAttack = source?.AbilitySystem?.Attributes?.GetCurrentValueInt(BattleUnitAttributes.Attack) ?? 0;

            // 伤害公式: damage * (1 + 0.05 * (attack - defense))
            // 攻击高于防御：伤害增加
            // 防御高于攻击：伤害减少
            float modifier = 1f + 0.05f * (sourceAttack - defense);
            modifier = Mathf.Clamp(modifier, 0.3f, 4f); // 限制在 30%-400%

            int finalDamage = Mathf.RoundToInt(damage * modifier);

            // 防御状态减伤 30%
            if (IsDefending.Value)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * 0.7f);
            }

            result.FinalDamage = finalDamage;

            // 计算死亡数量
            int remainingDamage = finalDamage;
            int killed = 0;
            int currentHp = CurrentHealth.Value;
            int maxHp = config?.Health ?? 1;

            while (remainingDamage > 0 && Count.Value > killed)
            {
                if (remainingDamage >= currentHp)
                {
                    remainingDamage -= currentHp;
                    killed++;
                    currentHp = maxHp; // 下一个单位满血
                }
                else
                {
                    currentHp -= remainingDamage;
                    remainingDamage = 0;
                }
            }

            result.UnitsKilled = killed;
            Count.Value -= killed;
            CurrentHealth.Value = Count.Value > 0 ? currentHp : 0;
            result.RemainingCount = Count.Value;
            result.RemainingHealth = CurrentHealth.Value;

            Debug.Log($"[BattleUnit] {DisplayName} took {finalDamage} damage, killed {killed}, remaining {Count.Value}");

            if (Count.Value <= 0)
            {
                Die(source);
            }

            return result;
        }

        /// <summary>
        /// 检查是否可以反击
        /// </summary>
        public bool CanRetaliate(BattleUnit attacker)
        {
            // 已死亡、已反击过、被远程攻击时无法反击
            return IsAlive && !hasRetaliatedThisTurn && IsAdjacent(attacker);
        }

        /// <summary>
        /// 检查是否与目标相邻
        /// </summary>
        public bool IsAdjacent(BattleUnit other)
        {
            if (other == null) return false;
            var diff = CellPosition.Value - other.CellPosition.Value;
            return Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1 && diff != Vector2Int.zero;
        }

        /// <summary>
        /// 标记已反击
        /// </summary>
        public void MarkRetaliated()
        {
            hasRetaliatedThisTurn = true;
        }

        /// <summary>
        /// 单位死亡
        /// </summary>
        void Die(BattleUnit killer = null)
        {
            ChangeState(BattleUnitState.Dead);

            GameEntry.Instance?.GetSystem<EventSystem>()?.Publish(
                new UnitDiedEvent { Unit = this, Killer = killer }
            );

            Debug.Log($"[BattleUnit] {DisplayName} died");
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// 回合开始处理
        /// </summary>
        public void OnRoundStart()
        {
            HasActed.Value = false;
            IsDefending.Value = false;
            IsWaiting.Value = false;
            hasRetaliatedThisTurn = false;

            abilitySystem?.OnTurnStart();

            if (IsAlive)
            {
                ChangeState(BattleUnitState.Idle);
            }
        }

        /// <summary>
        /// 行动完成
        /// </summary>
        public void OnActionComplete()
        {
            HasActed.Value = true;
            if (IsAlive)
            {
                ChangeState(BattleUnitState.Idle);
            }
        }

        /// <summary>
        /// 等待行动
        /// </summary>
        public void Wait()
        {
            IsWaiting.Value = true;
            ChangeState(BattleUnitState.Waiting);
        }

        /// <summary>
        /// 防御行动
        /// </summary>
        public void Defend()
        {
            IsDefending.Value = true;
            HasActed.Value = true;
            ChangeState(BattleUnitState.Defending);
        }

        #endregion

        #region State Control

        /// <summary>
        /// 选中单位
        /// </summary>
        public void Select()
        {
            if (IsAlive)
            {
                ChangeState(BattleUnitState.Selected);
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void Deselect()
        {
            if (IsAlive && IsInState(BattleUnitState.Selected))
            {
                ChangeState(BattleUnitState.Idle);
            }
        }

        /// <summary>
        /// 开始移动
        /// </summary>
        public void StartMoving()
        {
            ChangeState(BattleUnitState.Moving);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMoving()
        {
            if (IsAlive)
            {
                ChangeState(BattleUnitState.Idle);
            }
        }

        /// <summary>
        /// 开始行动（攻击/施法）
        /// </summary>
        public void StartActing()
        {
            ChangeState(BattleUnitState.Acting);
        }

        #endregion

        #region View Updates

        /// <summary>
        /// 设置移动状态（控制动画）
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (animator != null && HasAnimatorParameter("IsMoving"))
            {
                animator.SetBool("IsMoving", isMoving);
            }
        }

        /// <summary>
        /// 播放攻击动画
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (animator != null && HasAnimatorParameter("Attack"))
            {
                animator.SetTrigger("Attack");
            }
        }

        /// <summary>
        /// 播放受伤动画
        /// </summary>
        public void PlayHitAnimation()
        {
            if (animator != null && HasAnimatorParameter("Hit"))
            {
                animator.SetTrigger("Hit");
            }
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (animator != null && HasAnimatorParameter("Die"))
            {
                animator.SetTrigger("Die");
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
        public void SetFacing(Vector2Int direction)
        {
            if (spriteRenderer != null && direction.x != 0)
            {
                // 攻击方默认朝右，防守方默认朝左
                bool faceLeft = direction.x < 0;
                spriteRenderer.flipX = side == BattleSide.Attacker ? faceLeft : !faceLeft;
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            abilitySystem?.Cleanup();
        }

        protected override void OnDestroy()
        {
            Cleanup();
            base.OnDestroy();
        }

        #endregion
    }
}
