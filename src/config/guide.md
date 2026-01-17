# 配置系统指南

> 文明和英雄配置，用于新游戏选择流程

## 架构概览

```
玩家点击 "New Game"
    ↓
NewGameWindow 显示
    ↓
选择文明 (FactionConfig)
    ↓
选择英雄 (HeroConfig)
    ↓
创建 SessionContext + 初始化资源
    ↓
进入 WorldScene
```

## 文件说明

| 文件 | 职责 |
|------|------|
| `FactionConfig.cs` | 文明配置 ScriptableObject |
| `FactionConfigDatabase.cs` | 文明配置数据库 |
| `NewGameSettings.cs` | 新游戏设置数据结构 |

## 使用步骤

### 1. 创建文明配置

1. 右键 Project → `Create → TH7 → Faction Config`
2. 配置文明属性：
   - `FactionType`: 选择对应的 BiomeType
   - `DisplayName`: 显示名称
   - `Description`: 文明描述
   - `Icon`: 文明图标
   - `StartingResources`: 初始资源
   - `AvailableHeroes`: 可选英雄列表

### 2. 创建英雄配置

1. 右键 Project → `Create → TH7 → Hero Config`
2. 配置英雄属性：
   - `HeroId`: 唯一标识
   - `DisplayName`: 显示名称
   - `Faction`: 所属文明 (BiomeType)
   - `Class`: 英雄职业
   - `Portrait`: 头像
   - `Attack/Defense/SpellPower/Knowledge`: 属性
   - `StartingArmy`: 初始军队

### 3. 创建数据库

1. 右键 Project → `Create → TH7 → Faction Config Database`
2. 将所有 FactionConfig 拖入 `Factions` 列表
3. 将数据库拖入 `NewGameWindow.factionDatabase`

## 示例配置

### Arabian 文明

```
FactionConfig_Arabian:
  FactionType: Arabian
  DisplayName: 阿拉伯
  Description: 沙漠王国，擅长魔法和远程攻击
  StartingResources:
    Gold: 2000
    Wood: 5
    Ore: 5
    Crystal: 2
  AvailableHeroes:
    - HeroConfig_Aladin
    - HeroConfig_Sinbad
```

### Aladin 英雄

```
HeroConfig_Aladin:
  HeroId: aladin
  DisplayName: 阿拉丁
  Faction: Arabian
  Class: Mage
  Attack: 1
  Defense: 1
  SpellPower: 3
  Knowledge: 2
  StartingArmy:
    - UnitId: djinn, Count: 5
    - UnitId: archer, Count: 20
```

## UI 结构

NewGameWindow 预制体结构：
```
NewGameWindow
├── FactionSelectionPanel
│   ├── FactionList (Grid Layout)
│   │   └── [FactionItemPrefab...]
│   └── FactionInfo
│       ├── Icon
│       ├── Name
│       └── Description
├── HeroSelectionPanel
│   ├── HeroList (Grid Layout)
│   │   └── [HeroItemPrefab...]
│   └── HeroInfo
│       ├── Portrait
│       ├── Name
│       ├── Stats
│       └── Description
└── Buttons
    ├── BackButton
    ├── NextButton
    ├── StartButton
    └── CancelButton
```

## 扩展指南

### 添加新文明

1. 创建 FactionConfig 资产
2. 创建该文明的 HeroConfig 资产
3. 将 HeroConfig 添加到 FactionConfig.AvailableHeroes
4. 将 FactionConfig 添加到 FactionConfigDatabase

### 自定义初始资源

修改 `FactionConfig.StartingResources` 即可为不同文明设置不同的起始资源。
