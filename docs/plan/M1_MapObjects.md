# M1: 地图物件系统 - 详细实现计划

> 目标：让世界地图变得可交互（资源拾取、矿场占领、每日产出）

---

## 一、目标检查清单

- [ ] 能在地图上放置资源堆、矿场
- [ ] 英雄移动到资源堆时自动拾取
- [ ] 英雄移动到矿场时自动占领
- [ ] 每日结算时矿场产出资源
- [ ] UI 显示资源变化

---

## 二、架构设计

### 2.1 目录结构

```
src/world/
├── mapobject/                    # [新增] 地图物件模块
│   ├── MapObject.cs              # 地图物件基类
│   ├── ResourcePile.cs           # 资源堆（一次性拾取）
│   ├── Mine.cs                   # 矿场（占领后产出）
│   ├── MapObjectConfig.cs        # 地图物件配置 (SO)
│   ├── MapObjectDatabase.cs      # 地图物件数据库 (SO)
│   └── guide.md                  # 模块指南
├── action/
│   ├── HeroAction.cs             # [已有] 添加 InteractAction
│   └── ActionExecutor.cs         # [修改] 处理物件交互
└── WorldEvents.cs                # [修改] 添加物件相关事件
```

### 2.2 类图

```
MapObject (MonoBehaviour, 基类)
├── MapObjectType Type            # 物件类型
├── Vector3Int CellPosition       # 格子坐标
├── int OwnerId                   # 所有者 (-1=中立/未占领)
├── bool IsCollected              # 是否已收集
├── Interact(Hero)                # 交互方法 (virtual)
└── GetInteractAction(Hero)       # 获取交互行动

ResourcePile : MapObject
├── ResourceType ResourceType     # 资源类型
├── int Amount                    # 资源数量
└── Interact() → 拾取资源，销毁物件

Mine : MapObject
├── ResourceType ResourceType     # 产出资源类型
├── int DailyOutput               # 日产量
├── Reactive<int> OwnerId         # 所有者（响应式）
└── Interact() → 占领矿场
```

### 2.3 数据流

```
设计时: 在场景中放置 MapObject 预制体
    ↓
运行时: WorldContext.RegisterMapObjects() 收集所有物件
    ↓
交互时: Hero 移动到物件格子 → ActionExecutor 触发 Interact
    ↓
结算时: WorldTurnManager.ProcessDayEnd() 遍历矿场计算产出
```

---

## 三、实现步骤

### 步骤 1: 创建 MapObject 基类

**文件**: `src/world/mapobject/MapObject.cs`

```csharp
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 地图物件基类
    /// </summary>
    public abstract class MapObject : GameBehaviour
    {
        [Header("Map Object")]
        [SerializeField] protected MapObjectType objectType;
        [SerializeField] protected Sprite icon;

        // 运行时数据
        public Vector3Int CellPosition { get; private set; }
        public MapObjectType ObjectType => objectType;
        public Sprite Icon => icon;
        public virtual int OwnerId => -1;
        public virtual bool IsCollected => false;

        // 坐标转换器（由 WorldContext 注入）
        System.Func<Vector3Int, Vector3> positionConverter;

        protected override void Start()
        {
            base.Start();
            // 从世界坐标计算格子坐标
            UpdateCellPosition();
        }

        public void SetPositionConverter(System.Func<Vector3Int, Vector3> converter)
        {
            positionConverter = converter;
            UpdateCellPosition();
        }

        void UpdateCellPosition()
        {
            // 由 MapManager 提供的逆转换，或使用默认计算
            CellPosition = Vector3Int.FloorToInt(transform.position);
        }

        /// <summary>
        /// 英雄与物件交互
        /// </summary>
        public abstract InteractionResult Interact(Hero hero, SessionContext session);

        /// <summary>
        /// 是否可以交互
        /// </summary>
        public virtual bool CanInteract(Hero hero) => !IsCollected;

        /// <summary>
        /// 获取交互后的行动结果类型
        /// </summary>
        public abstract HeroActionType GetInteractionType();
    }

    /// <summary>
    /// 交互结果
    /// </summary>
    public class InteractionResult
    {
        public bool Success;
        public string Message;
        public ResourceBundle ResourcesGained;
        public bool ShouldRemove;  // 交互后是否移除物件

        public static InteractionResult Fail(string msg) =>
            new() { Success = false, Message = msg };

        public static InteractionResult Ok(string msg = null, bool remove = false) =>
            new() { Success = true, Message = msg, ShouldRemove = remove };
    }
}
```

