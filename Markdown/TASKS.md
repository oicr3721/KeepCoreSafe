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

# Current Task

Data architecture and placement UX overhaul complete.

Always complete one feature before moving to the next.

Do not skip ahead.

Do not implement future systems unless requested.

Always update this file when a task is completed.
