# GAME_RULES.md

# Core Loop

Preparation -> Combat -> Preparation, until the Core is destroyed.

On each Preparation entry:

- Grant 3-5 random blocks from `BlockSupplyData`.
- Reset the reroll count and reroll lock.
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
- Placement costs zero points.
- Unused granted items are discarded when Combat begins.

---

# Basic Blocks and Matching

- Basic blocks contain HP, a dismantle value, and `BlockColorData`.
- They have no active Combat behavior.
- Only basic blocks participate in matches.
- Same-color connectivity uses Up, Right, Down, and Left only.
- A match check starts after a successful basic-block placement.
- The search starts from the last placed block and consumes the rule's required count.
- The completed skill block appears at the last placed cell.
- Skill blocks do not participate in later matches.

Default rules are Red -> Attack, Blue -> Support, and Green -> Healer, with a count of 3.

---

# Grant and Rare Rules

- Grant size, basic weights, rare weights, and rare chance are Inspector data.
- Each granted slot rolls rare chance independently.
- A rare result grants a completed skill block directly.
- Rare results play a highlight animation in the grant UI and when placed.

---

# Reroll

- A reroll replaces every currently granted block.
- Rerolls spend PlacePoint.
- Cost is `InitialCost + RerollCount * CostIncrease`.
- Using any granted block locks reroll for the rest of that Preparation phase.
- Entering a new Preparation phase resets cost and lock state.

---

# Dismantling

Refund is:

`floor(DismantleValue * RefundRate * CurrentHP / MaxHP)`

The Core cannot be dismantled. Match consumption never gives a dismantle refund.

---

# Shop

- The default schedule begins after Wave 3 and repeats every 3 waves.
- Schedule, explicit extra waves, offer count, offers, and costs are ScriptableObject data.
- A purchase spends PlacePoint only if its offer effect can be applied.
- The prototype offer effect grants a completed skill block to the current grant.

---

# Combat, Enemy, and Camera

Existing automatic Combat, Grid A*, enemy overlap rules, health bars, camera controls,
time scale, Core death presentation, and Game Over behavior remain unchanged.

---

# Hover Readability

- Supply items and placed world Blocks use one shared tooltip view.
- The tooltip displays name, description, HP, dismantle value, and role-specific values.
- Hovering a placed area-effect Block displays its configured Grid effect range.
- Range-less Blocks never display an effect range.
- World range visualization hides immediately when the pointer leaves the Block.
