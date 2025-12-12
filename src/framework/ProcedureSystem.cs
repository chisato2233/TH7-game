using System;
using System.Collections.Generic;

namespace GameFramework
{
    public class ProcedureSystem : IGameSystem
    {
        public int Priority => 10;

        private Dictionary<Type, BaseProcedure> procedures = new Dictionary<Type, BaseProcedure>();
        private BaseProcedure currentProcedure;
        private BaseProcedure nextProcedure;

        public void OnInit() { }

        public void OnUpdate(float deltaTime)
        {
            if (nextProcedure != null)
            {
                if (currentProcedure != null)
                    currentProcedure.OnExit();

                currentProcedure = nextProcedure;
                nextProcedure = null;
                currentProcedure.OnEnter();
            }

            if (currentProcedure != null)
                currentProcedure.OnUpdate(deltaTime);
        }

        public void OnShutdown()
        {
            if (currentProcedure != null)
            {
                currentProcedure.OnExit();
                currentProcedure = null;
            }

            procedures.Clear();
        }

        public void RegisterProcedure<T>(T procedure) where T : BaseProcedure
        {
            Type type = typeof(T);
            if (procedures.ContainsKey(type))
                return;

            procedures.Add(type, procedure);
        }

        public void ChangeProcedure<T>() where T : BaseProcedure
        {
            Type type = typeof(T);
            if (!procedures.TryGetValue(type, out BaseProcedure procedure))
                return;

            if (currentProcedure != null && currentProcedure.GetType() == type)
                return;

            nextProcedure = procedure;
        }

        public BaseProcedure GetCurrentProcedure()
        {
            return currentProcedure;
        }

        public Type GetCurrentProcedureType()
        {
            return currentProcedure?.GetType();
        }

        public bool IsInProcedure<T>() where T : BaseProcedure
        {
            return currentProcedure != null && currentProcedure.GetType() == typeof(T);
        }
    }
}

