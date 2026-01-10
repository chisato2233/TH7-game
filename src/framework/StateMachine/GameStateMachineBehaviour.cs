using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 状态机 GameBehaviour 封装
    /// 结合 GameBehaviour（事件订阅、响应式监听）和状态机功能
    /// </summary>
    /// <typeparam name="TState">状态枚举类型</typeparam>
    /// <typeparam name="TOwner">拥有者类型</typeparam>
    public abstract class GameStateMachineBehaviour<TState, TOwner> : GameBehaviour
        where TState : Enum
        where TOwner : class
    {
        [Header("State Machine Debug")]
        [SerializeField] protected bool debugMode;
        [SerializeField] string currentStateName;
        [SerializeField] string previousStateName;
        [SerializeField] float stateTime;
        [SerializeField] bool isRunning;
        [SerializeField] int registeredStateCount;
        [SerializeField] int transitionCount;

        protected StateMachine<TState, TOwner> StateMachine { get; private set; }

        public TState CurrentState => StateMachine.CurrentState;
        public TState PreviousState => StateMachine.PreviousState;
        public float StateTime => StateMachine?.StateTime ?? 0f;

        /// <summary>
        /// 获取拥有者对象（子类实现）
        /// </summary>
        protected abstract TOwner GetOwner();

        /// <summary>
        /// 获取初始状态（子类实现）
        /// </summary>
        protected abstract TState GetInitialState();

        /// <summary>
        /// 配置状态机（子类可重写）
        /// </summary>
        protected virtual void ConfigureStateMachine() { }

        /// <summary>
        /// 是否自动注册状态
        /// </summary>
        protected virtual bool AutoRegisterStates => true;

        /// <summary>
        /// 是否自动初始化状态机（在 Start 中）
        /// </summary>
        protected virtual bool AutoInitialize => true;

        protected override void Awake()
        {
            base.Awake();

            StateMachine = new StateMachine<TState, TOwner>();

            if (debugMode)
            {
                StateMachine.OnStateChanged += (from, to) =>
                {
                    Debug.Log($"[{GetType().Name}] {from} -> {to}");
                };
            }
        }

        protected override void Start()
        {
            base.Start();

            if (AutoInitialize)
            {
                InitializeStateMachine();
            }
        }

        /// <summary>
        /// 初始化状态机（可在子类中手动调用）
        /// </summary>
        protected void InitializeStateMachine()
        {
            ConfigureStateMachine();
            StateMachine.Initialize(GetOwner(), GetInitialState(), AutoRegisterStates);
            currentStateName = StateMachine.CurrentState.ToString();
        }

        protected virtual void Update()
        {
            StateMachine?.Update(Time.deltaTime);
            UpdateDebugInfo();
        }

        void UpdateDebugInfo()
        {
            if (StateMachine == null) return;

            currentStateName = StateMachine.CurrentState.ToString();
            previousStateName = StateMachine.PreviousState.ToString();
            stateTime = StateMachine.StateTime;
            isRunning = StateMachine.IsRunning;
            registeredStateCount = StateMachine.RegisteredStateCount;
            transitionCount = StateMachine.TransitionCount;
        }

        protected virtual void FixedUpdate()
        {
            StateMachine?.FixedUpdate(Time.fixedDeltaTime);
        }

        protected override void OnDestroy()
        {
            StateMachine?.Stop();
            base.OnDestroy();
        }

        /// <summary>
        /// 强制切换状态
        /// </summary>
        public void ChangeState(TState newState)
        {
            StateMachine?.ChangeState(newState);
        }

        /// <summary>
        /// 检查当前状态
        /// </summary>
        public bool IsInState(TState state) => StateMachine?.IsInState(state) ?? false;
    }
}
