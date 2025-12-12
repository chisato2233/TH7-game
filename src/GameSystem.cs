using UnityEngine;
using GameFramework;

public class GameSystem : MonoBehaviour
{
    [SerializeField] private bool autoInit = true;

    private void Awake()
    {
        if (autoInit)
            InitializeGame();
    }

    public void InitializeGame()
    {
        GameEntry entry = GameEntry.Instance;

        RegisterCoreSystems(entry);
        RegisterGameSystems(entry);

        entry.InitAllSystems();
    }

    private void RegisterCoreSystems(GameEntry entry)
    {
        entry.RegisterSystem(new EventSystem());
        entry.RegisterSystem(new ProcedureSystem());
    }

    private void RegisterGameSystems(GameEntry entry)
    {
        // TODO: 注册游戏特定系统
    }
}
