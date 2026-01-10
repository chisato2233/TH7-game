using System;

namespace GameFramework
{
    /// <summary>
    /// 状态转换条件
    /// </summary>
    public class StateTransition<TState, TOwner>
        where TState : Enum
        where TOwner : class
    {
        public TState FromState { get; }
        public TState ToState { get; }
        public Func<TOwner, bool> Condition { get; }
        public int Priority { get; }

        public StateTransition(TState from, TState to, Func<TOwner, bool> condition, int priority = 0)
        {
            FromState = from;
            ToState = to;
            Condition = condition;
            Priority = priority;
        }

        public bool Evaluate(TOwner owner) => Condition?.Invoke(owner) ?? false;
    }

    /// <summary>
    /// 任意状态转换（从任何状态都可以触发）
    /// </summary>
    public class AnyStateTransition<TState, TOwner>
        where TState : Enum
        where TOwner : class
    {
        public TState ToState { get; }
        public Func<TOwner, bool> Condition { get; }
        public int Priority { get; }

        public AnyStateTransition(TState to, Func<TOwner, bool> condition, int priority = 100)
        {
            ToState = to;
            Condition = condition;
            Priority = priority;
        }

        public bool Evaluate(TOwner owner) => Condition?.Invoke(owner) ?? false;
    }
}
