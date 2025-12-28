# Framework 模块

> 游戏核心框架：系统管理、事件、上下文、响应式数据

## 目录结构

```
framework/
├── Core/                # 核心入口
│   ├── GameEntry.cs     # 游戏入口单例，管理 IGameSystem
│   ├── IGameSystem.cs   # 系统接口
│   └── GameBehaviour.cs # MonoBehaviour 扩展
├── Event/               # 事件系统
│   ├── EventSystem.cs   # 发布订阅
│   ├── AutoSubscribeAttribute.cs
│   └── AutoSubscribeProcessor.cs
└── Context/             # 上下文 + 响应式
    ├── GameContext.cs   # 上下文基类
    ├── ContextSystem.cs # 上下文管理
    ├── Reactive.cs      # 响应式数据
    └── Subscription.cs  # 统一订阅句柄
```

---

## Subscription (统一句柄)

所有订阅返回统一的 `Subscription`：

```csharp
Subscription sub = reactive.Watch(callback);
Subscription sub = eventSystem.Subscribe(callback);  // 通过 Listen
sub.Dispose();  // 取消订阅
```

### SubscriptionList

```csharp
var list = new SubscriptionList();
list += sub1;
list += sub2;
list.Dispose();  // 批量清理
```

---

## Reactive

```csharp
var gold = new Reactive<int>(100);
gold.Watch(v => Debug.Log(v));
gold.Value = 200;  // 触发回调
```

| 方法 | 说明 |
|------|------|
| `Watch` | 监听变化 |
| `WatchImmediate` | 监听并立即执行 |
| `WatchOnce` | 只监听一次 |
| `SetSilent` | 静默设置 |

---

## EventSystem

```csharp
// 事件定义 (struct)
public struct PlayerDiedEvent { public int Id; }

// 发布订阅
events.Subscribe<PlayerDiedEvent>(handler);
events.Publish(new PlayerDiedEvent { Id = 1 });
```

---

## GameBehaviour

继承获得自动生命周期管理：

```csharp
public class MyUI : GameBehaviour
{
    protected override void Start()
    {
        base.Start();
        Listen<PlayerDiedEvent>(OnDied);       // 事件
        Listen(gold, OnGoldChanged);            // Reactive
        ListenImmediate(gold, OnGoldChanged);   // 立即执行
    }

    [AutoSubscribe<ResourceChangedEvent>]  // 自动订阅
    void OnResourceChanged(ResourceChangedEvent e) { }
}
```

---

## Easy Save 3 集成

Reactive<T> 已与 ES3 集成，支持自动序列化：

```csharp
// 定义数据类（公共字段自动存储）
public class PlayerState
{
    public string Name;
    public Reactive<int> Day = new(1);    // Reactive 自动存储 Value
    public Reactive<int> Gold = new(0);
}

public class SessionContext : GameContext
{
    public PlayerState State = new();
    public List<TownData> Towns = new();
}

// 存档
ES3.Save("session", sessionContext, "save.es3");

// 读档
ES3.LoadInto("session", sessionContext, "save.es3");

// 检查
if (ES3.FileExists("save.es3")) { ... }
if (ES3.KeyExists("session", "save.es3")) { ... }
```

**序列化规则**：
- 公共字段：自动存储
- 私有字段：不存储（除非加 `[SerializeField]`）
- `[ES3NonSerializable]`：强制排除
- `Reactive<T>`：只存储 `.Value`，不存储订阅者

---

## 使用建议

| 场景 | 方案 |
|------|------|
| UI 数据绑定 | `Listen(reactive, callback)` |
| 跨系统通信 | `[AutoSubscribe<T>]` |
| 数据持久化 | `ES3.Save` / `ES3.LoadInto` |
