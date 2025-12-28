using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameFramework;

namespace TH7
{
    public class MainMenuController : MonoBehaviour
    {
        const string SAVE_DIR = "saves/";

        [SerializeField] string worldScene = "WorldScene";

        // 响应式存档列表
        public ReactiveList<SaveSlotInfo> SaveSlots { get; } = new();

        void Awake()
        {
            RefreshSaveSlots();
        }

        public void StartNewGame()
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            // 清理旧的 Session（如果存在）
            if (contextSystem.Root.HasChild<SessionContext>())
                contextSystem.Root.DisposeChild<SessionContext>();

            var session = contextSystem.Root.CreateChild<SessionContext>();
            session.StartNewSession("Player");

            Debug.Log("[MainMenu] 创建新会话，加载世界场景");
            SceneManager.LoadScene(worldScene);
        }

        public void ContinueGame(string slotId)
        {
            var contextSystem = GameEntry.Instance.GetSystem<ContextSystem>();

            // 清理旧的 Session（如果存在）
            if (contextSystem.Root.HasChild<SessionContext>())
                contextSystem.Root.DisposeChild<SessionContext>();

            var session = contextSystem.Root.CreateChild<SessionContext>();
            session.LoadSession(slotId);

            Debug.Log($"[MainMenu] 加载存档 {slotId}，进入世界场景");
            SceneManager.LoadScene(worldScene);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 刷新存档列表
        public void RefreshSaveSlots()
        {
            var slots = LoadSaveSlots();
            SaveSlots.Reset(slots);
        }

        // 从磁盘加载存档信息
        SaveSlotInfo[] LoadSaveSlots()
        {
            if (!ES3.DirectoryExists(SAVE_DIR))
                return System.Array.Empty<SaveSlotInfo>();

            return ES3.GetFiles(SAVE_DIR)
                .Where(f => f.EndsWith(".es3"))
                .Select(f =>
                {
                    string slotId = f.Replace(".es3", "");
                    string path = $"{SAVE_DIR}{f}";
                    return new SaveSlotInfo
                    {
                        SlotId = slotId,
                        PlayerName = ES3.Load<string>("session.PlayerName", path, "Unknown"),
                        Day = ES3.Load<int>("session.Day.SavedValue", path, 1),
                        LastSaveTime = ES3.GetTimestamp(path)
                    };
                })
                .OrderByDescending(s => s.LastSaveTime)
                .ToArray();
        }

        // 检查是否有存档
        public bool HasAnySave() => SaveSlots.Count > 0;

        // 获取最新存档
        public SaveSlotInfo GetLatestSave() => SaveSlots.Count > 0 ? SaveSlots[0] : null;

        // 删除存档
        public void DeleteSave(string slotId)
        {
            string path = $"{SAVE_DIR}{slotId}.es3";
            if (ES3.FileExists(path))
            {
                ES3.DeleteFile(path);
                Debug.Log($"[MainMenu] 删除存档: {slotId}");
                RefreshSaveSlots(); // 刷新列表
            }
        }
    }

    // 存档槽信息
    public class SaveSlotInfo
    {
        public string SlotId;
        public string PlayerName;
        public int Day;
        public System.DateTime LastSaveTime;

        public string DisplayName => $"{PlayerName} - Day {Day}";
        public string TimeAgo => FormatTimeAgo(LastSaveTime);

        static string FormatTimeAgo(System.DateTime time)
        {
            var span = System.DateTime.Now - time;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return time.ToString("yyyy-MM-dd");
        }
    }
}
