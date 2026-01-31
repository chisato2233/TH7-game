# 文明交付示例

## 美术风格规范

### 统一要求

| 项目 | 规范 |
|------|------|
| 整体画风 | 半写实风格 |
| 透视角度 | Isometric (Z as Y)，3/4 正面视角 (斜向面对镜头) |
| 人物比例 | 6-8 头身 |
| 背景 | 透明 (PNG Alpha通道) |

### 各文明可差异化

- 阵营主题色 (primaryColor)
- 服饰、盔甲、武器的文化元素
- 特有的装饰纹样和图腾

### 风格参考

半写实风格示例：清晰的动漫式轮廓线 + 写实的光影和材质细节，类似《原神》《崩坏：星穹铁道》的立绘风格。

---

## 文件夹结构

```
art/design/
├── README.md              # 本文件
├── neutral/               # 中立阵营
│   ├── faction.yaml       # 阵营配置
│   ├── heroes/            # 英雄文件夹
│   │   └── {hero_id}/
│   │       ├── config.yaml              # 英雄配置
│   │       ├── portrait_{size}.png      # 头像 (如 portrait_256x256.png)
│   │       ├── worldsprite_{size}.png   # 世界地图Sprite (如 worldsprite_128x128.png)
│   │       └── animations/              # 动画文件夹
│   ├── units/             # 兵种文件夹
│   │   └── {unit_id}/
│   │       ├── config.yaml              # 兵种配置
│   │       ├── icon_{size}.png          # 图标 (如 icon_64x64.png)
│   │       ├── sprite_{size}.png        # 战斗Sprite (如 sprite_128x128.png)
│   │       └── animations/              # 动画文件夹
│   └── buildings/         # 建筑配置
│       └── {building_id}/
│           ├── config.yaml              # 建筑配置
│           └── icon_{size}.png          # 建筑图标 (如 icon_128x128.png)
├── arabian/               # 阿拉伯阵营 (待创建)
├── castle/                # 城堡阵营 (待创建)
└── ...
```


## 图片资源规格

### 图片命名规范

**所有图片资源必须在文件名中标注尺寸**，格式：`{type}_{width}x{height}.png`

这样做的好处：
1. 程序可以自动解析尺寸设置 Pixel Per Unit
2. 美术同学可以清楚知道当前资源的实际大小
3. 支持自定义尺寸，不强制固定大小

### 英雄资源规格

| 资源类型 | 文件名格式 | 推荐尺寸 | 说明 |
|---------|-----------|---------|------|
| 头像 | portrait_{size}.png | 256x256 | 选择界面和详情面板 |
| 世界Sprite | worldsprite_{size}.png | 1024x1024 | 世界地图上的英雄渲染 |

示例：`portrait_512x512.png`、`worldsprite_1024x1024.png`

### 兵种资源规格

| 资源类型 | 文件名格式 | 推荐尺寸 | 说明 |
|---------|-----------|---------|------|
| 图标 | icon_{size}.png | 256x256 | 军队列表和招募界面 |
| 战斗Sprite | sprite_{size}.png | 512x512 | 战斗场景中的兵种渲染 |

示例：`icon_256x256.png`、`sprite_512x512.png`

### 建筑资源规格

| 资源类型 | 文件名格式 | 推荐尺寸 | 说明 |
|---------|-----------|---------|------|
| 建筑图标 | icon_{size}.png | 512x512 | 城镇建造界面 |

### Pixel Per Unit 计算规则

文件名中的尺寸数字即为 Pixel Per Unit 值：
- `worldsprite_128x128.png` → PPU = 128
- `worldsprite_1000x1000.png` → PPU = 1000
- `portrait_256x256.png` → PPU = 256

### 动画序列帧规范

动画资源放置在以 `_anim_image/` 结尾的文件夹中：

```
heroes/{hero_id}/
├── config.yaml
├── portrait_256x256.png          # 头像 (尺寸可自定义)
├── worldsprite_128x128.png       # 世界Sprite (尺寸可自定义)
└── animations/
    ├── idle_anim_image/          # 待机动画
    │   ├── frame_0001.png
    │   ├── frame_0002.png
    │   └── ... (共60帧)
    └── move_anim_image/          # 移动动画
        ├── frame_0001.png
        ├── frame_0002.png
        └── ... (共60帧)
```

#### 动画要求

