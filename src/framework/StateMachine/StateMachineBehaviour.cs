using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 状态机 MonoBehaviour 封装
    /// 适用于需要挂载到 GameObject 的场景
    /// </summary>
    /// <typeparam name="TState">状态枚举类型</typeparam>
    /// <typeparam name="TOwner">拥有者类型</typeparam>
    public abstract class StateMachineBehaviour<TState, TOwner> : MonoBehaviour
        where TState : Enum
        where TOwner : class
    {
        [Header("Debug Info")]
        [SerializeField] bool debugMode;
        [SerializeField] string currentStateName;
        [SerializeField] string previousStateName;
        [SerializeField] float stateTime;
        [SerializeField] bool isRunning;
        [SerializeField] int registeredStateCount;
        [SerializeField] int transitionCount;

        protected StateMachine<TState, TOwner> StateMachine { get; private set; }

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

        protected virtual void Awake()
        {
            StateMachine = new StateMachine<TState, TOwner>();

            if (debugMode)
            {
                StateMachine.OnStateChanged += (from, to) =>
                {
                    currentStateName = to.ToString();
                    Debug.Log($"[{GetType().Name}] {from} -> {to}");
                };
            }
        }

        protected virtual void Start()
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

        protected virtual void OnDestroy()
        {
            StateMachine?.Stop();
        }

        /// <summary>
        /// 强制切换状态
        /// </summary>
        public void ChangeState(TState newState)
        {
            StateMachine?.ChangeState(newState);
            currentStateName = StateMachine?.CurrentState.ToString();
        }

        /// <summary>
        /// 检查当前状态
        /// </summary>
        public bool IsInState(TState state) => StateMachine?.IsInState(state) ?? false;
    }
}
