using System;
using System.Collections.Generic;

namespace GameFramework
{
    /// <summary>
    /// 层级状态机（支持子状态机）
    /// </summary>
    /// <typeparam name="TState">状态枚举类型</typeparam>
    /// <typeparam name="TOwner">拥有者类型</typeparam>
    public class HierarchicalStateMachine<TState, TOwner> : StateMachine<TState, TOwner>
        where TState : Enum
        where TOwner : class
    {
        // 子状态机映射
        readonly Dictionary<TState, object> subStateMachines = new();

        /// <summary>
        /// 注册子状态机
        /// </summary>
        public void RegisterSubStateMachine<TSubState>(TState parentState, StateMachine<TSubState, TOwner> subMachine)
            where TSubState : Enum
        {
            subStateMachines[parentState] = subMachine;
        }

        /// <summary>
        /// 获取子状态机
        /// </summary>
        public StateMachine<TSubState, TOwner> GetSubStateMachine<TSubState>(TState parentState)
            where TSubState : Enum
        {
            return subStateMachines.TryGetValue(parentState, out var sub)
                ? sub as StateMachine<TSubState, TOwner>
                : null;
        }

        /// <summary>
        /// 更新（包括子状态机）
        /// </summary>
        public new void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // 更新当前状态的子状态机
            if (subStateMachines.TryGetValue(CurrentState, out var subMachine))
            {
                // 使用反射调用 Update（因为泛型类型不确定）
                var updateMethod = subMachine.GetType().GetMethod("Update");
                updateMethod?.Invoke(subMachine, new object[] { deltaTime });
            }
        }

        /// <summary>
        /// 固定更新（包括子状态机）
        /// </summary>
        public new void FixedUpdate(float fixedDeltaTime)
        {
            base.FixedUpdate(fixedDeltaTime);

            if (subStateMachines.TryGetValue(CurrentState, out var subMachine))
            {
                var fixedUpdateMethod = subMachine.GetType().GetMethod("FixedUpdate");
                fixedUpdateMethod?.Invoke(subMachine, new object[] { fixedDeltaTime });
            }
        }
    }
}
