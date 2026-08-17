# Pre-Logging Refactoring Report

## Scope and outcome

This pass reviewed the runtime Game Flow, Tutorial, Wave, Enemy, spawning, pathfinding, Block,
Grid, merge, Core, Energy, reroll, supply, shop, UI, localization, camera, VFX, audio, and Game Over
systems. Existing gameplay rules and serialized Scene/Prefab values were preserved. No logging,
dependency-injection framework, new global manager, or gameplay feature was introduced.

The project already had useful responsibility boundaries, event-driven Support/Healer targeting,
and pools for its highest-frequency effects. The refactor therefore targeted only costs demonstrated
by the code audit instead of reorganizing every system.

## Structural changes

### Active and passive Blocks

- Problem: the base `Block` owned `Update`, so every Wall, Supply, and Core instance received an
  empty Unity callback every frame.
- Change: `CombatBlock` now owns that tick and only Attack, Healer, and Support inherit it.
- Effect: passive Block behavior and serialization stay unchanged while their empty callbacks disappear.

### Enemy targeting ownership

- Problem: every Attack Block target acquisition performed `FindObjectsByType<Enemy>`, which searches
  the Scene and returns a temporary result array.
- Change: Attack Blocks enumerate WaveManager's existing active-Enemy registry through a read-only
  GameManager API.
- Effect: target acquisition scales with the active wave collection and performs no Scene search.

### Shared particle lifetime

- Problem: Explosion and Heal particle managers duplicated the same pool initialization, playback,
  active-list scan, and return lifecycle.
- Change: both concrete services inherit `PooledParticleEffectManager` while retaining their existing
  singleton names and public `PlayAt` APIs.
- Selection reason: this is a real is-a relationship with identical state/lifetime behavior. A new
  coordinating manager or interface would add indirection without removing responsibility.

### Future telemetry access

- Problem: future logging would otherwise need to traverse private manager fields and mutable objects.
- Change: `CaptureGameplayState` and `CaptureSupplyState` return immutable value snapshots.
- Effect: wave identity/type, planned and active Enemy counts, Energy, Core HP, reroll count/cost,
  grant count, rare chance, and transition flags can be read without mutation or collection copies.

## Performance changes

### High impact: Attack target search

- Before: one Scene-wide Enemy search and result-array allocation per target reacquisition.
- After: zero Scene-wide Enemy searches; direct enumeration of the existing registry.
- Measured/static count: hot `FindObjectsByType<Enemy>` call sites changed from 1 to 0.

### High impact during wave preparation: pathfinding allocations

- Before: one heap `SearchRecord` object per discovered state, plus a new List and HashSet for every
  processed distance layer.
- After: SearchRecord is stored directly in the Dictionary; one next-layer List and one HashSet are
  allocated per route and cleared/reused across layers.
- Exact structural reduction: layer temporary collections change from `2 × processed layer count`
  to 2 per route; per-state SearchRecord object allocations change from one to zero.
- Route outputs still allocate their immutable path List, as required by spawned Enemies.

### Medium impact: passive Block and Health Bar updates

- Before: all Block instances and all Block Health Bars received a Unity Update callback every frame.
- After: only active skill Blocks tick; a Health Bar enables its own timer only while recently damaged,
  then disables the component tick again without allocating a tween or coroutine object.
- Exact runtime percentage depends on the player's current Grid composition and was not guessed.

### Medium impact: Support cooldown lookup

- Before: each Attack/Heal action scanned all Blocks until it found an applicable Support.
- After: the first request for a Cell after a Grid change performs that same ordered scan; later requests
  are O(1) dictionary lookups until occupancy changes.

### Low impact: Enemy spawn delay

- Before: one `WaitForSeconds` object was created per spawned Enemy.
- After: one instance is reused for the wave's sequential spawn coroutine.

## Validation and measurements

Automated validation completed successfully for:

- Unity script compilation.
- Fixed Enemy route selection and path-length tolerance.
- GameScene reroll, Combat start, Enemy spawn, and Enemy movement over an eight-second PlayMode run.
- Gameplay/Supply snapshot correctness and pooled particle-service initialization.
- Shop guaranteed grants and merged HP inheritance.
- Core damage and Shockwave visual/hierarchy preservation.
- Tutorial Lily flow.
- Interactive Prologue and Tutorial-Core to In-Game-Core replacement.

The headless PlayMode run sampled 46,030 uncapped frames. It reported 41 bytes average and 3,474 bytes
maximum `GC Allocated In Frame`, but this includes Editor/batch-mode behavior and is not a before/after
player-build comparison. The value is recorded only as a regression-run observation, not as evidence of
a player-visible FPS improvement. `GC.GetAllocatedBytesForCurrentThread` returned zero for the isolated
path benchmark despite required result objects, so that reading was rejected as unsupported/unreliable.

## Unmeasured items

- No representative player-build CPU/GPU profile was available, so no frame-rate improvement percentage
  is claimed.
- Long-session memory growth, GPU/VFX cost, and device-specific input latency require a Development Build
  and Unity Profiler session on target hardware.
- Supply appearance is chance/interval driven; its core controller and offer behavior were covered by
  existing deterministic validations rather than waiting for a random event during the short load run.

## Behavior changes

There are no intentional gameplay-rule or player-flow changes. Route selection, target randomness,
Support selection order, cooldown values, health-bar duration, wave timing, damage, Energy, reroll,
Supply, Tutorial, Prologue, and Game Over semantics remain unchanged. Inspector references and values
were not rewritten; inherited particle fields retain their original serialized names.
