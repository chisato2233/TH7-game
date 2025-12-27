using System;
using System.Collections.Generic;
using UnityEngine;
using TH7;

namespace GameFramework
{
    /// <summary>
    /// 游戏上下文基类
    /// 用于管理分层的游戏生命周期，如：GameContext -> SessionContext -> WorldContext/BattleContext
    /// </summary>
    public abstract class GameContext : IDisposable
    {
        public ContextState State { get; private set; } = ContextState.None;
        public GameContext Parent { get; private set; }
        public string Name => GetType().Name;

        private List<GameContext> children = new List<GameContext>();
        private bool isDisposed = false;

        /// <summary>
        /// 获取所有子上下文（只读）
        /// </summary>
        public IReadOnlyList<GameContext> Children => children;

        /// <summary>
        /// 初始化上下文
        /// </summary>
        internal void Initialize(GameContext parent = null)
        {
            if (State != ContextState.None)
            {
                Debug.LogWarning($"[Context] {Name} 已经初始化过，跳过");
                return;
            }

            Parent = parent;
            State = ContextState.Initializing;

            Debug.Log($"[Context] {Name} 初始化中...");
            OnInitialize();
            State = ContextState.Active;
            Debug.Log($"[Context] {Name} 初始化完成");
        }

        /// <summary>
        /// 创建子上下文
        /// </summary>
        public T CreateChild<T>() where T : GameContext, new()
        {
            if (State != ContextState.Active)
            {
                Debug.LogError($"[Context] {Name} 状态为 {State}，无法创建子上下文");
                return null;
            }

            var child = new T();
            children.Add(child);
            child.Initialize(this);
            return child;
        }

        /// <summary>
        /// 创建子上下文（带参数）
        /// </summary>
        public T CreateChild<T>(Action<T> setup) where T : GameContext, new()
        {
            if (State != ContextState.Active)
            {
                Debug.LogError($"[Context] {Name} 状态为 {State}，无法创建子上下文");
                return null;
            }

            var child = new T();
            setup?.Invoke(child);
            children.Add(child);
            child.Initialize(this);
            return child;
        }

        /// <summary>
        /// 销毁指定子上下文
        /// </summary>
        public void DisposeChild<T>() where T : GameContext
        {
            var child = GetChild<T>();
            if (child != null)
            {
                DisposeChild(child);
            }
        }

        /// <summary>
        /// 销毁指定子上下文
        /// </summary>
        public void DisposeChild(GameContext child)
        {
            if (child == null || !children.Contains(child))
                return;

            child.Dispose();
            children.Remove(child);
        }

        /// <summary>
        /// 获取子上下文
        /// </summary>
        public T GetChild<T>() where T : GameContext
        {
            foreach (var child in children)
            {
                if (child is T typed)
                    return typed;
            }
            return null;
        }

        /// <summary>
        /// 检查是否存在指定类型的子上下文
        /// </summary>
        public bool HasChild<T>() where T : GameContext
        {
            return GetChild<T>() != null;
        }

        /// <summary>
        /// 向上查找父上下文
        /// </summary>
        public T GetParent<T>() where T : GameContext
        {
            var current = Parent;
            while (current != null)
            {
                if (current is T typed)
                    return typed;
                current = current.Parent;
            }
            return null;
        }

        /// <summary>
        /// 获取根上下文
        /// </summary>
        public GameContext GetRoot()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// 暂停上下文
        /// </summary>
        public void Pause()
        {
            if (State != ContextState.Active)
                return;

            State = ContextState.Paused;
            OnPause();

            // 同时暂停所有子上下文
            foreach (var child in children)
            {
                child.Pause();
            }
        }

        /// <summary>
        /// 恢复上下文
        /// </summary>
        public void Resume()
        {
            if (State != ContextState.Paused)
                return;

            State = ContextState.Active;
            OnResume();

            // 同时恢复所有子上下文
            foreach (var child in children)
            {
                child.Resume();
            }
        }

        /// <summary>
        /// 每帧更新（仅Active状态调用）
        /// </summary>
        internal void Update(float deltaTime)
        {
            if (State != ContextState.Active)
                return;

            OnUpdate(deltaTime);

            // 更新所有子上下文（使用副本避免迭代中修改）
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (i < children.Count)
                    children[i].Update(deltaTime);
            }
        }

        /// <summary>
        /// 销毁上下文及所有子上下文
        /// </summary>
        public void Dispose()
        {
            if (isDisposed || State == ContextState.Disposed)
                return;

            isDisposed = true;
            State = ContextState.Disposing;
            Debug.Log($"[Context] {Name} 销毁中...");

            // 先销毁所有子上下文（逆序）
            for (int i = children.Count - 1; i >= 0; i--)
            {
                children[i].Dispose();
            }
            children.Clear();

            OnDispose();
            State = ContextState.Disposed;
            Debug.Log($"[Context] {Name} 已销毁");
        }

        #region 子类重写

        /// <summary>
        /// 初始化时调用
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 每帧更新
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// 暂停时调用
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 恢复时调用
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 销毁时调用
        /// </summary>
        protected virtual void OnDispose() { }

        #endregion
    }
}
