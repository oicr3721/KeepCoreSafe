# GAME_DESIGN.md

# Project Overview

## Title

(TBD)

## Genre

Top-Down Defense / Strategy / Roguelite

## High Concept

The player designs a defensive structure by placing blocks around a Core.
Combat is fully automatic. The player's role is to build, observe, analyze, and improve the defense structure between waves.

The game emphasizes **strategic placement** rather than real-time control.

---

# Core Philosophy

The most important design principle is:

> **Winning should come from good planning, not fast reactions.**

Players should never feel that they lost because they clicked too slowly.

Instead, every success or failure should be explained by the quality of their defensive layout.

---

# Core Gameplay Loop

1. Arrange blocks.
2. Start the enemy wave.
3. Watch automatic combat.
4. Analyze the result.
5. Rearrange blocks.
6. Repeat.

This loop is the foundation of the game.

---

# Core Objective

Protect the Core for as long as possible.

If the Core is destroyed,
the run immediately ends.

---

# Player Role

The player is **not** a character.

The player is a strategist and architect.

The player does NOT:

* Attack enemies directly
* Control units during combat
* Heal blocks manually
* Reposition blocks during combat

The player DOES:

* Place blocks
* Design defensive formations
* Optimize adjacency bonuses
* Rearrange the structure between waves

---

# Core System

## Grid

The battlefield consists of a square grid.

Blocks can only be placed inside the grid.

Only one block may occupy a cell.

---

## Core Block

There is exactly one Core.

Rules:

* Cannot be removed.
* Cannot be moved.
* Must always exist.
* Game Over when destroyed.

---

## Blocks

Each block has:

* HP
* Type
* Passive ability
* Adjacency effects

When HP reaches zero,
the block is destroyed.

---

## Adjacency System

The placement of blocks is the primary source of strategy.

Blocks gain additional effects depending on neighboring blocks.

Examples:

* Turret next to Generator
* Wall next to Healer
* Special combinations between support blocks

Adjacency effects should encourage thoughtful positioning rather than simple stat increases.

---

## Enemy Waves

Enemies arrive in waves.

Every enemy has:

* Movement behavior
* Target priority
* Attack behavior

Enemies automatically choose targets according to their own rules.

Different enemy types should create different strategic problems.

---

## Combat

Combat is fully automatic.

No direct player control is allowed during battle.

Combat serves as feedback for the player's strategic decisions.

---

## Rearrangement Phase

After each wave,
players may reorganize their defense.

Players may:

* Move existing blocks

Players may NOT:

* Move the Core
* Rearrange blocks during combat

---

# Design Priorities

When implementing new features,
always prioritize these in order:

1. Strategic Placement
2. Readability
3. Interesting Decisions
4. Emergent Synergies
5. Replayability

Never sacrifice strategic depth for unnecessary complexity.

---

# Things To Avoid

Avoid mechanics that make player reflexes more important than planning.

Examples:

* Manual healing
* Active combat abilities
* Micro-management during waves
* High APM gameplay

The game should remain focused on planning and structure optimization.

---

# Success Criteria

Players should constantly ask themselves:

* Where should this block go?
* Which block should protect the Core?
* How can I improve this formation?
* Which adjacency creates the strongest defense?

Instead of:

* How fast can I click?
* Did I use my skill at the perfect timing?

---

# Coding Principles

When implementing gameplay systems:

* Keep systems modular.
* Avoid unnecessary coupling.
* Prefer composition over inheritance.
* Keep gameplay logic independent from UI whenever possible.
* Make systems easy to expand with new block types and enemy types.

---

# Implementation Priority

Prototype order:

1. Grid
2. Block Placement
3. Core
4. Enemy Movement
5. Enemy Attack
6. Block Destruction
7. Wave System
8. Adjacency System
9. Basic UI

Additional systems (leveling, progression, etc.) should only be implemented after the core gameplay loop is complete.

---

# Important Rule For AI

When generating code:

* Do not invent gameplay features.
* Do not add mechanics that are not described in this document.
* If a required design decision is missing, ask before implementing.
* Preserve the game's core philosophy: **planning over execution**.
