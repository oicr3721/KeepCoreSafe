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
- At wave start, `WaveManager` also freezes every spawn position and its precomputed route
  before the delayed spawn coroutine begins.
- `ShopEventController` owns Supply-event placement, survival resolution, and free offers.
- On wave completion, `GameManager` asks `ShopEventController` to start an eligible Supply
  spawn sequence before changing phase. `SupplySpawnPresentationController` owns the reveal
  tween and recognition hold; its completion callback is the only path that resumes the
  transition to Preparation. A wave with no new Supply continues immediately.
- `CoreEnergyController` owns per-wave energy, automatic charge, world pickups, and threshold dispatch.
- Before Preparation listeners present a new Supply grant, `GameManager` rolls the next base
  Energy requirement and asks `CoreEnergyController` to reset current Energy to zero.

The scene hierarchy exposes these responsibilities without adding new coordinating managers:

```text
GameManager
├── Game Systems
│   ├── Wave System
│   ├── Supply System
│   └── Core Energy System
├── World Interaction
└── Presentation
    ├── Stage Clear Presentation
    └── Wave Start Presentation
```

`GameManager` keeps explicit Inspector references to the child systems. Splitting the existing
components adds only a few Transforms; it does not add MonoBehaviours or Update calls.

---

# Data Model

## Block data

- `BlockData`: shared display, HP, base sprite, ordered health-stage
  sprites, optional `BlockColorData`, prefab, and filtering tags.
- `BasicBlockData`: uses the shared color identity for matching and adds passive Wall behavior.
- `AttackBlockData`, `HealerBlockData`, `SupportBlockData`: completed skill configuration.
- `CoreBlockData`: Core identity and the sole reference to that Core variant's authored prefab.

`BlockData.GetHealthSprite` resolves the configured highest-to-lowest health sprite array
using equal ratio bands and falls back to the base sprite for an empty or missing entry.
`CoreBlock` deliberately overrides the sprite-refresh hook: HP changes still update its shared
health bar, but never replace the SpriteRenderer authored in the selected Core prefab. Tutorial and
In-Game CoreData therefore keep empty health-stage arrays and reference distinct Core prefabs.

## Match data

- `BlockColorData`: designer-defined color identity and tint.
- `BlockMatchData`: list of source color, result skill block, and required count.

Match identity uses the `BlockColorData` asset reference. It does not use a hardcoded color enum.

## Supply data

`BlockSupplyData` contains:

- Minimum and maximum grant count
- Weighted basic-block pool
- Rare completed-block chance and weighted pool

## Shop data

- `ShopEventData`: Supply appearance interval/chance, hunter ratio, offer count, and catalog.
- `ShopOfferData`: common display, description, eligibility, and free application contract.
- `GrantedBlockShopOfferData`: concrete prototype effect that reserves a guaranteed slot
  instead of appending an extra granted item.

New upgrade behavior should be a new `ShopOfferData` subtype.

## Wave data

- `EnemyData` continues to own one Enemy type's stats, behavior prefab, targeting, and audio.
- `WaveData` owns only a designer-facing name, optional intent/strategy notes, and a weighted
  list of arbitrary existing `EnemyData` assets. It has no Min/Max Wave condition.
- `WaveDifficultyData` owns the unchanged progression curve, enemy-count range, required
  Energy, spawn pressure, Normal/Special `WaveData` pools, and `SpecialWaveInterval`.
- `WaveDifficultySnapshot` freezes the rolled scale values together with the selected
  `WaveData` and Special flag for the upcoming wave.

---

# Runtime Responsibilities

## BlockSupplyController

- Owns only the current Preparation grant.
- Deals, rerolls, consumes, and accepts shop-granted blocks.
- Owns the per-Preparation reroll count and next reroll cost, asks `BlockSupplyData` for the
  reroll-adjusted rare chance, and commits each allowed cost through `GameManager` to the
  authoritative current Energy state.
- Rejects a reroll whose next cost would exceed the negative range defined by base required Energy.
- Reserves purchased blocks as guaranteed slots in the next grant and fills only the
  remaining slots through the existing weighted random rolls.
- Tracks whether any block was used so reroll can be locked for the phase.
- Emits supply changes for UI and selection invalidation.

