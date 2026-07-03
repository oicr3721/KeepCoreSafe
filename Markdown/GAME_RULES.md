# GAME_RULES.md

# Prototype Scope

This document defines the gameplay rules for the prototype only.

The goal of this prototype is to verify whether the core gameplay loop is fun.

Do not implement future features unless explicitly requested.

---

# Core Gameplay Loop

1. Player places blocks on the grid.
2. Combat begins.
3. Enemies automatically attack.
4. If the Core is destroyed, the game ends.
5. If all enemies are defeated, the wave ends.

---

# Grid

- The battlefield uses a square grid.
- Blocks occupy grid cells permanently until moved or destroyed.
- Multiple Blocks cannot occupy the same cell.
- Enemies move between grid cells.
- Multiple Enemies may move through, stand in, and target the same cell.
- The Grid tracks Block occupancy only; Enemies never reserve cells.

---

# Core

- Exactly one Core Block must exist.
- The Core can be placed anywhere on the grid.
- If the Core HP reaches zero,
  the game is over.

---

# Blocks

Every block has:

- Max HP
- Current HP
- BlockProperty

Every block occupies exactly one grid cell.

Prototype Blocks

- Core
- Wall
- Healer

---

# BlockProperty

Do NOT use Unity Tag for gameplay logic.

Use a Flags Enum named BlockProperty.

A block may have multiple properties.

Example

Wall | Mechanical

Healer | Mechanical

Core

Future blocks may contain any combination of properties.

---

# Adjacency

Adjacency is selected by BlockData flags.

- Up
- Down
- Left
- Right
- Four diagonal directions
- Cardinal and diagonal grouped categories
- Everything square range

EffectRange controls the number of grid cells affected.

---

# Combat

Combat is fully automatic.

The player cannot move blocks during combat.

Every unit attacks using Attack Cooldown.

Combat runs in real time.

---

# Enemy

Prototype contains two enemy types.

- Melee Enemy
- Ranged Enemy

Ranged enemies keep their configured attack distance and fire curved missiles.

---

# Enemy Target Selection

Every enemy owns

- Detection Radius
- Priority List

Target selection process

1. Search every block inside Detection Radius.
2. Sort candidates using Priority List.
3. Select the highest priority target.
4. Continue attacking until the target is destroyed.
5. When the target disappears,
   repeat the search.

Enemies NEVER search the entire map.

Priority only affects targets already inside Detection Radius.

---

# Enemy Movement

Enemies use A* paths over the Grid.

- Blocks are obstacles.
- Enemies move one cell at a time.
- If no open route exists, enemies attack a blocking Block.
- If a Core approach is unavailable, enemies select the closest reachable block-free cell.
- Paths are recalculated after the blocking Block is destroyed.
- A fixed per-Enemy cell offset and weak separation steering reduce visual stacking.
- Separation never changes the A* path and does not use Enemy-to-Enemy collisions.
- Equal-cost paths and equivalent blocking targets are distributed by a stable per-Enemy seed.
- A preferred Core approach may be used when it is no more than four cells longer than the shortest path.
- Enemies outside the Grid enter from the nearest boundary rather than considering the full perimeter.

---

# Camera

- Middle-mouse drag pans in Preparation and Combat.
- Mouse-wheel input controls smooth orthographic zoom.
- Preparation and GameOver return the camera to the Core and default zoom.

---

# Damage

Blocks receive damage.

If Current HP reaches zero,

the block is destroyed.

Destroyed blocks disappear immediately.

---

# Healing

Healer blocks restore HP to nearby friendly blocks.

Healing cannot increase HP above Max HP.

---

# Game States

Preparation

↓

Combat

↓

Game Over

The prototype does not require additional game states.

---

# Prototype Goal

The prototype should verify

- Grid placement
- Enemy targeting
- Automatic combat
- Block destruction
- Healing
- Basic adjacency system

without implementing advanced gameplay systems.
