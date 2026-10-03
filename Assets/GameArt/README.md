# GameArt — Asset Architecture & Organization Guide

This folder contains the **clean, production-ready art assets** for Hollow Signal.  
Legacy/unprocessed assets remain in `Assets/Art/` and can be migrated here as they are validated and standardized.

---

## 1. Directory Structure Overview

```
Assets/GameArt/
├── _Shared/
│   ├── Materials/                  # Shared base materials (Trim sheets, generic metals, glass)
│   ├── Textures/
│   │   └── TrimSheets/             # Master Trim Sheets (Industrial, Concrete, Arcanepunk)
│   └── Shaders/                    # Custom shaders (URP Lit variants, Planar Inversion FX)
│
├── Environment/
│   ├── MapBases_Modular/           # Snap-to-grid architectural pieces for level layouts
│   │   ├── Floors/                 # 2x2m, 4x4m floor tiles, metal gratings, toxic canal edges
│   │   ├── Walls/                  # 2m/4m modular walls, low half-walls (1m), doorways
│   │   ├── Catwalks_Stairs/        # Suspended steel catwalks, industrial stairs, railings
│   │   └── Columns_Arches/         # Structural support pillars, ceiling ribs, archways
│   │
│   ├── Buildings_Exterior/         # Kitbash structures for exterior vistas & streetscapes
│   │   ├── Towers_HighRises/       # Dieselpunk spires, ventilation towers, smokestacks
│   │   ├── Monoliths_Ruins/        # Heavy brutalist blocks, precursor stone structures
│   │   └── Facades_Backdrops/      # Single-sided exterior building facades, boundary walls
│   │
│   └── Props/                      # World set dressing
│       ├── Industrial/             # Boilers, steam pipes, valves, generators, conduit runs
│       ├── Clutter_Containers/     # Crates, oil drums, pallets, debris piles, tool racks
│       ├── Interior_Furniture/     # Desks, lockers, cots, chairs, interrogation tables
│       └── Interactive/            # Terminals, lever consoles, locked gates, safes
│
├── Characters/
│   ├── PC/                         # Playable hero models (Brute, Leader, etc.)
│   ├── NPC/                        # Non-hostile NPCs, sentries, city merchants
│   ├── Enemies/                    # Hostile automatons, corrupted biometal fauna, cultists
│   └── Animations/                 # Rigged humanoid locomotion, combat, dialogue gestures
│
├── VFX/                            # Particle systems (steam leaks, sparks, toxic fluid glow)
└── UI/                             # Dialogue portraits, status icons, cursor sprites
```

---

## 2. Naming Conventions & Prefixes

Always prefix files so they can be identified immediately in search:

| Prefix | Asset Type | Example |
| :--- | :--- | :--- |
| `MESH_` | Static Mesh | `MESH_Wall_4m_Riveted.fbx`, `MESH_Valve_HighPressure.fbx` |
| `SK_` | Skeletal Mesh / Rig | `SK_Hero_Brute.fbx`, `SK_Sentry_Vane.fbx` |
| `MAT_` | Material | `MAT_Industrial_Trim_01.mat`, `MAT_ToxicCanal_Water.mat` |
| `TEX_` | Texture | `TEX_Industrial_Trim_01_BaseColor.png` |
| `A_` | Animation Clip | `A_Walk_Forward.anim` |
| `AC_` | Animator Controller | `AC_Humanoid_Locomotion.controller` |
| `VFX_` | Visual Effect Prefab | `VFX_Steam_PressureLeak.prefab` |

---

## 3. Metric Snapping Standards for Map Bases

- **Grid Size:** $1\text{m}$ base unit.
- **Floors:** $2\text{m} \times 2\text{m}$ or $4\text{m} \times 4\text{m}$, thickness $0.2\text{m}$.
- **Walls:** Height $3.5\text{m}$ to $4\text{m}$. Half-walls / railings $1\text{m}$ high.
- **Doorways:** $2\text{m}$ wide $\times 3\text{m}$ high.
- **Pivots:**
  - Floors: Snapped to bottom corner or exact center.
  - Walls: Snapped to bottom-center edge.
  - Props: Snapped to the bottom footprint ($Y = 0$).

---

## 4. Fixed Camera Rules (30° Pitch / 60° Yaw)

- **Back-face optimization:** Faces pointing away from the camera (North/West) can be left un-detailed or open.
- **Silhouette priority:** Ensure tops and front-facing surfaces have high visual clarity.
- **Low foregrounds:** When building rooms, keep South/East facing walls low ($1\text{m}$) or invisible so the interior is never occluded.