## PlacementController

- Owns mouse/Grid placement and dismantling input.
- Selects one granted item, creates its configured prefab, and consumes it after success.
- Calls `BlockMatchResolver` after basic-block placement.
- Replaces matched blocks without treating consumption as player dismantling.
- Does not generate grants or calculate random chances.
- Creates the Core and its four initial basic neighbors through normal Grid placement.
- Captures merge source visuals, commits the existing Grid replacement immediately, and
  delegates only the visual sequence to `MergePresentationController`.
- Computes the consumed blocks' average HP ratio before removal and applies it to the
  matched result through the shared Block HP/health-visual path.

## BlockMatchResolver

- Pure runtime matching service created by PlacementController.
- Performs cardinal BFS from the last placed block.
- Returns exactly the configured required number and result data.
- Never matches completed skill blocks.

## MergePresentationController

- Owns concurrent, unscaled-time merge presentation sequences.
- Uses visual proxies for consumed source blocks so gameplay objects can still be removed
  immediately by the existing merge flow.
- Masks and reveals the already-created result Block without owning its gameplay state.
- Reuses `CoreEnergyPulseView`, `ShockwaveRingView`, DOTween, and the existing camera impact
  shake, and releases every per-merge interaction lock on completion, cancellation, or destroy.

## Grid interaction locks

- `GridCell` stores a reference-counted interaction lock independent of occupancy.
- `GridManager.AcquireInteractionLock` returns an idempotent per-merge handle that releases
  only the cells acquired by that merge.
- Placement, dismantle previews/input, and world hover reject locked cells; unrelated cells
  remain interactive.

## Block health visuals

- `Block.UpdateHealthVisual` is the common API for every Block type.
- The base `Block` listens to its HP value and updates both the world sprite and its
  `BlockHealthBar`; subclasses do not duplicate health presentation logic.
- `BlockHealthBar` owns Inspector-configurable healthy, warning, and critical colors and
  applies them as discrete thresholds rather than a gradient.

## Block destruction presentation

- `Block.Die` requests presentation only after damage has reduced a Block to zero HP.
- `BlockDestroyEffectManager` owns a scene-authored pool and reuses inactive instances first.
  If every instance is active, it instantiates the serialized effect prefab once, registers it
  in the same managed list, and reuses that expanded capacity for later requests.
- `BlockDestroyEffect` animates only its serialized child renderers, selects a random
  `BlockPiece-Sheet` sprite, and uses `BlockData.DestroyEffectColor` as the initial tint.
- Dismantling and match consumption bypass `Block.Die`, so they do not play this effect.

## Event-driven area targets

- `GridManager.GridChanged` is the existing occupancy-change signal for placement, removal,
  destruction, and match replacement.
- `SupportBlock` rebuilds linked targets only from that signal or a Combat phase transition.
- `HealerBlock` rebuilds adjacent subscriptions from the same signal and caches its heal
  target; adjacent HP events update the cached priority without a per-frame Grid scan.

## Fixed enemy navigation

- `GridPathfinder` still uses the existing Grid and cardinal movement, but treats occupied
  non-Core cells as traversable route candidates with a destruction count.
- It first computes the occupancy-independent shortest distance, evaluates states only up to
  `shortest + EnemyData.PathLengthTolerance`, then minimizes block count, distance, and finally
  resolves exact ties with the enemy navigation seed.
- `WaveManager.CreateSpawnPlan` calculates every route against one wave-start Grid state and
  passes the immutable cell list through `Enemy.Initialize` when each delayed spawn occurs.
- `WaveManager` receives generic `EnemyData` entries from the selected `WaveData`; adding a new
  Enemy type to a composition does not require another archetype branch in the spawn pipeline.
- Melee and Ranged enemies never subscribe to `GridChanged`. They subscribe only to their
  current route block's `Died` event, then continue along the stored route.
- Enemies spawn outside the Grid. `RangedEnemy` explicitly enters through the first stored,
  in-bounds route cell before applying its normal range/retreat decisions.
- Enemy movement is deterministic Transform movement toward `GridToWorld(cell) + PersonalCellOffset`.
  `TryGetBlock` and `IsCellEmpty` are the only Block-obstruction checks; neither Enemy nor Block
  Prefabs contain `Rigidbody2D`/`Collider2D`, and no Physics2D contact participates in gameplay.
