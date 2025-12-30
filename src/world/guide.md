# World 系统指南

本模块实现世界地图探索、英雄控制、回合管理。

---

## 架构概览

```
WorldSceneController (MonoBehaviour)
    │
    ├── WorldContext (探索上下文)
    │   └── MapManager (地图管理)
    │
    ├── WorldTurnManager (回合管理)
    │   ├── ActionExecutor (行动执行)
    │   └── IActionProvider (行动来源)
    │       ├── PlayerActionProvider (玩家输入)
    │       └── AIActionProvider (AI 决策，未来)
    │
    └── WorldInputController (输入处理)
```

---

## 目录结构

```
src/world/
├── action/
│   ├── HeroAction.cs       # 英雄行动命令
│   ├── ActionResult.cs     # 行动执行结果
│   └── ActionExecutor.cs   # 行动执行器
├── provider/
│   ├── IActionProvider.cs      # 行动提供者接口
│   ├── PlayerActionProvider.cs # 玩家输入提供者
│   └── IPathfinder.cs          # 寻路接口 + 实现
├── hero/
│   └── HeroData.cs         # 英雄数据
├── input/
│   └── WorldInputController.cs # 输入控制器
├── WorldTurnManager.cs     # 回合管理器
└── guide.md
```

---

## 核心类说明

### HeroData
英雄数据，支持序列化存档。

```csharp
var hero = new HeroData("hero_01", "Knight", new Vector3Int(5, 5, 0));
hero.MovementPoints.Value;  // 当前移动力
hero.CanAct;                // 是否还能行动
hero.ConsumeMovement(5);    // 消耗移动力
hero.ResetMovement();       // 重置移动力（新回合）
```

### HeroAction
英雄行动命令基类，所有行动继承自此。

| 行动类型 | 类名 | 说明 |
|----------|------|------|
| Move | MoveAction | 移动到目标格子 |
| EnterTown | EnterTownAction | 进入城镇 |
| Attack | AttackAction | 攻击目标 |
| PickUp | PickUpAction | 拾取物品 |
| Wait | WaitAction | 等待（跳过） |
| EndTurn | EndTurnAction | 结束回合 |

### IActionProvider
行动提供者接口，定义如何获取英雄的下一个行动。

```csharp
public interface IActionProvider
{
    bool RequiresInput { get; }
    void RequestAction(HeroData hero, WorldContext ctx, Action<HeroAction> callback);
    void CancelRequest();
}
```

### PlayerActionProvider
玩家输入转换为 HeroAction。

```csharp
// 绑定输入
provider.BindInputActions(clickAction, rightClickAction, endTurnAction);

// 请求行动（非阻塞）
provider.RequestAction(hero, context, action => {
    // 玩家点击后回调
});
```

### WorldTurnManager
回合管理器（GameBehaviour），控制游戏流程。使用 EventSystem 发布事件。

```csharp
// WorldTurnManager 现在是 GameBehaviour，在 Inspector 中配置
// 在代码中初始化：
turnManager.Initialize(worldContext, actionExecutor);
turnManager.RegisterProvider(0, playerProvider);  // 玩家 0

turnManager.StartDay();  // 开始新的一天
turnManager.Resume();    // 从城镇/战斗返回后恢复

// 通过 EventSystem 订阅事件（使用 [Subscribe] 特性）：
[Subscribe]
void OnHeroTurnStarted(HeroTurnStartedEvent e) { }

[Subscribe]
void OnDayEnded(DayEndedEvent e) { }
```

---

## 回合流程

```
StartDay()
    │
    ├── 重置所有英雄移动力
    │
    └── StartPlayerTurn(0)  // 玩家回合
        │
        └── StartHeroTurn(hero)
            │
            ├── provider.RequestAction()  // 等待输入
            │       │
            │       └── OnActionReceived(action)
            │           │
            │           ├── executor.Execute()
            │           │       │
            │           │       └── OnActionCompleted()
            │           │           │
            │           │           ├── hero.CanAct? → ContinueHeroTurn()
            │           │           └── !hero.CanAct → EndHeroTurn()
            │           │
            │           ├── EnterTown → 暂停，打开城镇
            │           └── EndTurn → EndDay()
            │
            └── EndHeroTurn()
                │
                └── NextHero() 或 NextPlayer() 或 EndDay()
```

---

## Unity 配置

### 1. 创建 WorldTurnManager

1. 在 WorldScene 中创建空 GameObject `WorldTurnManager`
2. 添加 `WorldTurnManager` 组件
3. 配置:
   - Scene Controller: 拖入 WorldSceneController 对象
   - Debug Mode: 可选开启调试日志

### 2. 创建 WorldInputController

1. 在 WorldScene 中创建空 GameObject `WorldInput`
2. 添加 `WorldInputController` 组件
3. 注意：摄像头控制由 Cinemachine 处理，此组件只处理游戏输入

### 3. 配置 WorldSceneController

在 WorldController 对象上:

| 字段 | 拖入 |
|------|------|
| Turn Manager | WorldTurnManager 对象 |
| Map Manager | MapManager 对象 |
| Input Controller | WorldInput 对象 |
| World Camera | Main Camera |
| Town Panel | TownPanelUI |
| Resource Bar | ResourceBarUI |
| Town Config Database | TownConfigDatabase.asset |
| Unit Config Database | UnitConfigDatabase.asset |

### 4. 配置 Cinemachine（摄像头）

1. 创建 Cinemachine Virtual Camera
2. 配置 Body: Framing Transposer 或 Cinemachine Confiner
3. 配置 Input Provider 处理 WASD 移动和滚轮缩放

### 5. Input System（可选）

如果使用 InputActionAsset:

1. 创建 `Assets/data/WorldInputActions.inputactions`
2. 添加 Action Map: `World`
3. 添加 Actions:
   - Click (Button): `<Mouse>/leftButton`
   - RightClick (Button): `<Mouse>/rightButton`
   - EndTurn (Button): `<Keyboard>/e`

如果不配置 InputActionAsset，系统会自动创建默认输入。

---

## 扩展指南

### 添加新行动类型

1. 在 `HeroActionType` 枚举添加新类型
2. 创建新类继承 `HeroAction`
3. 在 `ActionExecutor.ExecuteCoroutine` 添加处理分支
4. 在 `PlayerActionProvider.ProcessClick` 添加创建逻辑

### 添加 AI 玩家

```csharp
public class AIActionProvider : IActionProvider
{
    public bool RequiresInput => false;  // AI 不需要等待

    public void RequestAction(HeroData hero, WorldContext ctx, Action<HeroAction> callback)
    {
        var action = DecideAction(hero, ctx);  // AI 决策
        callback(action);  // 立即返回
    }
}

// 注册 AI 玩家
turnManager.RegisterProvider(1, new AIActionProvider());
```

### 添加网络玩家

```csharp
public class NetworkActionProvider : IActionProvider
{
    public void RequestAction(HeroData hero, WorldContext ctx, Action<HeroAction> callback)
    {
        // 发送请求到服务器
        // 等待服务器返回行动
        // callback(receivedAction);
    }
}
```
