# GAME_DESIGN.md

# Project Overview

## Genre

Top-Down Defense / Match-Based Strategy / Roguelite

## High Concept

The player protects a Core by arranging a small random set of colored basic blocks.
Three connected blocks of the same color merge into a completed skill block.
Combat is automatic; the player's decisions are placement, reroll timing, dismantling,
and Supply reward choices.

The game emphasizes planning rather than fast reactions.

Enemy approach directions are predictable from the completed Preparation layout. At wave
start, every normal enemy commits to a route from its already-selected spawn position and does
not switch routes when blocks are destroyed later in the wave.

---

# Core Gameplay Loop

1. Enter Preparation and receive 3-4 blocks.
2. Decide whether to reroll the complete grant before using a block.
3. Place granted blocks on the Grid.
4. Connect same-colored basic blocks to create skill blocks.
5. Start the wave and observe automatic combat.
6. Return to Preparation, receive a new grant, and repeat.
7. After eligible waves, defend a Supply Block and choose a free reward if it survives.

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
- Placement has no currency cost.

---

# Basic Colored Blocks

Basic blocks have HP and act as solid obstacles, but have no active ability.
Their color is defined by `BlockColorData`, not by hardcoded enums.

Their visible damage state advances through five `Blocks-Sheet` sprites as HP decreases.
Every Block uses the same health-visual update path; skill and Core blocks currently retain
their normal sprite at every damage stage until dedicated damaged variants are provided.

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
- A completed skill block inherits the average remaining-HP ratio of the consumed basic
  blocks, applied against the result block's own maximum HP.

Match conversion is presented as a short SF energy-reconstruction sequence: source blocks
become white silhouettes, converge and compress at the result cell, then a flash, shockwave,
and particles reveal the completed block. This presentation does not delay or alter the
underlying match result. A dedicated completion sound plays at the exact burst frame when an
Attack, Support, or Healer block is revealed.

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

Support and Healer targeting reacts to Grid occupancy changes rather than rescanning the
surrounding Grid every frame. Healer priority also reacts when an adjacent block's HP changes.

When damage completely destroys a block, color-matched fragments burst from its center,
follow random parabolic paths, then darken and fade while descending. Dismantled blocks and
blocks consumed by matching do not use this destruction presentation.

Enemy hits add a compact laser-impact spark: a small number of bright yellow/orange embers
burst from the Enemy over a narrow area and disappear quickly alongside the existing hit flash.

Normal enemy routes favor their spawn direction: paths no more than two cells longer than the
shortest approach are considered, then the route requiring the fewest destroyed blocks wins.
Distance breaks remaining ties, followed by random selection for completely equal routes.

---

# Rerolls and Dismantling

- Rerolls replace the entire current grant.
- Rerolls immediately subtract Energy from the current Preparation state. The first costs 1,
  the second 2, the third 3, and so on, so repeated rerolls create visible negative Energy debt.
- Every reroll also raises the current Preparation grant's rare-block chance by `0.01`.
- The reroll label shows the next cost before the player commits. The button remains visible but
  becomes non-interactable when its cost would push total debt past the base required Energy.
- Reroll becomes permanently unavailable for the phase after one granted block is used.

- Dismantling removes the block without returning currency or an inventory item.

---

# Supply Events and Rewards

After a completed wave, a data-driven chance and minimum/maximum interval can create a 1 HP
Supply Block in a random empty cardinal neighbor of any existing block. The event is announced
only after a valid cell is found and placement succeeds.

When a new Supply Block appears, its pulse/burst reveal and a short recognition hold finish
before the next block grant and Placement phase begin. If no Supply event is created, the game
enters Preparation immediately with no added delay.

The next wave adds Supply hunters equal to the configured ratio of the normal wave population
(20%, minimum one by default). They precompute a route to the Supply Block; if it is destroyed,
they rebuild a route from their current position to the Core.

If the Supply Block survives its assigned wave, it is removed and the existing offer UI shows
three free, unique choices. One chosen completed block is guaranteed in a slot of the next
3-4 block grant; it does not increase the grant count. Destruction or player dismantling fails
the event and produces no offer.

# Core Energy and Shockwave

Enemies grant the integer energy configured in `EnemyData`. A world-space pickup travels from
the defeated enemy to the Core, where a distinct absorption pulse plays before the value is
added. The Core also gains a configurable integer amount per second during Combat.

Preparation resets current Energy to zero and rolls a base required-Energy goal from
`WaveDifficultyData`. Combat preserves any negative reroll debt and applies all incoming Energy
to that debt before positive Shockwave charge. Reaching the unchanged base goal immediately
starts the existing Shockwave clear presentation. One always-visible Energy UI shows the signed
value, base goal, normal positive fill, and a clamped red negative fill using two reusable
`DelayedFillGauge` instances.

When the completed Shockwave is released, current Energy drops to zero at the burst itself.
The Stage Clear pulse and ring are unique scene instances that are activated and replayed for
each clear, avoiding repeated creation and destruction without requiring a general-purpose pool.

---

# Design Priorities

1. Strategic placement
2. Meaningful use of limited random pieces
3. Readable matches and skill ranges
4. Interesting reroll and Supply-reward decisions
5. Replayability through changing grants

Do not reintroduce free block purchasing or placement costs into the normal loop.

During Preparation, red world-space rings reveal every unique spawn position for the next wave.
They appear with one clear pulse, continue with a subtle pulse, and disappear when Combat begins.
Their positions are fixed for the whole Preparation phase and are the exact positions used by
the wave spawner, allowing placement decisions to account for the incoming attack direction.

World blocks and Supply items expose the same tooltip information. Hovering a placed
skill block also displays the same effect-area visualization used during placement.
