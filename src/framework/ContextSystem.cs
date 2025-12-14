using System;
using UnityEngine;

namespace GameFramework
{
    /// <summary>
    /// 根上下文 - 游戏全局上下文，生命周期贯穿整个游戏
    /// </summary>
    public class RootContext : GameContext
    {
        protected override void OnInitialize()
        {
            // 全局初始化：配置、设置等
        }

        protected override void OnDispose()
        {
            // 全局清理
        }
    }

    /// <summary>
    /// 上下文系统 - 管理游戏上下文的生命周期
    /// </summary>
    public class ContextSystem : IGameSystem
    {
        public int Priority => 5; // 高优先级，在事件系统之后

        private RootContext rootContext;

        /// <summary>
        /// 根上下文（全局）
        /// </summary>
        public RootContext Root => rootContext;

        public void OnInit()
        {
            rootContext = new RootContext();
            rootContext.Initialize();
        }

        public void OnUpdate(float deltaTime)
        {
            rootContext?.Update(deltaTime);
        }

        public void OnShutdown()
        {
            rootContext?.Dispose();
            rootContext = null;
        }

        #region 便捷方法

        /// <summary>
        /// 在根上下文创建子上下文
        /// </summary>
        public T CreateContext<T>() where T : GameContext, new()
        {
            return rootContext?.CreateChild<T>();
        }

        /// <summary>
        /// 在根上下文创建子上下文（带初始化）
        /// </summary>
        public T CreateContext<T>(Action<T> setup) where T : GameContext, new()
        {
            return rootContext?.CreateChild(setup);
        }

        /// <summary>
        /// 获取根上下文的子上下文
        /// </summary>
        public T GetContext<T>() where T : GameContext
        {
            return rootContext?.GetChild<T>();
        }

        /// <summary>
        /// 销毁根上下文的子上下文
        /// </summary>
        public void DisposeContext<T>() where T : GameContext
        {
            rootContext?.DisposeChild<T>();
        }

        /// <summary>
        /// 检查是否存在指定上下文
        /// </summary>
        public bool HasContext<T>() where T : GameContext
        {
            return rootContext?.HasChild<T>() ?? false;
        }

        #endregion
    }
}
