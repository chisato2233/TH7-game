using System;
using System.Collections.Generic;

namespace GameFramework
{
    public class EventSystem : IGameSystem
    {
        public int Priority => 0;

        private Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        public void OnInit() { }

        public void OnUpdate(float deltaTime) { }

        public void OnShutdown()
        {
            eventTable.Clear();
        }

        public void Subscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
                eventTable[eventType] = Delegate.Combine(existingDelegate, listener);
            else
                eventTable[eventType] = listener;
        }

        public void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
            {
                Delegate newDelegate = Delegate.Remove(existingDelegate, listener);
                
                if (newDelegate == null)
                    eventTable.Remove(eventType);
                else
                    eventTable[eventType] = newDelegate;
            }
        }

        public void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (eventTable.TryGetValue(eventType, out Delegate existingDelegate))
            {
                Action<T> callback = existingDelegate as Action<T>;
                callback?.Invoke(eventData);
            }
        }

        public void ClearListeners<T>() where T : struct
        {
            Type eventType = typeof(T);
            if (eventTable.ContainsKey(eventType))
                eventTable.Remove(eventType);
        }

        public void ClearAllListeners()
        {
            eventTable.Clear();
        }
    }
}