- Enemy-to-Enemy collision and separation are intentionally absent. The per-Enemy visual cell
  offset keeps units readable without changing their logical Grid cell or route.
- `SuicideEnemy` consumes the same immutable route and owns only its blocker handling,
  route-blocker/low-HP triggers, synchronized warning state, and 3x3 Block-only explosion.
  It begins self-destruction at the same adjacent-Block condition where a Melee Enemy would
  begin attacking, rather than waiting for the route's final Cell.
- `Enemy.OnDamaged` is a no-op extension hook used by Suicide Enemy after nonlethal damage;
  `Enemy.Die(bool)` keeps the common death event while allowing self-detonation to suppress
  only the Energy award.

## Explosion particle pooling

- `ExplosionParticleEffectManager` is a scene-authored presentation service backed by the
  existing `ComponentPool<ParticleSystem>`.
- One effect is rented for every Block actually hit by a Suicide Enemy explosion and returned
  when its Particle System is no longer alive. Concurrent explosions expand the pool normally.
- Gameplay selects Block targets and applies damage; the manager owns only particle lifetime.

## Next-wave spawn indicators

- `GameManager` rolls and retains the next `WaveDifficultySnapshot` when Preparation begins.
- `WaveDifficultyController` retains only the previously selected `WaveData` at runtime and
  excludes it from the next selection when the active pool has another valid asset. Runtime
  selection history never mutates the ScriptableObject.
- Every positive `SpecialWaveInterval` multiple selects only from the Special pool; other waves
  select only from the Normal pool. An interval of zero disables Special waves for Tutorial.
- `WaveManager.PrepareWave` fixes enemy types and world spawn positions once. Placement and
  camera changes do not regenerate them.
- `WaveManager.StartWave` builds routes against the final Grid while preserving those prepared
  positions, so indicators and instantiated enemies consume the same spawn data.
- `EnemySpawnIndicatorView` is a prefab-authored world-space ring. A reusable `ComponentPool`
  supplies one view per unique position; DOTween runs the one-time reveal and subtle loop.
- `PlacementController` rejects cells reserved by the prepared spawn data.

## Damage feedback

- `DamageFeedback` remains the shared flash, shake, and scale-punch implementation.
- Its optional serialized `ParticleSystem` is restarted on each valid hit, allowing Prefabs
  to add particle feedback without changing the target's damage logic.
- Enemy Prefabs include a shared nested `Enemy Hit Particles` prefab. It is pre-created with
  the Enemy and does not allocate a particle GameObject per hit.
- Every Block Prefab similarly owns one nested `Dust Particle System`. The expected Grid size
  and one-system-per-block cost do not justify a central pool, and simultaneous hits cannot
  contend for a limited shared pool.
- `HealParticleEffectManager` owns a scene-authored `ComponentPool<ParticleSystem>` used by both
  `HealerBlock` and color-recovery Shop offers. Healers no longer own one particle instance each.

## Merge audio

- `MergePresentationController` owns the serialized special-block merge `AudioClip`.
- The controller requests playback through `AudioManager` in the same sequence callback as
  `PlayBurst`, guarded by Attack, Support, or Healer result properties.
- `AudioManager.Play(AudioClip)` uses the existing SFX AudioSource pool and volume setting.

## ShopEventController

- Evaluates the Supply-event data during the post-wave transition and places only after finding
  a valid cell.
- Resolves survival, selects unique free offers, and coordinates application.
- Exposes the active Supply target and additional-hunter count to `WaveManager`.
- Has no knowledge of concrete upgrade implementation.
- Records the selected offer without applying it during the covered card presentation. On close,
  it raises `ShopClosing` so the UI hides, applies the selected offer, and only then raises
  `ShopClosed` so guaranteed blocks are queued before `BlockSupplyController` deals the grant.

## SupplySpawnPresentationController

- Reuses the merge presentation's Pulse, Shockwave, and Burst particle prefabs.
- Owns the unscaled reveal/hold sequence and restores the Supply Block visual state on release.
- Does not own phase state; it reports completion to `GameManager` through the callback supplied
  by `ShopEventController`.

