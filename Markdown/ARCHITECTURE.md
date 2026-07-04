# ARCHITECTURE.md

# Principles

- ScriptableObjects are the source of truth for configurable design data.
- Runtime state never lives in ScriptableObjects.
- Placement, supply, matching, shop, combat, and UI have separate responsibilities.
- Add colors, blocks, rules, and shop offers without editing existing controllers.
- The Grid tracks Block occupancy only.

---

# Game Flow

`GameManager` owns only Preparation, Combat, and GameOver transitions.

- `BlockSupplyController` starts a fresh grant on Preparation.
- `PlacementController` consumes grant items while placing.
- `WaveManager` owns Combat spawning and completion.
- `ShopEventController` opens configured shop events on eligible Preparation entries.

---

# Data Model

## Block data

- `BlockData`: shared display, HP, dismantle value, sprite, prefab, and filtering tags.
- `BasicBlockData`: adds `BlockColorData` and passive Wall behavior.
- `AttackBlockData`, `HealerBlockData`, `SupportBlockData`: completed skill configuration.
- `CoreBlockData`: Core identity.

`BlockData.DismantleValue` replaces the old placement-cost meaning. It is never checked
or deducted during placement.

## Match data

- `BlockColorData`: designer-defined color identity and tint.
- `BlockMatchData`: list of source color, result skill block, and required count.

Match identity uses the `BlockColorData` asset reference. It does not use a hardcoded color enum.

## Supply data

`BlockSupplyData` contains:

- Minimum and maximum grant count
- Weighted basic-block pool
- Rare completed-block chance and weighted pool
- Initial reroll cost and linear cost increase

## Shop data

- `ShopEventData`: schedule, offer count, and catalog.
- `ShopOfferData`: common display, description, cost, and purchase contract.
- `GrantedBlockShopOfferData`: concrete prototype effect.

New upgrade behavior should be a new `ShopOfferData` subtype.

---

# Runtime Responsibilities

## BlockSupplyController

- Owns only the current Preparation grant.
- Deals, rerolls, consumes, and accepts shop-granted blocks.
- Tracks the per-phase reroll count and whether any block was used.
- Emits supply changes for UI and selection invalidation.

## PlacementController

- Owns mouse/Grid placement and dismantling input.
- Selects one granted item, creates its configured prefab, and consumes it after success.
- Calls `BlockMatchResolver` after basic-block placement.
- Replaces matched blocks without refunds.
- Does not generate grants, calculate random chances, or charge placement points.
- Creates the Core and its four initial basic neighbors through normal Grid placement.

## BlockMatchResolver

- Pure runtime matching service created by PlacementController.
- Performs cardinal BFS from the last placed block.
- Returns exactly the configured required number and result data.
- Never matches completed skill blocks.

## ShopEventController

- Evaluates the data schedule after completed waves.
- Selects unique offers and coordinates payment/application.
- Has no knowledge of concrete upgrade implementation.

---

# UI and Prefabs

- `PreparationUI` pools grant buttons and exposes reroll state.
- `RareBlockAppearance` plays the rare-result highlight.
- `ShopEventUI` pools generic offer buttons.
- `BlockDescriptionTooltip` remains a single reusable tooltip.
- `WorldBlockHoverController` translates mouse position to an occupied Grid cell and
  requests that same tooltip for placed Blocks.
- `PlacementVisualizer` owns independent Placement and Hover requests. A renderable
  Placement request has priority; otherwise a renderable Hover request is shown.
- Tooltip requests are owner-scoped so Supply and World systems cannot hide each other.
- Runtime visual objects are instantiated from prefabs, not assembled by gameplay code.

---

# Existing Systems Preserved

Grid A*, enemies, waves, Block combat behaviors, health bars, damage feedback, time scale,
camera controls, Core death presentation, and Game Over remain independent of the new loop.
