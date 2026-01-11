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
│   ├── Hero.cs             # 英雄 MonoBehaviour
│   └── HeroData.cs         # 英雄数据
├── input/
│   └── WorldInputController.cs # 输入控制器
├── view/
│   ├── PathPreview.cs              # 路径预览
│   ├── HeroSelectionIndicator.cs   # 选中英雄指示器
│   └── SelectableHeroIndicator.cs  # 可选英雄高亮指示器
├── WorldTurnManager.cs     # 回合管理器
├── WorldEvents.cs          # 世界事件定义
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
玩家输入转换为 HeroAction。使用状态机管理交互流程。

**状态定义**：
```csharp
enum PlayerInputState
{
    Disabled,      // 禁用状态
    Idle,          // 空闲，等待玩家选择英雄
    HeroSelected,  // 已选中英雄，等待选择目标
    Executing      // 正在执行行动
}
```

**交互流程**：
```
Idle (显示可选英雄高亮)
  │
  ├── 左键点击可行动英雄 → HeroSelected (显示选中指示器、路径预览)
  │                           │
  │                           ├── 左键点击地面 → 移动命令 → Idle
  │                           ├── 左键点击城镇 → 进入城镇 → Idle
  │                           ├── 左键点击其他英雄 → 切换选中 → HeroSelected
  │                           └── 右键 → 取消选中 → Idle
  │
  └── 结束回合按钮 → 结束回合命令
```

**EventSystem 集成**：
PlayerActionProvider 通过 EventSystem 发布以下事件：
- `SelectableHeroesChangedEvent` - 可选英雄列表变化
- `HeroSelectedEvent` - 英雄被选中
- `HeroDeselectedEvent` - 英雄取消选中
- `PathPreviewUpdatedEvent` - 路径预览更新

```csharp
// 绑定输入
provider.BindInputActions(clickAction, rightClickAction, endTurnAction);

// 请求行动（非阻塞）
provider.RequestAction(null, context, action => {
    // 玩家操作后回调，action.Hero 包含执行行动的英雄
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
        └── WaitingForAction (状态机状态)
            │
            ├── provider.RequestAction(null, ...)  // 等待玩家选择英雄并行动
            │       │
            │       └── OnActionReceived(action)
            │           │
            │           ├── executor.Execute()
            │           │       │
            │           │       └── OnActionCompleted()
            │           │           │
            │           │           └── ContinuePlayerTurn()
            │           │               │
            │           │               ├── 还有可行动英雄 → WaitingForAction
            │           │               └── 无可行动英雄 → DayEnd
            │           │
            │           ├── EnterTown → Interacting (暂停，打开城镇)
            │           └── EndTurn → DayEnd
            │
            └── DayEnd
                │
                ├── 城镇产出
                ├── 推进日期
                └── StartDay() (新的一天)
```

**玩家自由选择英雄**：
- 回合管理器不再自动选中英雄
- 玩家可自由选择任意可行动的英雄
- 可选英雄会显示高亮指示器
- 英雄行动后如果还有移动力，玩家可继续操作该英雄或选择其他英雄

---

## 事件系统 (EventSystem)

World 模块使用 `GameFramework.EventSystem` 进行解耦通信。

### 事件列表

**回合相关事件**：
| 事件 | 说明 | 数据 |
|------|------|------|
| `TurnPhaseChangedEvent` | 回合阶段变化 | Phase, PreviousPhase |
| `HeroTurnStartedEvent` | 英雄回合开始 | Hero |
| `HeroTurnEndedEvent` | 英雄回合结束 | Hero |
| `DayEndedEvent` | 一天结束 | Day, Week, Month |
| `WeekStartedEvent` | 一周开始 | Week, Month |

**英雄行动事件**：
| 事件 | 说明 | 数据 |
|------|------|------|
| `ActionStartedEvent` | 行动开始 | Action |
| `ActionCompletedEvent` | 行动完成 | Action, Result |
| `HeroMovedEvent` | 英雄移动 | Hero, FromCell, ToCell |
| `EnterTownRequestedEvent` | 请求进入城镇 | Hero, Town |
| `BattleRequestedEvent` | 请求战斗 | Hero, Enemy |

**玩家输入事件**：
| 事件 | 说明 | 数据 |
|------|------|------|
| `SelectableHeroesChangedEvent` | 可选英雄列表变化 | Heroes |
| `HeroSelectedEvent` | 英雄被选中 | Hero |
| `HeroDeselectedEvent` | 英雄取消选中 | - |
| `PathPreviewUpdatedEvent` | 路径预览更新 | Path, CanReach |
| `MapClickedEvent` | 地图点击 | CellPosition, WorldPosition, IsRightClick |
| `EndTurnRequestedEvent` | 请求结束回合 | - |

