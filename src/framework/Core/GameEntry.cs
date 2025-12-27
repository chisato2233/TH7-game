using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework
{
    public class GameEntry : MonoBehaviour
    {
        private static GameEntry instance;

        public static GameEntry Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[GameEntry]");
                    instance = go.AddComponent<GameEntry>();
                    DontDestroyOnLoad(go);
                    instance.EnsureInitialized();
                }
                return instance;
            }
        }

        private Dictionary<Type, IGameSystem> systems = new();
        private List<IGameSystem> updateSystems = new();
        private bool isInitialized;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
        }

        /// <summary>
        /// 确保核心系统已初始化（懒加载）
        /// </summary>
        void EnsureInitialized()
        {
            if (isInitialized) return;

            // 注册核心系统
            RegisterSystemInternal(new EventSystem());
            RegisterSystemInternal(new ContextSystem());

            // 初始化所有系统
            foreach (var system in updateSystems)
                system.OnInit();

            isInitialized = true;
            Debug.Log("[GameEntry] 核心系统初始化完成");
        }

        void RegisterSystemInternal(IGameSystem system)
        {
            var type = system.GetType();
            if (systems.ContainsKey(type)) return;

            systems.Add(type, system);
            updateSystems.Add(system);
            updateSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void RegisterSystem<T>(T system) where T : IGameSystem
        {
            EnsureInitialized();

            var type = typeof(T);
            if (systems.ContainsKey(type)) return;

            systems.Add(type, system);
            updateSystems.Add(system);
            updateSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            system.OnInit();
        }

        public T GetSystem<T>() where T : class, IGameSystem
        {
            EnsureInitialized();
            return systems.TryGetValue(typeof(T), out var system) ? system as T : null;
        }

        /// <summary>
        /// 兼容旧代码，现在是空操作
        /// </summary>
        [Obsolete("系统会自动初始化，无需手动调用")]
        public void InitAllSystems() { }

        void Update()
        {
            if (!isInitialized) return;

            var deltaTime = Time.deltaTime;
            foreach (var system in updateSystems)
                system.OnUpdate(deltaTime);
        }

        void OnDestroy()
        {
            if (instance != this) return;

            Debug.Log("[GameEntry] 关闭");

            foreach (var system in updateSystems)
                system.OnShutdown();

            systems.Clear();
            updateSystems.Clear();
            instance = null;
        }

        void OnApplicationQuit()
        {
            if (instance == this)
                instance = null;
        }
    }
}
