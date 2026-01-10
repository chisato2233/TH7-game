namespace GameFramework
{
    /// <summary>
    /// 状态接口
    /// </summary>
    /// <typeparam name="TOwner">状态机拥有者类型</typeparam>
    public interface IState<TOwner> where TOwner : class
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void OnEnter(TOwner owner);

        /// <summary>
        /// 每帧更新
        /// </summary>
        void OnUpdate(TOwner owner, float deltaTime);

        /// <summary>
        /// 固定更新（物理）
        /// </summary>
        void OnFixedUpdate(TOwner owner, float fixedDeltaTime);

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        void OnExit(TOwner owner);
    }

    /// <summary>
    /// 状态基类（提供默认空实现）
    /// </summary>
    public abstract class StateBase<TOwner> : IState<TOwner> where TOwner : class
    {
        public virtual void OnEnter(TOwner owner) { }
        public virtual void OnUpdate(TOwner owner, float deltaTime) { }
        public virtual void OnFixedUpdate(TOwner owner, float fixedDeltaTime) { }
        public virtual void OnExit(TOwner owner) { }
    }
}
