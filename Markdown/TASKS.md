# TASKS.md

# Prototype Development Roadmap

## Phase 1 - Foundation

* [x] Create project folder structure
* [x] Create Grid system
* [x] Create Grid Cell
* [x] Create Block base class
* [x] Create Core Block
* [x] Allow block placement
* [x] Visualize grid

---

## Phase 2 - Enemy

* [x] Create Enemy base class
* [x] Enemy movement
* [x] Enemy target selection
* [x] Enemy attack
* [x] Block destruction

---

## Phase 3 - Wave

* [x] Wave manager
* [x] Enemy spawning
* [x] Wave complete detection
* [x] Rearrangement phase

---

## Phase 4 - Core Gameplay

* [x] Block adjacency system
* [x] Passive abilities
* [ ] Multiple enemy types
* [x] Core destruction
* [x] Game Over

---

## Phase 5 - UI

* [x] HP bars
* [x] Placement UI
* [x] Wave UI
* [x] Basic feedback

---

## Phase 6 - Data Architecture & Placement UX Overhaul

* [x] Convert Block system to ScriptableObject-based data system
* [x] Convert Enemy system to ScriptableObject-based data system
* [x] Add adjacency configuration system for blocks (4-direction selectable flags)
* [x] Add block stats into data assets (HP, damage, heal amount, cost, cooldown, etc.)
* [x] Add enemy stats into data assets (HP, speed, attack, priority behavior)
* [x] Refactor runtime Block/Enemy to reference ScriptableObject data

---

* [x] Implement block placement preview system (ghost preview)
* [x] Implement drag-to-place block placement from UI
* [x] Implement continuous placement mode (does not cancel after placing one block)
* [x] Implement right-click block deletion in grid

---

* [x] Implement block drag-and-drop reposition system
  - Existing placed blocks can be held and dragged
  - Block follows mouse with transparent preview
  - On release, block is relocated if valid

---

* [x] Add placement visualization system
  - Show adjacency effect directions (up/down/left/right indicators)
  - Show attack range radius for AttackBlock
  - Show heal/support range indicators when placing blocks

---

## Rules for this phase

- Do NOT split placement system into multiple independent systems
- Keep all placement logic centralized in PlacementController or equivalent
- ScriptableObject is the single source of truth for all balance data
- Do NOT modify combat system unless required for data integration

---

# Phase 7 - Combat Readability & Enemy Variety

* [x] Add diagonal and Everything effect directions
* [x] Change AttackBlock to persistent random grid-area targeting
* [x] Add animated laser feedback to AttackBlock
* [x] Add health-scaled dismantle refund and hover preview
* [x] Add RangedEnemy and curved missile attacks
* [x] Add shared hit flash and shake feedback for Blocks and Enemies

---

# Phase 8 - Grid Movement & Placement Polish

* [x] Correct Everything visualization to `(EffectRange * 2 + 1)` cells
* [x] Add enemy destination-cell reservation (superseded by Phase 10)
* [x] Replace free enemy movement with cell-based movement
* [x] Replace path search with A*
* [x] Add DOTween placement and dismantle animations
* [x] Block all placement input while the pointer is over UI

---

# Phase 9 - Enemy Surrounding & Camera Controls

* [x] Reserve separate final target cells for Enemies (superseded by Phase 10)
* [x] Select the closest reachable cell when Core access is unavailable
* [x] Recalculate paths after blockers or target availability change
* [x] Add smooth middle-mouse camera panning
* [x] Add smooth mouse-wheel zoom with limits
* [x] Return camera to Core on Preparation and GameOver

---

# Phase 10 - Reservation-free Enemy Flow

* [x] Remove Enemy occupancy and all cell reservations from GridManager
* [x] Allow Enemies to share paths, cells, and destinations
* [x] Add a persistent per-Enemy visual cell offset
* [x] Add weak mathematical separation steering
* [x] Preserve A*, Block destruction, targeting, and wave behavior

---

# Phase 11 - Enemy Path Diversity & Feedback Stability

* [x] Add stable per-Enemy A* tie-breaking
* [x] Distribute equivalent Core approaches and blocking targets
* [x] Limit preferred-route detours to four cells
* [x] Use only nearby boundary entries for outside-Grid spawns
* [x] Prevent repeated DamageFeedback calls from accumulating visual drift

---

# Phase 12 - Specialized Data & Prefab Runtime

* [x] Split BlockData into role-specific data types
* [x] Split EnemyData into MeleeEnemyData and RangedEnemyData
* [x] Keep BlockProperty as an automatic targeting/filtering tag
* [x] Replace runtime GameObject/component construction with prefabs
* [x] Add phase-aware Block health-bar visibility
* [x] Add configurable health thresholds, colors, duration, and layout
* [x] Move remaining touched gameplay and presentation values to serialized settings

---

# Current Task

Data specialization, prefab migration, and phase-aware Block health bars complete.

Always complete one feature before moving to the next.

Do not skip ahead.

Do not implement future systems unless requested.

Always update this file when a task is completed.