### 步骤 2: 实现 ResourcePile（资源堆）

**文件**: `src/world/mapobject/ResourcePile.cs`

```csharp
using UnityEngine;

namespace TH7
{
    /// <summary>
    /// 资源堆 - 一次性拾取
    /// </summary>
    public class ResourcePile : MapObject
    {
        [Header("Resource")]
        [SerializeField] ResourceType resourceType;
        [SerializeField] int amount = 5;

        bool collected;

        public ResourceType ResourceType => resourceType;
        public int Amount => amount;
        public override bool IsCollected => collected;

        public override HeroActionType GetInteractionType() => HeroActionType.PickUp;

        public override bool CanInteract(Hero hero) => !collected;

        public override InteractionResult Interact(Hero hero, SessionContext session)
        {
            if (collected)
                return InteractionResult.Fail("已被拾取");

            // 添加资源
            session.Resources.Add(resourceType, amount);
            collected = true;

            Debug.Log($"[ResourcePile] {hero.HeroName} 拾取 {amount} {resourceType}");

            return new InteractionResult
            {
                Success = true,
                Message = $"+{amount} {resourceType}",
                ResourcesGained = new ResourceBundle { { resourceType, amount } },
                ShouldRemove = true
            };
        }
    }
}
```

### 步骤 3: 实现 Mine（矿场）

**文件**: `src/world/mapobject/Mine.cs`

```csharp
using UnityEngine;
using GameFramework;

namespace TH7
{
    /// <summary>
    /// 矿场 - 占领后每日产出
    /// </summary>
    public class Mine : MapObject
    {
        [Header("Mine")]
        [SerializeField] ResourceType resourceType;
        [SerializeField] int dailyOutput = 2;
        [SerializeField] SpriteRenderer flagRenderer;  // 旗帜显示所有者
        [SerializeField] Color[] playerColors = { Color.blue, Color.red, Color.green, Color.yellow };

        // 所有者 (-1 = 中立)
        readonly Reactive<int> ownerId = new(-1);

        public ResourceType ResourceType => resourceType;
        public int DailyOutput => dailyOutput;
        public override int OwnerId => ownerId.Value;
        public Reactive<int> OwnerIdReactive => ownerId;

        public override HeroActionType GetInteractionType() => HeroActionType.Interact;

        protected override void Start()
        {
            base.Start();
            // 监听所有者变化，更新旗帜颜色
            ownerId.Watch(UpdateFlag);
            UpdateFlag(ownerId.Value);
        }

        void UpdateFlag(int owner)
        {
            if (flagRenderer == null) return;

            if (owner < 0)
            {
                flagRenderer.color = Color.gray;
            }
            else if (owner < playerColors.Length)
            {
                flagRenderer.color = playerColors[owner];
            }
        }

        public override bool CanInteract(Hero hero)
        {
            // 可以占领中立矿场，或夺取敌方矿场
            return ownerId.Value != hero.OwnerPlayerId;
        }

        public override InteractionResult Interact(Hero hero, SessionContext session)
        {
            int previousOwner = ownerId.Value;
            ownerId.Value = hero.OwnerPlayerId;

            string msg = previousOwner < 0
                ? $"占领了 {resourceType} 矿场"
                : $"夺取了 {resourceType} 矿场";

            Debug.Log($"[Mine] {hero.HeroName} {msg}");

            return InteractionResult.Ok(msg);
        }

        /// <summary>
        /// 每日产出（由 WorldTurnManager 调用）
        /// </summary>
        public void ProduceDailyOutput(SessionContext session)
        {
            if (ownerId.Value < 0) return;

            session.Resources.Add(resourceType, dailyOutput);
            Debug.Log($"[Mine] {resourceType} 矿场产出 {dailyOutput}");
        }
    }
}
```

### 步骤 4: 扩展 WorldContext 管理物件

**文件**: `src/context/WorldContext.cs` (修改)

添加以下内容：