## CoreEnergyController

- Owns a signed `ObservableInt` whose maximum is base required Energy and whose minimum is its
  negative counterpart.
- Resets Energy to zero on Preparation, applies reroll costs as negative Energy, and preserves
  that debt when Combat begins.
- Accumulates inspector-configured automatic charge only during Combat.
- Pools world pickups and Core absorption pulses; energy is applied when a pickup reaches Core.
- Requests the existing Stage Clear presentation exactly once when the threshold is reached.
- Exposes an explicit zero reset used at the exact Shockwave release frame. Normal Preparation
  initialization still resets current Energy and refreshes the next base maximum.

## StageClearPresentationController

- Owns one scene-authored `CoreEnergyPulseView` and one `ShockwaveRingView` child.
- Activates, replays, and deactivates those views for each clear instead of instantiating and
  destroying identical single-instance effects every wave.
- Resets Core Energy immediately before calling `ShockwaveRingView.Play`, so the Energy UI
  drops to zero at release rather than after the presentation.
- Multiple simultaneous Stage Clear presentations are rejected, so a pool provides no benefit.
- Nearby repeated effects keep their appropriate existing lifetimes: Wave Start and Energy pickup
  effects already use `ComponentPool`, while merge effects may overlap and remain per-sequence.

---

# UI and Prefabs

- `PreparationUI` pools grant buttons, displays the next reroll cost, and disables the Button's
  interactability when reroll is unavailable without deactivating its GameObject. It does not
  own an Energy readout.
- `ShockwaveCountdownUI` remains visible in Preparation and Combat and projects one signed
  Energy source into current/required text, a normal positive gauge, and a red minus gauge.
- `AudioVolumeSettingsUI` and `AudioManager` share one normalized `0..1` volume-multiplier range.
  Saved BGM and SFX values are clamped to that range, with `1` as the default and maximum.
- `SupplyPresentationUI` owns a scene-authored `Buttons` container with one `CanvasGroup` for
  Confirm/Reroll fade-in and fade-out. Confirm deactivates the container after fading, so later
  child-level reroll availability refreshes cannot make the controls visible while docked.
- Confirm converts the `Dock Target` rectangle into the `Content Root` parent's coordinate space,
  animates both position and size, then copies the target anchors, pivot, position, and size exactly.
  The original expanded layout is cached and restored before the next Supply presentation.
- `RareBlockAppearance` plays the rare-result highlight.
- `ShopEventUI` pools generic offer buttons.
- `HoverCanvasGroupFade` is attached directly to `ShopEventUI`'s existing `Visual` object. Its
  existing Image remains the raycast target, while DOTween transitions only CanvasGroup alpha
  between 1 and the serialized near-transparent hover value without changing interaction flags.
- `BlockDescriptionTooltip` remains a single reusable tooltip.
- `WorldBlockHoverController` translates mouse position to an occupied Grid cell and requests
  the shared tooltip for placed Blocks.
- `PlacementVisualizer` owns independent Placement and Hover requests. A renderable
  Placement request has priority; otherwise a renderable Hover request is shown.
- `PlacementController` preserves the selected BlockData RGB for both valid and invalid previews.
  Invalid Cells use an unscaled-time alpha pulse with serialized minimum alpha and cycle duration;
  hiding the preview or returning to a valid Cell resets that pulse immediately.
- Tooltip requests are owner-scoped so Supply and World systems cannot hide each other.
- Reusable visual structure is scene/prefab-authored; runtime code supplies state and animation.

## Tutorial UI and accessibility entry point

- `TutorialScene` synchronizes common UI RectTransform, typography, layout, and graphic settings
  from the current `GameScene`. Chromatic UI colors retain the source saturation, value, and alpha
  while using the Tutorial green hue; semantic red feedback remains red.
- Self-contained Offer, Energy, Wave Announcement, and Stage Clear UI roots reuse the current
  GameScene structure. Tutorial-only Dialogue, Glitch Transition, and Grid Highlight are preserved,
  and common UI that the Tutorial does not use remains inactive rather than being deleted.
- `PlacementController.GrantedBlockSelectionRequested` is a general pre-selection validation hook.
  `TutorialDirector` uses it only during the first Red lesson to reject Green without mutating selection.
