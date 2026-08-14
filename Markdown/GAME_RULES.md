# GAME_RULES.md

# Core Loop

Preparation -> Combat -> Preparation, until the Core is destroyed.

On each Preparation entry:

- Grant 3-4 blocks from `BlockSupplyData`.
- Reset the reroll lock.
- Permit placement, dismantling, and shop interaction.

During Combat, placement and dismantling are disabled.

---

# Placement

- Blocks occupy exactly one Grid cell.
- Only one Block may occupy a cell.
- The Core is placed automatically at the Grid center and cannot be moved or dismantled.
- On initial Core creation, random basic blocks are placed in its Up, Down, Left, and
  Right cells using the configured basic supply pool. Duplicates are allowed.
- Starting basic blocks follow normal matching, combat, and dismantling rules.
- A granted item is consumed only after successful placement.
- Placement has no currency cost.
- Unused granted items are discarded when Combat begins.

---

# Basic Blocks and Matching

- Basic blocks contain HP and `BlockColorData`.
- They have no active Combat behavior.
- Only basic blocks participate in matches.
- Same-color connectivity uses Up, Right, Down, and Left only.
- A match check starts after a successful basic-block placement.
- The search starts from the last placed block and consumes the rule's required count.
- The completed skill block appears at the last placed cell.
- Its initial HP ratio is the arithmetic mean of each consumed block's current HP divided
  by that block's own maximum HP.
- Skill blocks do not participate in later matches.

Default rules are Red -> Attack, Blue -> Support, and Green -> Healer, with a count of 3.

When a match is committed, its source cells and result cell are temporarily interaction
locked until the result block's visual reveal completes. The Grid replacement itself remains
immediate, and other cells remain available for placement, dismantling, and hover interaction.

---

# Grant and Rare Rules

- Grant size, basic weights, rare weights, and rare chance are Inspector data.
- Each granted slot rolls rare chance independently.
- Each reroll adds `0.01` to the rare chance used by every newly generated slot for the
  remainder of that Preparation phase.
- A rare result grants a completed skill block directly.
- Rare results play a highlight animation in the grant UI and when placed.

---

# Reroll

- A reroll replaces every currently granted block.
- The Nth reroll immediately subtracts N from current Core Energy, so the debt after N rerolls
  is `-N * (N + 1) / 2` while the base required Energy remains unchanged.
- The reroll button displays the next cost before use. It becomes non-interactable, while
  remaining visible, when that reroll would make the debt exceed the base required Energy.
- Using any granted block locks reroll for the rest of that Preparation phase.
- Entering a new Preparation phase resets the lock state.

---

# Dismantling

The Core cannot be dismantled. Dismantling removes a block without granting currency or
returning it to the current supply. Match consumption is not treated as dismantling.

---

# Supply Event

- After eligible completed waves, data controls appearance chance and minimum/maximum interval.
- A 1 HP Supply Block is placed in a random valid cardinal neighbor of an existing block.
- The next wave adds a configurable ratio of Supply hunters, minimum one by default.
- Supply hunters route to Supply first, then rebuild a route to Core if Supply is destroyed.
- Surviving through its assigned wave opens three unique, free offers; destruction or
  dismantling grants no reward.
- The selected completed-block offer reserves one slot in the next Preparation grant.
- Guaranteed blocks never increase the grant beyond the
  configured maximum; remaining slots retain the normal weighted/rare rolls.

---

# Core Energy and Shockwave

- Each Enemy grants its configured integer `EnergyOnDeath` through a world pickup to Core.
- Core also gains the configured integer energy per second during Combat.
- Every Preparation starts at zero with a base required-energy value rolled from difficulty data.
- Rerolls can make current Energy negative. Combat starts from that signed value, and incoming
  Energy first raises it toward zero before building positive Shockwave charge.
- Reaching the goal immediately starts the existing Shockwave and clears the wave.
- At the exact Shockwave release, current Energy is set to zero so the charge UI falls with the
  release. Returning to Preparation still performs the normal zero initialization and maximum refresh.
- The Energy UI remains visible during Preparation and Combat. It observes the same current
  Energy source for signed text, normal positive fill, and red negative fill; both fills are
  clamped to the base required value.

---

# Combat, Enemy, and Camera

Existing automatic Combat, Grid movement, enemy overlap rules, health bars, camera controls,
time scale, Core death presentation, and Game Over behavior remain unchanged.

When a wave starts, all normal-enemy spawn positions and routes are fixed before delayed
spawning begins. Route selection follows these rules:

- Find the shortest movement distance to a cardinal Core-adjacent cell, ignoring block occupancy.
- Consider only paths within `shortest distance + PathLengthTolerance` (default 2).
- Prefer the candidate containing the fewest occupied non-Core block cells.
- If block counts tie, prefer the shorter candidate.
- If both values tie, select randomly using that enemy's navigation seed.

An Enemy follows the stored route for the full wave. Grid placement/removal/destruction never
recalculates it. When a block on that route is reached, the Enemy attacks it and listens for its
death; after removal it continues to the next stored cell. Ranged retreat is limited to cells on
the same stored route.

Melee and Ranged enemies may be instantiated outside the Grid, but their first movement target
must be the first in-bounds cell of the stored route. An out-of-bounds cell is never accepted as
a movement destination.

All Blocks refresh their world sprite and health-bar color when HP changes. Basic Blocks use
the five `Blocks-Sheet_0` through `Blocks-Sheet_4` damage stages from highest to lowest HP.
Blocks without dedicated damage art repeat their existing sprite across all five stages.
Health bars use discrete healthy (green), warning (orange), and critical (red) bands.

Damage destruction plays a pooled fragment effect tinted from the destroyed Block's
`BlockColorData`. Dismantling and match consumption do not count as damage destruction.
The pool reuses inactive effects first and expands from its configured prefab when all effects
are concurrently active, so no valid damage-destruction request replaces an active effect.
Support and Healer area targets refresh on Grid occupancy changes; Healer priority also
refreshes when an adjacent Block's HP changes.

Every valid Enemy damage event restarts that Enemy's pre-created hit particles together with
the normal damage feedback. Special Attack, Support, and Healer match results play their
configured merge sound through `AudioManager` at the same instant as the Merge Burst.

---

# Hover Readability

- Supply items and placed world Blocks use one shared tooltip view.
- The tooltip displays name, description, HP, and role-specific values.
- Hovering a placed area-effect Block displays its configured Grid effect range.
- Range-less Blocks never display an effect range.
- World range visualization hides immediately when the pointer leaves the Block.

---

# Next-Wave Spawn Preview

- Next-wave spawn positions are fixed when Preparation begins.
- Each unique position is marked by a red ring in world space until Combat starts.
- Block placement/removal and camera movement never regenerate or move stored positions.
- A reserved spawn cell cannot accept a block.
- Enemies spawn from those same stored positions. Routes are calculated at Combat start so they
  still reflect the final Grid layout.
