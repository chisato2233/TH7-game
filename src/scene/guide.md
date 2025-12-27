# Scene 模块

> 场景控制器：管理场景初始化和上下文创建

## 文件说明

| 文件 | 职责 |
|------|------|
| `BootController.cs` | 启动场景，初始化系统，播放动画 |
| `MainMenuController.cs` | 主菜单，创建 SessionContext |
| `WorldSceneController.cs` | 世界场景，创建 WorldContext |

---

## 场景流程

```
BootScene                    MainMenuScene              WorldScene
    │                             │                          │
BootController              MainMenuController      WorldSceneController
    │                             │                          │
初始化 GameEntry            StartNewGame()            获取 SessionContext
注册 EventSystem                 │                          │
注册 ContextSystem          创建 SessionContext       创建 WorldContext
    │                             │                          │
播放启动动画               LoadScene(World)          Setup(MapManager)
    │                                                        │
LoadScene(MainMenu)                                     探索开始
```

---

## BootController

启动场景控制器，必须在 Build Settings 第一位。

```csharp
// 系统初始化
var entry = GameEntry.Instance;
entry.RegisterSystem(new EventSystem());
entry.RegisterSystem(new ContextSystem());
entry.InitAllSystems();
```

### 启动动画

使用 DOTween 实现：
1. Logo 淡入 + 缩放弹出
2. 标题文字淡入 + 上移
3. 副标题淡入
4. 整体淡出
5. 加载主菜单

---

## MainMenuController

```csharp
// 开始新游戏
public void StartNewGame()
{
    var session = contextSystem.Root.CreateChild<SessionContext>();
    session.StartNewSession("Player");
    SceneManager.LoadScene(worldScene);
}

// 继续游戏
public void ContinueGame(string saveId) { ... }

// 退出
public void QuitGame() { ... }
```

---

## WorldSceneController

```csharp
void Start()
{
    var session = contextSystem.Root.GetChild<SessionContext>();
    worldContext = session.CreateChild<WorldContext>();
    worldContext.Setup(mapManager);
}

void OnDestroy()
{
    session?.DisposeChild<WorldContext>();
}
```

---

## 调试模式

直接运行非 Boot 场景时，需要手动初始化系统：

```csharp
// 在 GameSystem.cs 或场景控制器中
if (GameEntry.Instance.GetSystem<EventSystem>() == null)
{
    // 初始化系统...
}
```
