# Unity 操作指南

本文档说明在 Unity Editor 中需要手动完成的配置步骤。

---

## 1. WorldScene 配置

城镇界面采用 UI 覆盖层方式，集成在 WorldScene 中。

### WorldSceneController 配置

1. 选中 `WorldController` GameObject
2. 配置 `WorldSceneController` 组件:
   - `Map Manager`: 地图管理器
   - `Town Panel`: TownPanelUI（全屏城镇面板）
   - `Resource Bar`: ResourceBarUI（顶部资源条）
   - `Town Config Database`: 拖入配置资源
   - `Unit Config Database`: 拖入配置资源

### WorldScene 层级结构

```
WorldScene
├── WorldController (WorldSceneController)
├── Map (Tilemap Grid)
│   ├── Ground Tilemap
│   └── Objects Tilemap
├── WorldUI (Canvas)
│   ├── ResourceBar
│   └── MiniMap (可选)
└── TownPanel (Canvas, 默认隐藏)
    ├── TownPanelUI
    ├── BuildingInfoPanel
    └── RecruitPanel
```

---

## 2. 创建 ScriptableObject 配置

### TownConfigDatabase

1. `Assets` → `Create` → `TH7` → `Town Config Database`
2. 保存到 `Assets/data/TownConfigDatabase.asset`
3. 添加建筑配置

### UnitConfigDatabase

1. `Assets` → `Create` → `TH7` → `Unit Config Database`
2. 保存到 `Assets/data/UnitConfigDatabase.asset`
3. 添加兵种配置

### BuildingConfig

为每个建筑创建配置：
1. `Assets` → `Create` → `TH7` → `Building Config`
2. 配置:
   - `Type`: 建筑类型
   - `Display Name`: 显示名称
   - `Description`: 描述
   - `Icon`: 图标
   - `Basic Cost`: 基础建造成本
   - `Upgrade Cost`: 升级成本
   - `Requirements`: 前置建筑列表
   - `Gold Per Day`: 每日产金（TownHall）
   - `Produced Unit Id`: 产出兵种（Dwelling）
   - `Weekly Growth`: 每周产出数量

### UnitConfig

为每个兵种创建配置：
1. `Assets` → `Create` → `TH7` → `Unit Config`
2. 配置战斗属性、招募成本等

---

## 3. UI 预制体

### 目录结构

```
Assets/
  prefabs/
    UI/
      Common/
        ResourceDisplay.prefab
        ResourceBar.prefab
        ResourceCost.prefab
      Town/
        TownPanel.prefab
        BuildingSlot.prefab
        BuildingInfoPanel.prefab
        RecruitPanel.prefab
        RecruitUnitSlot.prefab
      MainMenu/
        SaveSlotWindow.prefab
        SaveSlotItem.prefab
```

### ResourceDisplay 预制体

1. 创建 UI Image + TextMeshProUGUI
2. 添加 `ResourceDisplayUI` 组件
3. 配置:
   - `Icon`: Image 组件
   - `Amount Text`: TextMeshProUGUI 组件
   - `Resource Type`: 资源类型
   - `Auto Bind To Session`: 是否自动绑定

### ResourceBar 预制体

1. 创建 Horizontal Layout Group
2. 添加 7 个 ResourceDisplay 子物体
3. 添加 `ResourceBarUI` 组件
4. 配置各资源 Display 引用

### BuildingSlot 预制体

1. 创建 UI Button/Image
2. 添加 `BuildingSlotUI` 组件
3. 配置:
   - `Icon Image`: 建筑图标
   - `Name Text`: 名称文本
   - `Tier Indicator`: 等级指示器
   - `Locked Overlay`: 锁定遮罩
   - `Built Indicator`: 已建造标识
   - `Button`: ButtonManager 组件

### TownPanel 预制体

1. 创建主面板布局
2. 添加 `TownPanelUI` 组件
3. 子物体:
   - Header (城镇名、阵营)
   - BuildingGrid (建筑网格区域)
   - InfoPanel (建筑详情区域)
   - Buttons (招募、离开按钮)

### BuildingInfoPanel 预制体

1. 使用 `UIWindowBehaviour` 基类
2. 配置动画类型 (DOTween 推荐)
3. 添加 CanvasGroup 组件
4. 配置:
   - `Icon Image`: 建筑图标
   - `Name Text`: 名称
   - `Description Text`: 描述
   - `Status Text`: 状态
   - `Cost Container`: 成本显示容器
   - `Cost Item Prefab`: ResourceCost 预制体
   - `Requirements Text`: 前置条件
   - `Build Button`: 建造按钮
   - `Upgrade Button`: 升级按钮
   - `Close Button`: 关闭按钮

### RecruitPanel 预制体

1. 使用 `UIWindowBehaviour` 基类
2. 包含:
   - 兵种列表 (`RecruitListUI`)
   - 选中单位信息区域
   - 数量输入控件
   - 成本预览
   - 招募/关闭按钮

---

## 4. 可拖动窗口配置

为窗口添加拖动功能：

1. 在窗口的**标题栏**区域创建一个子物体
2. 添加 `WindowDragger` 组件 (Michsky.MUIP)
3. 配置:
   - `Drag Object`: 整个窗口的 RectTransform
   - `Drag Area`: Canvas 的 RectTransform (可选)
   - `Top On Drag`: true (拖动时置顶)

---

## 5. 场景 Build Settings

添加场景到 Build Settings:

1. `File` → `Build Settings`
2. 添加:
   - `scenes/BootScene.unity` (index 0)
   - `scenes/MainMenuScene.unity`
   - `scenes/WorldScene.unity`

---

## 6. Easy Save 3 配置

1. `Tools` → `Easy Save 3` → `Settings`
2. 确保 ES3 类型已生成
3. 如果使用新的可序列化类型，点击 `Auto Update References`

### 自定义类型支持

`Reactive<T>` 和 `ReactiveList<T>` 已配置为 ES3 兼容:
- 只序列化 `SavedValue` / `SavedItems` 属性
- 不序列化监听器（运行时状态）

---

## 7. 项目设置检查

### Player Settings

- Company Name: 你的公司名
- Product Name: TH7
- Version: 0.1.0

### Input System

如果使用新输入系统，确保 `Project Settings` → `Player` → `Active Input Handling` 设置正确。

### URP

确保 `Settings/UniversalRenderPipelineAsset` 已分配到 `Graphics` 设置。
