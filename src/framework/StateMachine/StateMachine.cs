using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 通用状态机（纯逻辑，非 MonoBehaviour）
    /// </summary>
    /// <typeparam name="TState">状态枚举类型</typeparam>
    /// <typeparam name="TOwner">拥有者类型</typeparam>
    public class StateMachine<TState, TOwner>
        where TState : Enum
        where TOwner : class
    {
        // 状态实例缓存
        readonly Dictionary<TState, IState<TOwner>> states = new();

        // 转换规则
        readonly List<StateTransition<TState, TOwner>> transitions = new();
        readonly List<AnyStateTransition<TState, TOwner>> anyStateTransitions = new();

        // 当前状态
        TState currentState;
        TState previousState;
        IState<TOwner> currentStateInstance;
        TOwner owner;

        // 状态持续时间
        float stateTime;

        // 事件
        public event Action<TState, TState> OnStateChanged;
        public event Action<TState> OnStateEntered;
        public event Action<TState> OnStateExited;

        // 属性
        public TState CurrentState => currentState;
        public TState PreviousState => previousState;
        public float StateTime => stateTime;
        public bool IsRunning { get; private set; }
        public TOwner Owner => owner;
        public int RegisteredStateCount => states.Count;
        public int TransitionCount => transitions.Count + anyStateTransitions.Count;

        /// <summary>
        /// 初始化状态机
        /// </summary>
        /// <param name="owner">拥有者对象</param>
        /// <param name="initialState">初始状态</param>
        /// <param name="autoRegisterStates">是否自动注册状态（通过反射）</param>
        public void Initialize(TOwner owner, TState initialState, bool autoRegisterStates = true)
        {
            this.owner = owner;
            currentState = initialState;
            previousState = initialState;

            if (autoRegisterStates)
            {
                AutoRegisterStates();
            }

            IsRunning = true;
            EnterState(currentState);
        }

        /// <summary>
        /// 自动注册状态（通过反射查找实现了 IState 的类）
        /// </summary>
        void AutoRegisterStates()
        {
            var stateType = typeof(TState);
            var stateInterfaceType = typeof(IState<TOwner>);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("Unity"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (!stateInterfaceType.IsAssignableFrom(type)) continue;

                        var bindingAttr = type.GetCustomAttribute<StateBindingAttribute>();
                        if (bindingAttr != null && bindingAttr.StateEnumType == stateType)
                        {
                            var stateEnum = (TState)bindingAttr.StateValue;
                            var instance = (IState<TOwner>)Activator.CreateInstance(type);
                            RegisterState(stateEnum, instance);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }
        }

        /// <summary>
        /// 手动注册状态
        /// </summary>
        public void RegisterState(TState state, IState<TOwner> stateInstance)
        {
            states[state] = stateInstance;
        }

        /// <summary>
        /// 注册泛型状态（自动创建实例）
        /// </summary>
        public void RegisterState<TStateImpl>(TState state) where TStateImpl : IState<TOwner>, new()
        {
            states[state] = new TStateImpl();
        }

        /// <summary>
        /// 添加状态转换
        /// </summary>
        public void AddTransition(TState from, TState to, Func<TOwner, bool> condition, int priority = 0)
        {
            transitions.Add(new StateTransition<TState, TOwner>(from, to, condition, priority));
        }

        /// <summary>
        /// 添加任意状态转换
        /// </summary>
        public void AddAnyTransition(TState to, Func<TOwner, bool> condition, int priority = 100)
        {
            anyStateTransitions.Add(new AnyStateTransition<TState, TOwner>(to, condition, priority));
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;

            stateTime += deltaTime;
            CheckTransitions();
            currentStateInstance?.OnUpdate(owner, deltaTime);
        }

        /// <summary>
        /// 固定更新
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!IsRunning) return;
            currentStateInstance?.OnFixedUpdate(owner, fixedDeltaTime);
        }

        void CheckTransitions()
        {
            foreach (var transition in anyStateTransitions)
            {
                if (EqualityComparer<TState>.Default.Equals(transition.ToState, currentState))
                    continue;

                if (transition.Evaluate(owner))
                {
                    ChangeState(transition.ToState);
                    return;
                }
            }

            foreach (var transition in transitions)
            {
                if (!EqualityComparer<TState>.Default.Equals(transition.FromState, currentState))
                    continue;

                if (transition.Evaluate(owner))
                {
                    ChangeState(transition.ToState);
                    return;
                }
            }
        }

        /// <summary>
        /// 强制切换状态
        /// </summary>
        public void ChangeState(TState newState)
        {
            if (!IsRunning) return;
            if (EqualityComparer<TState>.Default.Equals(newState, currentState)) return;

            ExitState(currentState);

            previousState = currentState;
            currentState = newState;
            stateTime = 0f;

            OnStateChanged?.Invoke(previousState, currentState);

            EnterState(currentState);
        }

        void EnterState(TState state)
        {
            if (states.TryGetValue(state, out var stateInstance))
            {
                currentStateInstance = stateInstance;
                currentStateInstance.OnEnter(owner);
            }
            else
            {
                currentStateInstance = null;
                Debug.LogWarning($"[StateMachine] State {state} not registered");
            }

            OnStateEntered?.Invoke(state);
        }

        void ExitState(TState state)
        {
            currentStateInstance?.OnExit(owner);
            OnStateExited?.Invoke(state);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            ExitState(currentState);
            IsRunning = false;
        }

        public void Resume()
        {
            if (IsRunning) return;
            IsRunning = true;
            EnterState(currentState);
        }

        public bool IsInState(TState state) =>
            EqualityComparer<TState>.Default.Equals(currentState, state);

        public IState<TOwner> GetStateInstance(TState state) =>
            states.TryGetValue(state, out var instance) ? instance : null;

        public T GetStateInstance<T>(TState state) where T : class, IState<TOwner> =>
            GetStateInstance(state) as T;
    }
}