| 属性 | 要求 |
|-----|------|
| 帧数 | 60帧 (可根据需要调整，建议60帧用于流畅动画) |
| 背景 | 透明 (PNG格式，Alpha通道) |
| 帧尺寸 | 与静态Sprite一致 |
| 帧率 | 30 FPS (默认) |

#### 支持的动画类型

| 动画类型 | 文件夹名 | 用途 |
|---------|---------|------|
| idle | idle_anim_image/ | 待机动画 |
| move | move_anim_image/ | 移动动画 |
| attack | attack_anim_image/ | 攻击动画 |
| hit | hit_anim_image/ | 受击动画 |
| death | death_anim_image/ | 死亡动画 |
| cast | cast_anim_image/ | 施法动画 |

#### 兵种动画资源结构

```
units/{unit_id}/
├── config.yaml
├── icon_256x256.png          # 图标 (尺寸可自定义)
├── sprite_512x512.png        # 战斗Sprite (尺寸可自定义)
└── animations/
    ├── idle_anim_image/      # 待机动画 (60帧)
    ├── move_anim_image/      # 移动动画 (60帧)
    ├── attack_anim_image/    # 攻击动画 (60帧)
    └── death_anim_image/     # 死亡动画 (60帧)
```



## 阵营类型 (FactionType / BiomeType)

| 类型 | 说明 |
|-------------|----|
| Neutral,    |中立 | 
| Arabian,    |阿拉伯|
| Egyptian,   |埃及 |
| Indian,     |印度 |
| Greek,      |希腊 |
| Chinese,    |汉唐 |
| Mongolian,  |蒙古 |
| Islander    |南岛 |

## 英雄职业 - 暂时没用 (HeroClass)

| 职业 | 说明 |
|------|------|
| Warrior | 战士 - 高攻击/防御 |
| Mage | 法师 - 高法力/知识 |
| Ranger | 游侠 - 平衡型 |
| Cleric | 牧师 - 支援型 |

## 兵种等级 (UnitTier)

| 等级 | 说明 |
|------|------|
| Tier1 | 基础兵种 (农民、骷髅等) |
| Tier2 | 进阶兵种 (剑士、弓箭手等) |
| Tier3 | 精锐兵种 (骑士、法师等) |
| Tier4 | 高级兵种 (狮鹫、恶魔等) |
| Tier5 | 顶级兵种 (巨龙、天使等) |

## 建筑类型 (BuildingType)

### 核心建筑
| 类型 | 说明 |
|------|------|
| TownHall | 城镇大厅 - 产金 |
| Fort | 要塞 - 城防 |
| Tavern | 酒馆 - 招募英雄 |
| Marketplace | 市场 - 资源交易 |
| Blacksmith | 铁匠铺 - 攻击加成 |

### 魔法建筑
| 类型 | 说明 |
|------|------|
| MageGuild | 法师公会 - 学习魔法 |

### 兵种建筑
| 类型 | 说明 |
|------|------|
| Dwelling1 | 1级兵种建筑 |
| Dwelling2 | 2级兵种建筑 |
| Dwelling3 | 3级兵种建筑 |
| Dwelling4 | 4级兵种建筑 |
| Dwelling5 | 5级兵种建筑 |


### 特殊建筑
| 类型 | 说明 |
|------|------|
| Grail | 圣杯建筑 |

## 建筑等级 (BuildingTier)

| 等级 | 说明 |
|------|------|
| None | 无 |
| Basic | 基础版 |
| Upgraded | 升级版 |

## 资源类型

| 类型 | 说明 |
|------|------|
| gold | 金币 |
| wood | 木材 |
| ore | 矿石 |
| crystal | 水晶 |

## YAML 配置示例

### 阵营配置 (faction.yaml)

```yaml
# Faction Configuration
# 阵营配置

factionType: Neutral           # [枚举] 见上方 "阵营类型" 表格
displayName: Neutral
description: Unaligned forces, including various neutral creatures and mercenaries

primaryColor: "#808080"        # 十六进制颜色

# 初始资源
startingResources:
  gold: 1500
  wood: 8
  ore: 8
  crystal: 0

# 可用英雄 (强引用)
availableHeroes:
  - "@HeroConfig_neutral_mercenary_captain"
  - "@HeroConfig_neutral_wandering_mage"

# 阵营兵种 (强引用)
factionUnits:
  - "@UnitConfig_neutral_peasant"
  - "@UnitConfig_neutral_rogue"

# 阵营建筑 (强引用)
factionBuildings:
  - "@BuildingConfig_neutral_tavern"
```

