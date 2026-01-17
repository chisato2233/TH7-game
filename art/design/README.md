# TH7 美术交付规范

## 文件夹结构

```
art/design/
├── README.md              # 本文件
├── neutral/               # 中立阵营
│   ├── faction.yaml       # 阵营配置
│   ├── heroes/            # 英雄文件夹
│   │   └── {hero_id}/
│   │       ├── config.yaml    # 英雄配置
│   │       ├── portrait.png   # 头像 (256x256)
│   │       └── world.png      # 世界地图图标 (64x64)
│   ├── units/             # 兵种文件夹
│   │   └── {unit_id}/
│   │       ├── config.yaml    # 兵种配置
│   │       ├── icon.png       # 图标 (64x64)
│   │       └── combat.png     # 战斗精灵 (128x128)
│   └── buildings/         # 建筑配置
│       └── {building_id}/
│           ├── config.yaml    # 建筑配置
│           └── sprite.png     # 建筑图标 (128x128)
├── arabian/               # 阿拉伯阵营 (待创建)
├── castle/                # 城堡阵营 (待创建)
└── ...
```

## 命名规范

- 所有ID使用小写英文 + 下划线: `mercenary_captain`, `fire_mage`
- 每个英雄/兵种/建筑一个独立文件夹
- 文件夹名 = ID名
- 配置文件统一命名为 `config.yaml`

## 图片资源规格

| 资源类型 | 文件名 | 尺寸 | 说明 |
|---------|--------|------|------|
| 英雄头像 | portrait.png | 256x256 | 选择界面和详情面板 |
| 英雄地图图标 | mapicon.png | 64x64 | 世界地图上的显示 |
| 兵种图标 | icon.png | 64x64 | 军队列表和招募界面 |
| 兵种战斗精灵 | combat.png | 128x128 | 战斗场景 |
| 建筑图标 | sprite.png | 128x128 | 城镇建造界面 |

## 阵营类型

| 类型 | 说明 |
|------|------|
| Neutral | 中立 |
| Arabian | 阿拉伯 |
| Castle | 城堡 |
| Dungeon | 地牢 |
| Forest | 森林 |
| Inferno | 地狱 |
| Necropolis | 亡灵 |

## 英雄职业

| 职业 | 说明 |
|------|------|
| Warrior | 战士 - 高攻击/防御 |
| Mage | 法师 - 高法力/知识 |
| Ranger | 游侠 - 平衡型 |
| Cleric | 牧师 - 支援型 |

## 兵种等级

| 等级 | 说明 |
|------|------|
| Tier 1 | 基础兵种 (农民、骷髅等) |
| Tier 2 | 进阶兵种 (剑士、弓箭手等) |
| Tier 3 | 精锐兵种 (骑士、法师等) |
| Tier 4 | 高级兵种 (狮鹫、恶魔等) |
| Tier 5 | 顶级兵种 (巨龙、天使等) |

## 资源类型

| 类型 | 说明 |
|------|------|
| gold | 金币 |
| wood | 木材 |
| ore | 矿石 |
| crystal | 水晶 |

## 工作流程

1. 复制 `neutral/` 文件夹作为模板
2. 重命名为对应阵营 (如 `arabian/`)
3. 修改 `faction.yaml` 中的阵营信息
4. 在 `heroes/` 中为每个英雄创建文件夹，包含 `config.yaml` 和图片资源
5. 在 `units/` 中为每个兵种创建文件夹，包含 `config.yaml` 和图片资源
6. 在 `buildings/` 中为每个建筑创建文件夹
7. 提交到 Git

## 注意事项

- YAML 文件使用 UTF-8 编码
- 缩进使用 2 个空格
- 字符串包含特殊字符时使用引号
- 颜色使用十六进制格式: `"#FF0000"`
- 图片格式统一使用 PNG