### 订阅事件

使用 `[AutoSubscribe]` 特性自动订阅（需继承 GameBehaviour）：

```csharp
public class MyController : GameBehaviour
{
    [AutoSubscribe]
    void OnHeroSelected(HeroSelectedEvent e)
    {
        Debug.Log($"选中英雄: {e.Hero.HeroName}");
    }

    [AutoSubscribe]
    void OnSelectableHeroesChanged(SelectableHeroesChangedEvent e)
    {
        // 更新可选英雄高亮
        selectableIndicator?.UpdateSelectableHeroes(e.Heroes);
    }
}
```

### 发布事件

```csharp
var eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
eventSystem?.Publish(new HeroSelectedEvent { Hero = selectedHero });
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
| Path Preview | PathPreview 对象 |
| Selection Indicator | HeroSelectionIndicator 对象 |
| Selectable Hero Indicator | SelectableHeroIndicator 对象 |
| Town Config Database | TownConfigDatabase.asset |
| Unit Config Database | UnitConfigDatabase.asset |
| Hero Prefab | Hero.prefab |

### 4. 配置视图组件

**PathPreview** - 显示移动路径预览：
1. 创建空 GameObject `PathPreview`
2. 添加 `PathPreview` 组件
3. 配置线条颜色、宽度等

**HeroSelectionIndicator** - 显示选中英雄指示器：
1. 创建空 GameObject `HeroSelectionIndicator`
2. 添加 `HeroSelectionIndicator` 组件
3. 配置指示器颜色、大小、脉冲效果

**SelectableHeroIndicator** - 显示可选英雄高亮：
1. 创建空 GameObject `SelectableHeroIndicator`
2. 添加 `SelectableHeroIndicator` 组件
3. 配置高亮颜色、大小、脉冲效果

### 5. 配置 Cinemachine（摄像头）

1. 创建 Cinemachine Virtual Camera
2. 配置 Body: Framing Transposer 或 Cinemachine Confiner
3. 配置 Input Provider 处理 WASD 移动和滚轮缩放

### 6. Input System（可选）

如果使用 InputActionAsset:

1. 创建 `Assets/data/WorldInputActions.inputactions`
2. 添加 Action Map: `World`
3. 添加 Actions:
   - Click (Button): `<Mouse>/leftButton`
   - RightClick (Button): `<Mouse>/rightButton`
   - EndTurn (Button): `<Keyboard>/e`

如果不配置 InputActionAsset，系统会自动创建默认输入。

---

## Reactive 数据（UI 绑定）

World 模块提供以下 Reactive 数据供 UI 绑定，实现数据驱动的界面更新。

### 可绑定数据列表

| 来源 | 属性 | 类型 | 说明 |
|------|------|------|------|
| `SessionContext.Data` | `Day` | `Reactive<int>` | 当前天数 |
| `WorldTurnManager` | `CurrentPhase` | `Reactive<TurnPhase>` | 回合阶段（Idle/PlayerTurn/AITurn/TurnEnd）|
| `WorldTurnManager` | `ActiveHero` | `Reactive<Hero>` | 当前行动的英雄 |
| `PlayerActionProvider` | `SelectedHero` | `Reactive<Hero>` | 玩家选中的英雄 |
| `PlayerActionProvider` | `InputState` | `Reactive<PlayerInputState>` | 输入状态 |
| `Hero` | `CellPosition` | `Reactive<Vector3Int>` | 英雄位置 |
| `Hero` | `MovementPoints` | `Reactive<int>` | 剩余移动力 |

### UI 绑定示例

**显示当前天数**：
```csharp
public class DayDisplayUI : UIBehaviour
{
    [SerializeField] TextMeshProUGUI dayText;

    void Start()
    {
        var session = GetSessionContext();
        ListenImmediate(session.Data.Day, day => {
            int week = (day - 1) / 7 + 1;
            int month = (day - 1) / 28 + 1;
            dayText.text = $"第{month}月 第{week}周 第{day}天";
        });
    }
}
```

**显示回合阶段**：
```csharp
public class TurnPhaseUI : UIBehaviour
{
    [SerializeField] TextMeshProUGUI phaseText;
    [SerializeField] WorldTurnManager turnManager;

