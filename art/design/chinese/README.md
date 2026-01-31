# Han–Tang Civilization (Chinese Faction)

The Han–Tang Civilization is an imperial faction defined by discipline, strategy, and scholarly governance.
It draws inspiration from Han–Tang cultural aesthetics: centralized authority, ordered formations, recorded doctrine,
and a unified philosophy derived from the Five Elements (Wu Xing) as an integrated system.

This package follows the **exact folder and asset delivery structure** of the neutral template.
You may add more content, but you must not remove or omit any required items.

---

## 1. Art Style Specification

### Global Requirements

| Item | Requirement |
|------|------------|
| Overall Style | Semi-realistic game illustration |
| Camera / Perspective | **Isometric (Z as Y)**, **3/4 front** (diagonally facing the camera) |
| Character Proportion | 6–8 heads tall |
| Background | **Transparent PNG** (alpha channel required) |

### Civilization-Specific Variations Allowed

- Faction theme color (`primaryColor`)
- Costume, armor, weapon cultural elements
- Signature ornaments, patterns, and totems

### Visual References (Style Interpretation)

Semi-realistic style = clear anime-like contour line + realistic lighting/material details.
(Comparable to modern high-quality RPG illustrations.)

---

## 2. Folder Structure

```
art/design/
├── README.md
├── chinese/
│   ├── README.md
│   ├── faction.yaml
│   ├── heroes/
│   ├── units/
│   └── buildings/
```

Within each hero/unit/building folder, the **config.yaml** and required PNG assets must follow the naming rules below.

---

## 3. Faction Configuration (`faction.yaml`)

### Required Fields

- `factionType`: string
- `displayName`: string
- `description`: string
- `primaryColor`: hex color string, e.g. `"#D4AF37"`
- `startingResources`: starting resources
- `availableHeroes`: strong references to hero configs
- `factionUnits`: strong references to unit configs
- `factionBuildings`: strong references to building configs

### Strong Reference Format

Use `@...` to reference config assets, e.g.
- `@HeroConfig_chinese_ban_heng`
- `@UnitConfig_chinese_archer_cavalry`
- `@BuildingConfig_chinese_tavern`

---

## 4. Hero Asset Specification

### Required Files per Hero

| Asset Type | Filename Format | Recommended Size | Notes |
|-----------|------------------|------------------|------|
| Portrait | `portrait_{size}.png` | 256x256 | Selection UI and detail panel |
| World Sprite | `worldsprite_{size}.png` | 1024x1024 | Hero rendering on world map |
| Animation Frames | `frame_0001.png` ... `frame_0060.png` | Match `worldsprite` | 60 frames each for `idle` and `move` |

Examples: `portrait_512x512.png`, `worldsprite_1024x1024.png`

### Animation Directory Rules

```
animations/
├── idle_anim_image/
│   ├── frame_0001.png
│   ├── ...
│   └── frame_0060.png
└── move_anim_image/
    ├── frame_0001.png
    ├── ...
    └── frame_0060.png
```

- PNG must be transparent background
- Frames are image sequences (not video files)
- Filenames must be zero-padded: `frame_0001.png` → `frame_0060.png`

---

## 5. Unit Asset Specification

| Asset Type | Filename Format | Recommended Size | Notes |
|-----------|------------------|------------------|------|
| Icon | `icon_{size}.png` | 256x256 | Army list and recruitment UI |
| Battle Sprite | `sprite_{size}.png` | 512x512 | Unit rendering in battle scene |

Examples: `icon_256x256.png`, `sprite_512x512.png`

---

## 6. Building Asset Specification

| Asset Type | Filename Format | Recommended Size | Notes |
|-----------|------------------|------------------|------|
| Building Icon | `icon_{size}.png` | 512x512 | Town building UI |
| Building Sprite | `sprite_{size}.png` | 1024x1024 | Town/world rendering |

---

## 7. Han–Tang Faction Identity (Player-Facing)

### Core Doctrine
- **Order Over Chaos**: stability and hierarchy as the foundation of power
- **Strategy Over Strength**: preparation, formation, and timing decide outcomes
- **Unified Wu Xing**: the Five Elements are expressed as an integrated system of balance, not separate cults

### Visual Motifs
- Bamboo scrolls (doctrine, law, strategy)
- Bow and disciplined cavalry traditions
- Cloud-pattern geometry and central symmetry
- Imperial gold + ceremonial red palette

### Heroes (Lore Overview)
Hero lore documents are provided inside `heroes/<hero_name>/README.md`.

---

## 8. Delivery Checklist

- [ ] Folder structure matches the template (no missing directories)
- [ ] All YAML files are valid and fields complete
- [ ] PNG filenames follow the required formats and include size
- [ ] All PNG assets use transparent backgrounds (alpha)
- [ ] Animation frame sequences contain **60 frames** for `idle` and `move`