# ARCHITECTURE.md

# Goal

This document defines the architecture of the prototype.

Keep the implementation simple.

Do NOT over-engineer the project.

Only implement what is required for the prototype.

---

# Architecture Principles

- Single Responsibility Principle
- Composition over inheritance whenever reasonable
- Keep Managers lightweight
- Event-driven when useful
- Avoid unnecessary abstraction

---

# Folder Structure

Assets/

    Scripts/

        Core/

        Grid/

        Block/

        Enemy/

        Combat/

        Managers/

        UI/

---

# High Level Architecture

GameManager

↓

GridManager

↓

Grid

↓

Blocks

↓

Enemies

GameManager controls the game flow only.

Managers should coordinate systems instead of containing gameplay logic.

---

# Game Flow

Preparation

↓

Combat

↓

GameOver

GameManager is responsible for changing game states.

---

# Grid System

Responsible for

- Grid creation
- Cell lookup
- Block placement
- Block movement
- Block occupancy check

The Grid only knows which Block occupies each cell.

Enemy occupancy and destination state are deliberately not stored in GridManager.

The Grid never handles combat logic.

---

# Block System

Every block derives from Block.

Prototype Blocks

- CoreBlock
- WallBlock
- HealerBlock

Every Block contains

- Max HP
- Current HP
- BlockProperty

Block runtime behavior is selected by its prefab component and matching data subtype.

BlockData hierarchy

- BlockData: display name, HP, cost, sprite, prefab
- AreaBlockData: effect range and affected directions
- TimedAreaBlockData: action cooldown
- AttackBlockData: attack value
- HealerBlockData: heal value
- SupportBlockData: cooldown multiplier
- CoreBlockData / WallBlockData: common data only

Every Block exposes

- TakeDamage()
- Heal()
- Die()

Blocks should not directly control other systems.

---

# BlockProperty

Gameplay logic must NOT use Unity Tags.

Use a Flags Enum.

Example

BlockProperty

- Core
- Wall
- Healer
- Mechanical

A block may contain multiple properties when future targeting tags require it.

Example

Wall | Mechanical

The primary role property is supplied by the concrete BlockData subtype rather than
being selected manually. BlockProperty remains a targeting and filtering tag; it no
longer decides which runtime component is created.

---

# Enemy System

Every enemy derives from Enemy.

Prototype

Enemy

↓

MeleeEnemy

Additional prototype enemy

Enemy

↓

RangedEnemy

Enemy is responsible for

- Movement
- Target Selection
- Attacking

EnemyData contains only shared movement, health, attack, visual, priority, and prefab data.
MeleeEnemyData identifies melee configuration, while RangedEnemyData alone exposes
range, tolerance, missile speed, arc height, and projectile prefab fields.

Enemy movement queries the Grid for A* paths, but does not reserve or occupy cells.

Enemies move one cell at a time with smooth world-space interpolation between cells.

When a Core approach is unavailable, pathfinding selects the closest reachable,
block-free cell to the Core as the Enemy's fallback target.

Each Enemy keeps one visual offset inside a cell. A weak mathematical separation
steering force reduces visual overlap without changing paths or using collisions.

Each Enemy also owns a stable navigation seed. A* remains cost-based, while equal-cost
cells, nearby Core approaches, and equivalent blocking Blocks use that seed as a
deterministic tie-breaker. This distributes pressure without restoring reservations.

Enemies spawned outside the Grid enter through the nearest local boundary cells only.

---

# Camera

The Main Camera owns a single GameCameraController component.

- Middle-mouse drag pans the camera.
- Mouse-wheel input changes orthographic zoom.
- Preparation and GameOver smoothly return focus to the Core and default zoom.

---

# Target Selection

Enemy owns

- Detection Radius
- Priority List

Target selection flow

Search nearby Blocks

↓

Sort using Priority

↓

Select Target

↓

Attack until destroyed

↓

Search again

Priority never causes an enemy to search outside its detection radius.

---

# Combat System

Combat is automatic.

Player input is disabled during combat.

Combat responsibilities

- Damage
- Healing
- Death

Combat should not manage movement.

---

# Adjacency System

Adjacency is configured per BlockData with flags.

- Four cardinal directions
- Four diagonal directions
- Cardinal and diagonal category flags
- Everything square mode covering `(EffectRange * 2 + 1)` cells per axis

The adjacency system should be independent from individual block implementations.

Blocks ask the adjacency system for nearby blocks.

---

# Managers

Managers should coordinate systems.

Managers should not contain gameplay rules.

Prototype Managers

GameManager

Controls game state.

GridManager

Owns the Grid.

CombatManager

Starts and ends combat.

WaveManager

Spawns enemies.

Managers communicate through events when appropriate.

---

# Data vs Runtime

Separate static data from runtime state.

Examples

Static

- Max HP
- Damage
- Heal Amount
- Detection Radius

Runtime

- Current HP
- Current Target
- Cooldowns
- Position

Do not store runtime values inside ScriptableObjects.

Runtime gameplay objects are instantiated from configured prefabs. BlockData and
EnemyData reference their matching prefabs, RangedEnemyData references its projectile,
and scene systems reference the health-bar and grid-line prefabs. Runtime scripts do
not construct gameplay GameObjects or attach required components dynamically.

---

# Future Extension

The architecture should allow adding

- New Block types
- New Enemy types
- Additional BlockProperty values
- New adjacency effects

without modifying existing gameplay systems whenever possible.

Do not implement these extensions yet.

Only keep the architecture compatible with them.

---

# Prototype Scope

The prototype only requires

- Grid
- CoreBlock
- WallBlock
- HealerBlock
- MeleeEnemy
- RangedEnemy
- Target Selection
- Damage
- Healing
- Game Over

Everything else should be implemented later.