### 英雄配置 (heroes/{hero_id}/config.yaml)

```yaml
# Hero Configuration
# 英雄配置

heroId: arabian_desert_knight   # 全局唯一ID [a-zA-Z0-9_]
displayName: Desert Knight
description: A noble warrior from the scorching sands
faction: Arabian               # [枚举] 见上方 "阵营类型" 表格
class: Warrior                 # [枚举] 见上方 "英雄职业" 表格

# 视觉资源 (本文件夹内)
portrait: portrait_256x256.png           # 头像
worldSprite: worldsprite_1024x1024.png   # 世界Sprite

# 基础属性
attack: 3
defense: 2
spellPower: 1
knowledge: 1

# 初始军队 (强引用，可选)
startingArmy:
  - unit: "@UnitConfig_arabian_swordsman"
    count: 15
  - unit: "@UnitConfig_arabian_archer"
    count: 8
```

### 兵种配置 (units/{unit_id}/config.yaml)

```yaml
# Unit Configuration
# 兵种配置

unitId: arabian_swordsman       # 全局唯一ID [a-zA-Z0-9_]
displayName: Swordsman
description: A skilled warrior wielding a curved blade
faction: Arabian               # [枚举] 见上方 "阵营类型" 表格
tier: Tier2                    # [枚举] 见上方 "兵种等级" 表格

# 视觉资源 (本文件夹内)
icon: icon_256x256.png         # 图标
combatSprite: sprite_512x512.png  # 战斗Sprite

# 战斗属性
attack: 5
defense: 3
minDamage: 2
maxDamage: 4
health: 10
speed: 6
initiative: 8

# 招募信息
goldCost: 60
growthPerWeek: 8

# 特殊能力 (可选)
abilities:
  - no_retaliation
```

### 建筑配置 (buildings/{building_id}/config.yaml)

```yaml
# Building Configuration
# 建筑配置

buildingId: arabian_tavern      # 全局唯一ID [a-zA-Z0-9_]
type: Tavern                   # [枚举] 见上方 "建筑类型" 表格
displayName: Tavern
description: A place to recruit heroes and gather information

# 视觉资源 (本文件夹内)
icon: icon_512x512.png

# 基础建造费用
basicCost:
  gold: 500
  wood: 5
  ore: 5

# 升级费用 (可选)
upgradeCost:
  gold: 1000
  wood: 10
  ore: 10

# 前置建筑要求 (可选)
requirements:
  - building: TownHall         # [枚举] 见上方 "建筑类型" 表格
    tier: Basic                # [枚举] 见上方 "建筑等级" 表格

# 产出 (兵种建筑使用)
goldPerDay: 0
producedUnitId: ""
weeklyGrowth: 0
```

### 兵种建筑示例 (Dwelling)

```yaml
buildingId: arabian_dwelling1
type: Dwelling1                # [枚举] 见上方 "建筑类型" 表格
displayName: Swordsman Barracks
description: Trains swordsmen for your army

icon: icon_512x512.png

basicCost:
  gold: 200
  wood: 5
  ore: 0

upgradeCost:
  gold: 400
  wood: 10
  ore: 5

requirements: []

goldPerDay: 0
producedUnitId: arabian_swordsman  # 对应兵种的 unitId
weeklyGrowth: 14
```

## 工作流程

1. 复制 `neutral/` 文件夹作为模板
2. 重命名为对应阵营 (如 `arabian/`)
3. 修改 `faction.yaml` 中的阵营信息
4. 在 `heroes/` 中为每个英雄创建文件夹，包含 `config.yaml` 和图片资源
5. 在 `units/` 中为每个兵种创建文件夹，包含 `config.yaml` 和图片资源
6. 在 `buildings/` 中为每个建筑创建文件夹，包含 `config.yaml` 和图片资源


## 注意事项

- YAML 文件使用 UTF-8 编码
- 缩进使用 2 个空格
- 字符串包含特殊字符时使用引号
- 颜色使用十六进制格式: `"#FF0000"`
- 图片格式统一使用 PNG
- 内容全英文
- 建筑 ID 建议添加阵营前缀: `neutral_tavern`, `castle_barracks`
