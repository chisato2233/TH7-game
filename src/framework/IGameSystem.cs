namespace GameFramework
{
    public interface IGameSystem
    {
        int Priority { get; }
        void OnInit();
        void OnUpdate(float deltaTime);
        void OnShutdown();
    }
}

