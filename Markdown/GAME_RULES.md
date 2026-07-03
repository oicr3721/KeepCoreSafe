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
- Only Blocks occupy grid cells.
- Multiple Blocks cannot occupy the same cell.
- Enemies do NOT use the grid.
- Enemies move freely in world space.

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

Adjacency uses only the four cardinal directions.

- Up
- Down
- Left
- Right

Diagonal tiles are NOT adjacent.

---

# Combat

Combat is fully automatic.

The player cannot move blocks during combat.

Every unit attacks using Attack Cooldown.

Combat runs in real time.

---

# Enemy

Prototype contains only one enemy type.

- Melee Enemy

Ranged enemies will be implemented later.

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

Enemies move freely.

Enemies are NOT restricted by the grid.

Movement implementation is intentionally left simple for the prototype.

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