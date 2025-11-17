using UnityEngine;

namespace GameFramework
{
    public abstract class BaseProcedure
    {
        public virtual void OnEnter()
        {
            Debug.Log($"进入流程: {GetType().Name}");
        }

        public virtual void OnUpdate(float deltaTime) { }

        public virtual void OnExit()
        {
            Debug.Log($"退出流程: {GetType().Name}");
        }
    }
}

