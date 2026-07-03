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
- Occupancy check

The Grid only knows which Block occupies each cell.

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

A block may contain multiple properties.

Example

Wall | Mechanical

---

# Enemy System

Every enemy derives from Enemy.

Prototype

Enemy

↓

MeleeEnemy

Future

Enemy

↓

RangedEnemy

Enemy is responsible for

- Movement
- Target Selection
- Attacking

Enemy does NOT know the Grid.

Enemy moves freely in world space.

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

Adjacency checks only

- Up
- Down
- Left
- Right

Diagonal cells are ignored.

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
- Target Selection
- Damage
- Healing
- Game Over

Everything else should be implemented later.