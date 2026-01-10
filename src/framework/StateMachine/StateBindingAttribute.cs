using System;

namespace GameFramework
{
    /// <summary>
    /// 状态绑定特性
    /// 用于将状态类与枚举值绑定，支持自动注册
    /// </summary>
    /// <example>
    /// [StateBinding(typeof(HeroState), HeroState.Idle)]
    /// public class HeroIdleState : StateBase&lt;Hero&gt; { }
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StateBindingAttribute : Attribute
    {
        public Type StateEnumType { get; }
        public object StateValue { get; }

        public StateBindingAttribute(Type stateEnumType, object stateValue)
        {
            StateEnumType = stateEnumType;
            StateValue = stateValue;
        }
    }
}
