# GAME_DESIGN.md

# Project Overview

## Genre

Top-Down Defense / Match-Based Strategy / Roguelite

## High Concept

The player protects a Core by arranging a small random set of colored basic blocks.
Three connected blocks of the same color merge into a completed skill block.
Combat is automatic; the player's decisions are placement, reroll timing, dismantling,
and occasional shop choices.

The game emphasizes planning rather than fast reactions.

---

# Core Gameplay Loop

1. Enter Preparation and receive 3-5 random blocks.
2. Decide whether to spend points rerolling the complete grant.
3. Place granted blocks on the Grid.
4. Connect same-colored basic blocks to create skill blocks.
5. Start the wave and observe automatic combat.
6. Return to Preparation, receive a new grant, and repeat.
7. On configured waves, choose from a data-driven shop event.

If the Core is destroyed, the run ends immediately.

At run start, four random basic blocks from the active supply pool are placed in the
cardinal cells around the Core. They are normal matchable and dismantlable blocks.

---

# Granted Blocks

The player cannot freely select or purchase arbitrary blocks during normal placement.
Each Preparation phase grants a random set from `BlockSupplyData`.

- Grant count, weights, and rare chance are configurable.
- A rare roll may grant an already-completed skill block.
- Placing a granted block consumes that single granted item.
- Placement itself costs no points.

---

# Basic Colored Blocks

Basic blocks have HP and act as solid obstacles, but have no active ability.
Their color is defined by `BlockColorData`, not by hardcoded enums.

Prototype colors:

- Red
- Blue
- Green
- Yellow (available as data; not in the default pool until a match rule is assigned)

Colors and their skill results are separate data, so either can be extended independently.

---

# Match Conversion

- Matching uses cardinal adjacency only; diagonals never connect.
- A match begins from the block most recently placed by the player.
- The default requirement is three blocks, configurable per rule.
- If four or more blocks are connected, only the required number discovered from the
  most recently placed block is consumed.
- The result skill block is placed at the most recently placed block's cell.
- Completed skill blocks cannot match or transform again.

Default mappings:

- Red -> AttackBlock
- Blue -> SupportBlock
- Green -> HealerBlock

Mappings and required counts are configured in `BlockMatchData`.

---

# Skill Blocks

Skill blocks are completed buildings. During Combat they continuously perform their
existing timer-based behavior:

- AttackBlock attacks enemies in its configured area.
- HealerBlock repairs blocks in its configured area.
- SupportBlock reduces adjacent skill cooldowns.

Skill blocks are never treated as basic match pieces.

---

# Points and Rerolls

Points are not a placement currency. They are reserved for rerolls, shop purchases,
upgrades, and future special actions.

- Rerolls replace the entire current grant.
- Reroll cost increases after every reroll in the same Preparation phase.
- Cost resets when a new Preparation phase begins.
- Reroll becomes permanently unavailable for the phase after one granted block is used.

Dismantling still returns a configured portion of the block's dismantle value, scaled
by its remaining HP ratio.

---

# Shop Events

Shop events open after configured waves. Their schedule and offer list are data-driven.
The initial concrete offer type grants a completed skill block to the current grant.
Future upgrades should be implemented as new `ShopOfferData` subtypes without changing
the shop schedule, payment, or UI flow.

---

# Design Priorities

1. Strategic placement
2. Meaningful use of limited random pieces
3. Readable matches and skill ranges
4. Interesting reroll and shop decisions
5. Replayability through changing grants

Do not reintroduce free block purchasing or placement costs into the normal loop.

World blocks and Supply items expose the same tooltip information. Hovering a placed
skill block also displays the same effect-area visualization used during placement.