```csharp
// 在 WorldContext 类中添加

// 地图物件管理
readonly List<MapObject> mapObjects = new();
readonly List<Mine> mines = new();

/// <summary>
/// 所有地图物件
/// </summary>
public IReadOnlyList<MapObject> MapObjects => mapObjects;

/// <summary>
/// 所有矿场
/// </summary>
public IReadOnlyList<Mine> Mines => mines;

/// <summary>
/// 注册地图物件
/// </summary>
public void RegisterMapObject(MapObject obj)
{
    if (obj == null || mapObjects.Contains(obj)) return;

    mapObjects.Add(obj);
    if (obj is Mine mine)
        mines.Add(mine);

    Debug.Log($"[World] 注册地图物件: {obj.ObjectType} at {obj.CellPosition}");
}

/// <summary>
/// 注销地图物件
/// </summary>
public void UnregisterMapObject(MapObject obj)
{
    if (obj == null) return;

    mapObjects.Remove(obj);
    if (obj is Mine mine)
        mines.Remove(mine);
}

/// <summary>
/// 获取指定位置的地图物件
/// </summary>
public MapObject GetMapObjectAt(Vector3Int cell)
{
    foreach (var obj in mapObjects)
    {
        if (obj.CellPosition == cell && !obj.IsCollected)
            return obj;
    }
    return null;
}

/// <summary>
/// 处理每日矿场产出
/// </summary>
public void ProcessDailyMineOutput(SessionContext session)
{
    foreach (var mine in mines)
    {
        mine.ProduceDailyOutput(session);
    }
}
```

### 步骤 5: 修改 ActionExecutor 处理物件交互

**文件**: `src/world/action/ActionExecutor.cs` (修改)

在 `ExecuteCoroutine` 中的 `MoveAction` 处理后添加：

```csharp
// 移动完成后检查目标格子是否有物件
if (action is MoveAction moveAction)
{
    // ... 现有移动逻辑 ...

    // 移动完成后检查物件交互
    var mapObject = worldContext.GetMapObjectAt(moveAction.Destination);
    if (mapObject != null && mapObject.CanInteract(action.Hero))
    {
        var session = worldContext.GetParent<SessionContext>();
        var result = mapObject.Interact(action.Hero, session);

        if (result.Success)
        {
            // 发布交互事件
            var eventSystem = GameEntry.Instance?.GetSystem<EventSystem>();
            eventSystem?.Publish(new MapObjectInteractedEvent
            {
                Hero = action.Hero,
                MapObject = mapObject,
                Result = result
            });

            // 如果需要移除物件
            if (result.ShouldRemove)
            {
                worldContext.UnregisterMapObject(mapObject);
                GameObject.Destroy(mapObject.gameObject);
            }
        }
    }
}
```

### 步骤 6: 添加物件相关事件

**文件**: `src/world/WorldEvents.cs` (修改)

```csharp
// 添加以下事件类

/// <summary>
/// 地图物件交互事件
/// </summary>
public class MapObjectInteractedEvent
{
    public Hero Hero;
    public MapObject MapObject;
    public InteractionResult Result;
}

/// <summary>
/// 矿场占领事件
/// </summary>
public class MineOccupiedEvent
{
    public Hero Hero;
    public Mine Mine;
    public int PreviousOwner;
}

/// <summary>
/// 资源拾取事件
/// </summary>
public class ResourcePickedUpEvent
{
    public Hero Hero;
    public ResourceType ResourceType;
    public int Amount;
}
```

### 步骤 7: 修改 WorldTurnManager 处理每日产出

**文件**: `src/world/WorldTurnManager.cs` (修改)

在 `ProcessDayEnd` 方法中添加：

```csharp
void ProcessDayEnd()
{
    // 城镇产出（已有）
    ProcessTownIncome();

    // [新增] 矿场产出
    ProcessMineIncome();

    // 推进日期
    sessionContext.AdvanceDay();

    // ...
}

void ProcessMineIncome()
{
    worldContext?.ProcessDailyMineOutput(sessionContext);
}
```

### 步骤 8: 创建 UI 通知组件

**文件**: `src/ui/World/ResourceNotificationUI.cs` (新增)

```csharp
using UnityEngine;
using TMPro;
using DG.Tweening;
using GameFramework;

namespace TH7.UI
{
    /// <summary>
    /// 资源变化通知 UI
    /// </summary>
    public class ResourceNotificationUI : UIBehaviour
    {
        [Header("References")]
        [SerializeField] TextMeshProUGUI notificationText;
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] float displayDuration = 2f;
        [SerializeField] float fadeInDuration = 0.3f;
        [SerializeField] float fadeOutDuration = 0.5f;

        Sequence currentSequence;

        [AutoSubscribe]
        void OnResourcePickedUp(ResourcePickedUpEvent e)
        {
            ShowNotification($"+{e.Amount} {e.ResourceType}", Color.green);
        }

        [AutoSubscribe]
        void OnMineOccupied(MineOccupiedEvent e)
        {
            ShowNotification($"占领了 {e.Mine.ResourceType} 矿场", Color.yellow);
        }

        void ShowNotification(string message, Color color)
        {
            currentSequence?.Kill();

            notificationText.text = message;
            notificationText.color = color;
            canvasGroup.alpha = 0;

            currentSequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1, fadeInDuration))
                .AppendInterval(displayDuration)
                .Append(canvasGroup.DOFade(0, fadeOutDuration));
        }
    }
}
```