- `TutorialDirector.redBlock` and `greenBlock` remain explicit color-identity references. The scene's
  scripted supply/starting arrays and the Director's stage usage determine the swapped Tutorial order;
  neither source `BasicBlockData` asset is modified.
- `TutorialColorblindPrompt` is a prefab-authored localized modal. `AccessibilitySettings` persists
  Colorblind Mode unlock/enabled flags and exposes change events as the integration seam for a
  future rendering implementation; it currently owns no visual correction behavior.
- `TutorialDirector` drives Lily's existing `Idle` and `Happy` Animator triggers. Successful
  Attack/Healer creation and the completed defense produce a short Happy reaction before returning
  to Idle; the existing glitch and blackout still own the transition into PrologueScene.
- The final dialogue also starts a non-blocking finale presentation. `TutorialDirector` injects
  Lily's Grid Cell into the existing `SuicideEnemyData.Prefab`, requests an exact-cell route from
  `GridPathfinder`, enables that Enemy's normal simulation outside Combat, and asks the shared
  camera controller to focus on Lily. Warning, audio, explosion VFX, and Block damage remain owned
  by `SuicideEnemy`; the Director does not duplicate them or wait for their completion.
- `CoreBlock.SetDestructionProtected` is enabled only for that finale and clamps otherwise-lethal
  damage to one remaining HP. Normal Tutorial combat and every main-game wave keep the existing
  Core destruction and Game Over path unchanged.
- `TutorialDirector.lilyOffsetFromCore` positions its explicit Lily Transform reference through
  `GridManager.GridToWorld`. `PlacementController.BlockPlacementValidationRequested` is the shared,
  side-effect-free extension point used by both placement preview and commit; Tutorial rejects the
  Lily cell there and handles the commit-only rejection event with localized dialogue and `Happy`.

## Interactive Prologue

- `PrologueDirector` owns the one-scene state machine: recognition delay, direct left-click Lily
  selection, Grid placement preview, Core destination detection, input lock, fusion sequence,
  Tutorial-Core prefab to In-Game-Core prefab replacement, and the normal black GameScene transition.
- The inactive `Prologue Core` object is only a visual-free spawn anchor. At runtime the Director
  instantiates `tutorialCoreData.Prefab` through normal Block initialization without a gameplay HP bar.
  Fusion instantiates `inGameCoreData.Prefab`, transfers the current HP ratio, then destroys the old
  Tutorial Core object. Prefab-authored children such as the In-Game Lily Animator remain intact.
- It reuses `GridManager` coordinates, `TutorialGridHighlight`, `GameCameraController`,
  `CoreEnergyPulseView`, `ShockwaveRingView`, and the Merge Burst particle prefab. Lily is a visual
  interaction object rather than a combat `Block`, preventing Prologue-only behavior from entering
  normal placement, matching, health, or wave rules.
- Clicking a placed Lily immediately lifts it into placement state without an inventory-button
  intermediate step. Pickup and placement use separate Inspector-assigned AudioClips.
- `PrologueDirector.cameraOffset` is applied to the Core position through
  `GameCameraController.SetDefaultViewCenter`, keeping the Core centered by default while allowing
  scene-specific framing adjustments in the Inspector.
- `PrologueThreatOverlay` manages a fixed scene-authored pool of TMP labels. Each label independently
  chooses from raw mixed-language/code/binary commands, a random full-screen position, a 12-multiple
  font size, jitter, duration, and flicker; the fusion sequence rejects and clears all labels together
  without creating UI objects.
- `InteractivePrologueSetup` performs an idempotent scene migration: it preserves the existing scene,
  disables only the obsolete paragraph visuals, keeps the Core anchor visual-free, wires explicit
  Inspector references, and validates both distinct Core prefabs plus the Lily connections.
- Prologue uses the same unmodified black `SceneTransition.Load` flow as every other scene. The old
  Lily inventory button and Prologue White Flash scene objects remain inactive for safe UI preservation.

---

# Existing Systems Preserved

Grid A*, enemies, waves, Block combat behaviors, health bars, damage feedback, time scale,
camera controls, Core death presentation, and Game Over remain independent of the new loop.
