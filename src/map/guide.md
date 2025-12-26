# Map 模块使用指南

> 本模块提供 Tilemap 可视化编辑 + 运行时逻辑数据分离的地图系统

## 架构概览

```
设计时: Unity Tilemap Editor + GameTile 绘制
    ↓
运行时: MapManager.InitializeMap() 从 Tilemap 读取
    ↓
逻辑层: MapData 用于寻路、移动力计算
```

## 文件说明

| 文件 | 职责 |
|------|------|
| `GameTile.cs` | TileBase 子类，用于 Tilemap 绘制，携带地形属性 |
| `Tile.cs` | 纯逻辑地块结构体，运行时使用 |
| `MapData.cs` | 运行时地图数据容器，从 Tilemap 初始化 |
| `MapManager.cs` | 地图管理器，提供坐标转换和查询接口 |
| `TerrainConfig.cs` | 地形配置表，定义移动力消耗和可通行性 |

## 使用步骤

### 1. 创建 GameTile 资产

1. 在 Project 窗口右键 → `Create → TH7 → Game Tile`
2. 配置 GameTile 属性：
   - `Sprite`: 地块贴图
   - `Ground`: 基础地表 (Land/Water/DeepWater/Void)
   - `Surface`: 地表覆盖 (None/Road/Forest/Mountain/...)
   - `Biome`: 视觉风格 (Neutral/Arabian/Egyptian/...)

### 2. 创建 TerrainConfig 配置

1. 右键 → `Create → TH7 → Terrain Config`
2. 配置各地形的移动力消耗和可通行性：
   - `GroundTypes`: 配置 Land=1, Water=2, DeepWater=999, Void=999
   - `SurfaceTypes`: 配置 None=0, Road=-1, Forest=1, Mountain=999 等

### 3. 设置场景

1. 创建 Grid 对象 (GameObject → 2D Object → Tilemap → Rectangular)
2. 使用 Tile Palette 绘制地图（用创建的 GameTile）
3. 创建空对象挂载 `MapManager`
4. 将 Tilemap 拖入 `MapManager.groundTilemap`
5. 将 TerrainConfig 拖入 `MapManager.terrainConfig`

### 4. 运行时访问

```csharp
// 获取 MapManager (通过 WorldContext)
var mapManager = worldContext.MapManager;

// 世界坐标转格子坐标
Vector3Int cell = mapManager.WorldToCell(worldPos);

// 获取逻辑 Tile
Tile tile = mapManager.GetTileAt(worldPos);

// 检查可通行性
bool canPass = mapManager.IsPassable(worldPos);

// 获取移动消耗
int cost = mapManager.GetMovementCost(worldPos);
```

## 地块分层说明

| 层级 | 枚举 | 说明 |
|------|------|------|
| Ground | `GroundType` | 基础地表，决定基础可通行性 |
| Surface | `SurfaceType` | 覆盖物，修正移动消耗 |
| Object | `MapObjectType` | 地图物件，可交互对象 (当前未完全实现) |
| Biome | `BiomeType` | 视觉风格，影响贴图选择 |

## 扩展指南

### 添加新地形类型

1. 在 `GameEnums.cs` 中添加枚举值
2. 创建对应的 GameTile 资产
3. 在 TerrainConfig 中配置属性

### 多层 Tilemap (待扩展)

当前仅支持单层 groundTilemap，如需多层：
1. 在 MapManager 中添加 `surfaceTilemap`、`objectTilemap`
2. 修改 `MapData.InitFromTilemap()` 合并多层数据

## 注意事项

- GameTile 是 ScriptableObject，修改后需保存
- MapData 在运行时生成，不会修改原始 Tilemap
- 坐标系使用 Unity Tilemap 默认坐标系 (左下为原点)