---

## 四、配置数据

### 4.1 创建 MapObjectConfig (可选，后续扩展)

**文件**: `src/world/mapobject/MapObjectConfig.cs`

```csharp
using UnityEngine;

namespace TH7
{
    [CreateAssetMenu(menuName = "TH7/Map Object Config")]
    public class MapObjectConfig : ScriptableObject
    {
        public string objectId;
        public MapObjectType type;
        public Sprite icon;
        public string displayName;

        [Header("Resource Pile")]
        public ResourceType resourceType;
        public int minAmount = 3;
        public int maxAmount = 7;

        [Header("Mine")]
        public int dailyOutput = 2;
    }
}
```

### 4.2 预制体结构

**ResourcePile 预制体**:
```
ResourcePile_Wood (ResourcePile.cs)
├── Sprite (SpriteRenderer) - 木材堆图片
└── Settings:
    - ObjectType: Resource
    - ResourceType: Wood
    - Amount: 5
```

**Mine 预制体**:
```
Mine_Gold (Mine.cs)
├── Sprite (SpriteRenderer) - 金矿图片
├── Flag (SpriteRenderer) - 所有者旗帜
└── Settings:
    - ObjectType: Mine
    - ResourceType: Gold
    - DailyOutput: 1000
```

---

## 五、Unity 配置步骤

### 5.1 创建预制体

1. **ResourcePile_Wood**
   - 创建空 GameObject，添加 `ResourcePile` 组件
   - 添加子物体 `Sprite`，配置 SpriteRenderer
   - 设置 ResourceType = Wood, Amount = 5
   - 保存为预制体

2. **Mine_Gold**
   - 创建空 GameObject，添加 `Mine` 组件
   - 添加子物体 `Sprite` 和 `Flag`
   - 配置 FlagRenderer 引用
   - 设置 ResourceType = Gold, DailyOutput = 1000
   - 保存为预制体

### 5.2 在场景中放置

1. 打开 WorldScene
2. 将预制体拖入场景
3. 调整位置（确保在 Tilemap 格子上）
4. 保存场景

### 5.3 配置 WorldSceneController

确保 WorldContext 在初始化时收集所有 MapObject：

```csharp
// 在 WorldSceneController.InitializeContext() 后添加
void InitializeMapObjects()
{
    var allObjects = FindObjectsByType<MapObject>(FindObjectsSortMode.None);
    foreach (var obj in allObjects)
    {
        worldContext.RegisterMapObject(obj);
        obj.SetPositionConverter(cell => mapManager.CellToWorld(cell));
    }
}
```

---

## 六、测试计划

### 测试 1: 资源拾取
1. 在地图放置 ResourcePile_Wood
2. 控制英雄移动到该位置
3. 验证：
   - [ ] 资源自动增加
   - [ ] 资源堆消失
   - [ ] UI 显示通知

### 测试 2: 矿场占领
1. 在地图放置 Mine_Gold
2. 控制英雄移动到该位置
3. 验证：
   - [ ] 旗帜变为玩家颜色
   - [ ] UI 显示占领通知

### 测试 3: 每日产出
1. 占领一个 Mine_Gold
2. 结束回合进入新的一天
3. 验证：
   - [ ] 金币增加 dailyOutput 数量
   - [ ] 控制台日志正确

---

## 七、后续扩展

完成 M1 后，可以继续添加：

1. **Monster（野怪）** - M3 阶段
2. **Artifact（神器）** - 装备系统
3. **Portal（传送门）** - 快速移动
4. **Dwelling（野外兵营）** - 招募单位

---

## 八、文件清单

需要创建/修改的文件：

| 操作 | 文件 |
|------|------|
| 新增 | `src/world/mapobject/MapObject.cs` |
| 新增 | `src/world/mapobject/ResourcePile.cs` |
| 新增 | `src/world/mapobject/Mine.cs` |
| 新增 | `src/world/mapobject/guide.md` |
| 新增 | `src/ui/World/ResourceNotificationUI.cs` |
| 修改 | `src/context/WorldContext.cs` |
| 修改 | `src/world/action/ActionExecutor.cs` |
| 修改 | `src/world/WorldEvents.cs` |
| 修改 | `src/world/WorldTurnManager.cs` |
| 修改 | `src/scene/WorldSceneController.cs` |
