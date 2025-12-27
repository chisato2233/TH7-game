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
                    GameObject go = new GameObject("[GameEntry]");
                    instance = go.AddComponent<GameEntry>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<Type, IGameSystem> systems = new Dictionary<Type, IGameSystem>();
        private List<IGameSystem> updateSystems = new List<IGameSystem>();
        private bool isInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterSystem<T>(T system) where T : IGameSystem
        {
            Type type = typeof(T);
            if (systems.ContainsKey(type))
                return;

            systems.Add(type, system);
            updateSystems.Add(system);
            updateSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            if (isInitialized)
                system.OnInit();
        }

        public T GetSystem<T>() where T : class, IGameSystem
        {
            Type type = typeof(T);
            if (systems.TryGetValue(type, out IGameSystem system))
                return system as T;

            return null;
        }

        public void InitAllSystems()
        {
            if (isInitialized)
                return;

            Debug.Log("=== GameEntry 初始化开始 ===");
            
            foreach (var system in updateSystems)
            {
                system.OnInit();
            }

            isInitialized = true;
            Debug.Log("=== GameEntry 初始化完成 ===");
        }

        private void Update()
        {
            if (!isInitialized) return;

            float deltaTime = Time.deltaTime;
            foreach (var system in updateSystems)
            {
                system.OnUpdate(deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            Debug.Log("=== GameEntry 关闭 ===");
            
            foreach (var system in updateSystems)
            {
                system.OnShutdown();
            }

            systems.Clear();
            updateSystems.Clear();
            instance = null;
        }

        private void OnApplicationQuit()
        {
            if (instance == this)
                instance = null;
        }
    }
}