    void Start()
    {
        ListenImmediate(turnManager.CurrentPhase, phase => {
            phaseText.text = phase switch
            {
                TurnPhase.PlayerTurn => "玩家回合",
                TurnPhase.AITurn => "敌方回合",
                TurnPhase.TurnEnd => "回合结算",
                _ => ""
            };
        });
    }
}
```

**显示选中英雄信息**：
```csharp
public class SelectedHeroInfoUI : UIBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI heroNameText;
    [SerializeField] TextMeshProUGUI movementText;

    PlayerActionProvider playerProvider;
    SubscriptionList subs = new();

    public void Bind(PlayerActionProvider provider)
    {
        playerProvider = provider;

        // 监听选中英雄变化
        subs += ListenImmediate(provider.SelectedHero, OnHeroChanged);
    }

    void OnHeroChanged(Hero hero)
    {
        if (hero == null)
        {
            panel.SetActive(false);
            return;
        }

        panel.SetActive(true);
        heroNameText.text = hero.HeroName;

        // 监听英雄移动力变化
        subs += ListenImmediate(hero.MovementPoints, mp => {
            movementText.text = $"移动力: {mp}/{hero.MaxMovementPoints}";
        });
    }

    protected override void OnDestroy()
    {
        subs.Dispose();
        base.OnDestroy();
    }
}
```

**监听输入状态**：
```csharp
public class InputStateIndicatorUI : UIBehaviour
{
    [SerializeField] Image stateIcon;
    [SerializeField] Sprite idleSprite;
    [SerializeField] Sprite selectedSprite;
    [SerializeField] Sprite executingSprite;

    public void Bind(PlayerActionProvider provider)
    {
        ListenImmediate(provider.InputState, state => {
            stateIcon.sprite = state switch
            {
                PlayerInputState.Idle => idleSprite,
                PlayerInputState.HeroSelected => selectedSprite,
                PlayerInputState.Executing => executingSprite,
                _ => null
            };
            stateIcon.enabled = stateIcon.sprite != null;
        });
    }
}
```

### 注意事项

- 使用 `ListenImmediate` 会立即触发一次回调，适合初始化 UI
- 使用 `Listen` 只在值变化时触发
- 记得在 `OnDestroy` 中释放订阅（`SubscriptionList.Dispose()`）
- GameBehaviour/UIBehaviour 的 Listen 方法会自动管理订阅生命周期

---

## World UI 组件配置（Modern UI Pack）

World 模块提供以下 UI 脚本，位于 `src/ui/World/`。各组件支持 `autoBindOnStart`，自动从 SessionContext 获取数据绑定。

### UI 组件列表

| 组件 | 脚本 | 绑定数据 |
|------|------|----------|
| 天数显示 | `DayDisplayUI.cs` | `SessionContext.Data.Day` |
| 回合阶段 | `TurnPhaseUI.cs` | `WorldTurnManager.CurrentPhase` |
| 选中英雄 | `SelectedHeroInfoUI.cs` | `PlayerActionProvider.SelectedHero` |

### Unity 配置步骤

#### 1. 创建 Canvas

在 WorldScene 中创建 Canvas（或使用现有的）：
- Render Mode: `Screen Space - Overlay`
- UI Scale Mode: `Scale With Screen Size` (1920x1080)

#### 2. 天数显示（DayDisplayUI）

```
DayDisplay (DayDisplayUI)
├── DayText (TextMeshProUGUI)
├── WeekText (TextMeshProUGUI, 可选)
└── MonthText (TextMeshProUGUI, 可选)
```

配置：勾选 `Auto Bind On Start` 即可自动绑定。

#### 3. 回合阶段（TurnPhaseUI）

```
TurnPhase (TurnPhaseUI)
└── PhaseText (TextMeshProUGUI)
```

需要手动绑定（因为需要 TurnManager 引用）：
```csharp
// 在 WorldSceneController.SetupUI() 中
turnPhaseUI.Bind(turnManager);
```

#### 4. 选中英雄信息（SelectedHeroInfoUI）

```
SelectedHeroInfo (SelectedHeroInfoUI)
└── InfoContainer (GameObject)
    ├── HeroPortrait (Image)
    ├── HeroNameText (TextMeshProUGUI)
    └── MovementText (TextMeshProUGUI)
```

需要手动绑定：
```csharp
// 在 WorldSceneController.SetupUI() 中
selectedHeroInfoUI.Bind(playerProvider);
```

### 示例布局

```
┌─────────────────────────────────────────────┐
│  [Day 1]                        [Gold:1000] │ <- 顶部栏
├─────────────────────────────────────────────┤
│                                             │
│              (游戏世界)                       │
│                                             │
├─────────────────────────────────────────────┤
│  ┌──────────┐                               │
│  │ [头像]   │  Hero Name                    │ <- 选中英雄信息
│  │          │  MP: 10/10     [Your Turn]    │ <- 回合阶段
│  └──────────┘                               │
└─────────────────────────────────────────────┘
```

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
