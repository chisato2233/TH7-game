# MapObject 模块指南

> 地图物件系统：资源堆、矿场等可交互对象

## 架构概览

```
MapObject (基类)
├── ResourcePile  - 资源堆（一次性拾取）
├── Mine          - 矿场（占领后每日产出）
└── [扩展]        - Monster, Artifact, Portal...
```

通过继承实现多态，不使用枚举标识类型。

## 文件说明

| 文件 | 职责 |
|------|------|
| `MapObject.cs` | 地图物件基类，定义交互接口 |
| `ResourcePile.cs` | 资源堆，拾取后消失 |
| `Mine.cs` | 矿场，占领后每日产出资源 |
| `MapObjectSpawner.cs` | 运行时随机生成资源堆 |

## 核心类

### MapObject

```csharp
public abstract class MapObject : GameBehaviour
{
    // 属性
    Vector3Int CellPosition;        // 格子坐标
    int OwnerId;                    // 所有者 (-1 = 中立)
    bool IsCollected;               // 是否已收集
    bool RemoveAfterInteract;       // 交互后是否移除

    // 方法
    InteractionResult Interact(Hero hero, SessionContext session);
    bool CanInteract(Hero hero);
}
```

### InteractionResult

```csharp
public class InteractionResult
{
    bool Success;
    string Message;
    ResourceBundle ResourcesGained;
    bool ShouldRemove;  // 是否移除物件
}
```

## 使用步骤

### 1. 创建预制体

**ResourcePile_Wood**:
```
ResourcePile_Wood (ResourcePile.cs)
└── Sprite (SpriteRenderer)

设置:
- ResourceType: Wood
- Amount: 5
```

**Mine_Gold**:
```
Mine_Gold (Mine.cs)
├── Sprite (SpriteRenderer)
└── Flag (SpriteRenderer)

设置:
- ResourceType: Gold
- DailyOutput: 1000
- FlagRenderer: Flag 对象
```

### 2. 放置物件（手动）

1. 打开 WorldScene
2. 将预制体拖入场景
3. 调整位置（对齐 Tilemap 格子）
4. 保存场景

### 3. 随机生成（推荐）

使用 `MapObjectSpawner` 运行时随机生成资源堆：

1. 在 WorldScene 创建空对象，挂载 `MapObjectSpawner`
2. 配置生成参数：
   - `Spawn Count`: 生成数量
   - `Min Distance From Origin`: 距原点最小距离
   - `Min Distance Between`: 物件间最小距离
3. 添加 `Resource Configs`：
   - `Resource Type`: 资源类型
   - `Prefab`: 资源堆预制体（需要有 SpriteRenderer）
   - `Weight`: 权重（越大越容易生成）
   - `Min/Max Amount`: 资源数量范围
4. 将 Spawner 拖入 `WorldSceneController.mapObjectSpawner`

```
示例配置:
┌─────────────┬────────┬────────┬─────────┬─────────┐
│ ResourceType│ Prefab │ Weight │ MinAmt  │ MaxAmt  │
├─────────────┼────────┼────────┼─────────┼─────────┤
│ Gold        │ Pile_G │ 30     │ 100     │ 500     │
│ Wood        │ Pile_W │ 25     │ 3       │ 8       │
│ Ore         │ Pile_O │ 25     │ 3       │ 8       │
│ Crystal     │ Pile_C │ 20     │ 1       │ 3       │
└─────────────┴────────┴────────┴─────────┴─────────┘
```

### 4. 运行时注册

物件会在 `WorldSceneController.InitializeMapObjects()` 中自动注册到 `WorldContext`。

## 交互流程

```
英雄移动到目标格子
    ↓
ActionExecutor.TryInteractWithMapObject()
    ↓
mapObject.CanInteract(hero)?
    ↓ Yes
mapObject.Interact(hero, session)
    ↓
发布 MapObjectInteractedEvent
    ↓
result.ShouldRemove? → 销毁物件
```

## 每日产出流程

```
WorldTurnManager.ProcessDayEnd()
    ↓
ProcessMineProduction(session)
    ↓
worldContext.ProcessDailyMineOutput(session)
    ↓
遍历所有矿场 → mine.ProduceDailyOutput()
```

## 事件列表

| 事件 | 说明 |
|------|------|
| `MapObjectInteractedEvent` | 物件交互完成 |
| `MineOccupiedEvent` | 矿场被占领 |
| `ResourcePickedUpEvent` | 资源被拾取 |

## 扩展指南

### 添加新物件类型

1. 创建类继承 `MapObject`
2. 实现 `Interact()`, `CanInteract()`
3. 创建预制体

### 示例：神器物件

```csharp
public class Artifact : MapObject
{
    [SerializeField] string artifactId;

    public override bool RemoveAfterInteract => true;

    public override InteractionResult Interact(Hero hero, SessionContext session)
    {
        // 将神器添加到英雄背包
        hero.AddArtifact(artifactId);
        return InteractionResult.Ok($"Found artifact: {artifactId}", remove: true);
    }
}
```
