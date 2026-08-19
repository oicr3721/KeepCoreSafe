# Playtest Analytics Design and Integration

## Existing integration seams

- `TutorialDirector` owns the tutorial sequence and the four intentional UX exception paths.
- `PrologueDirector` owns prologue entry, successful fusion, and the transition to `GameScene`.
- `GameManager` owns real-game start, wave start, wave terminal results, and Core-destruction Game Over.
- `BlockSupplyController.TryReroll`, `ShopEventController.TrySelectOffer`, and
  `PlacementController.ResolveMatch` are the successful commit points for their respective actions.
- Existing immutable `GameManager.GameplayState` and Grid enumeration provide terminal-state data
  without introducing parallel gameplay state.

Analytics calls are attached only to those successful state transitions. UI, animation, hover, and
per-frame paths are not event sources.

## Architecture

```text
Tutorial / Prologue / Game / Supply / Offer / Merge
                         |
                  AnalyticsService
                  /              \
       central schema       IAnalyticsBackend
                                  |
                         GameAnalyticsBackend
                                  |
                        GameAnalytics SDK 8.0.1
```

- Only `GameAnalyticsBackend` imports `GameAnalyticsSDK`.
- `AnalyticsSchema` owns all event, progression, field, tutorial-step, and exception identifiers.
- `AnalyticsService` owns run/wave lifecycle deduplication and low-cost aggregation.
- SDK errors are caught at the boundary and never alter gameplay results or timing.
- The SDK's automatic random desktop User ID and session handling are retained. No second player
  identifier or manual session lifecycle is created, and advertising-ID tracking is explicitly
  disabled before initialization.
- `AnalyticsConsentSettings` is the persisted source of truth for optional collection. The SDK is
  not initialized before opt-in. The one-time prompt appears only after the player first selects Start
  or Tutorial, allowing Title-screen language selection beforehand; either answer continues to the
  originally selected scene. Declining or withdrawing consent prevents future event submission without
  restricting gameplay, and the Title Settings entry uses the same persisted state.
- Automatic GameAnalytics error, hardware, health, memory, and FPS reporting are disabled for the
  private playtest. Only the deliberately selected events below are submitted.
- GameAnalytics credentials are intentionally not stored in source code. Configure each target
  platform in Unity's GameAnalytics setup window before collecting a build's events.
- `AnalyticsPlaytestBuildProcessor` blocks a Windows build when its Game Key, Secret Key, Build, or
  consent prefab is missing. A plain-text privacy notice is copied beside the built executable.

## Selected events

| Semantic event | GameAnalytics form | Source / moment | Key data |
| --- | --- | --- | --- |
| TutorialStarted | Design `funnel:tutorial:started` | Tutorial Director starts | SDK session/user |
| TutorialStepCompleted | Progression Complete | Intro, Attack merge, Healer lesson, defense result | stable step ID |
| TutorialExceptionOccurred | Design `tutorial:exception` | Wrong first placement/color, Lily Cell, invalid dismantle | step ID, exception type |
| TutorialCompleted | Design `funnel:tutorial:completed` | Immediately before glitch transition | SDK session/user |
| PrologueStarted / Completed | Design funnel events | Prologue start / successful fusion before scene load | SDK session/user |
| GameStarted | Design `funnel:game:started` | `GameScene` GameManager start only | SDK session/user |
| WaveStarted | Progression Start | Prepared wave successfully starts | wave number, asset ID, normal/special, composition/energy summary |
| WaveCompleted | Progression Complete | GameManager accepts a terminal clear result | clear type, wave state, rerolls, terminal board aggregate |
| GameOver | Design + Progression Fail | Core destruction state transition | wave/type, active/planned enemies, required/current energy, rerolls, Core ratio, board aggregate |
| GameAbandoned | Design `session:game:abandoned` | Confirmed pause-menu return to Title during an active run | wave, phase, current low-cost state |
| GracefulExit | Design `session:game:graceful_exit` | `Application.quitting` during an active non-Game-Over run | wave, phase, current low-cost state |
| RerollUsed | Design | Cost paid and reroll committed | next wave, count, paid cost |
| OfferSelected | Design | Valid Offer selection committed | current wave, stable Offer ID |
| MergePerformed | Design | Result Block successfully placed | next wave, result Block ID, source count |

Offer and Block data expose an optional serialized analytics ID. Existing assets remain untouched and
fall back to their stable ScriptableObject asset name until explicit IDs are assigned.

## Board snapshot decision

Full Cell-by-Cell Block/HP snapshots are deliberately omitted. They can exceed practical custom-field
string limits, create high-cardinality payloads, and GameAnalytics custom fields are mainly useful in
raw export rather than standard dashboards. At `WaveCompleted` and `GameOver` only, the integration
records total, basic, and skill Block counts. If spatial research becomes necessary, it should use a
separate versioned snapshot/export pipeline rather than inflating every progression event.

## Deliberately omitted data

- Mouse movement, hover, UI animation, and generic button clicks.
- Enemy movement, individual Enemy damage, and every Block HP/Energy mutation.
- Every placement/dismantle action and intermediate merge operations.
- Full board coordinates/HP and arbitrary localized display strings.
- A custom player ID or manual session start/end events.

These omissions keep the dataset tied to funnel, failure, and meaningful system-use questions.
Forced process termination cannot reliably execute `Application.quitting`; those cases remain
inferable from GameAnalytics' automatic session records rather than being mislabeled as Game Over.

## Private executable distribution

A locally launched Windows executable sends events exactly like an itch.io download when the player
has opted in and the computer can reach GameAnalytics. Offline events may remain queued by the SDK
and be submitted later. The build's consent disclosure lists the random installation/session IDs,
device/session metadata, IP-derived country, and selected gameplay events. It explicitly states that
sharing is optional, explains how to withdraw through Title Settings, and directs the tester to the
included `PLAYTEST_PRIVACY_NOTICE.txt` for the complete notice.

For a future public store release, publish a developer-owned privacy notice at a stable web URL and
link it from the store page and game. The bundled notice is intended for direct, private distribution
where the developer already has a contact channel with every tester.

## Validation

`AnalyticsServiceTests` injects a recording backend and verifies lifecycle/step deduplication, one
terminal event per wave, one Game Over per run, progression status, reroll propagation, and terminal
board-summary presence without making network requests.

## WebGL SDK 8.0.1 compatibility

The GameAnalytics 8.0.1 WebGL bridge calls its JavaScript runtime with incorrect argument positions
for Design events without a numeric value and Progression events without a score. Those overloads
silently lose all custom fields. Its JavaScript validation also treats numeric zero and `false` as
null, omits them, and emits misleading warning/error events.

`GameAnalyticsBackend` contains a WebGL-player-only compatibility path. Events with custom fields use
the correctly aligned numeric overloads with a neutral zero when no semantic value/score exists, and
zero/false custom-field values are serialized as `"0"`/`"false"`. Other platforms retain their native
numeric and boolean types. Downstream analysis should therefore cast these two WebGL string forms back
to the intended type.

## Raw export decision (2026-08-19)

- GameAnalytics raw Data Export is a PipelineIQ add-on; advertised pricing starts at USD 499/month.
  It is not appropriate for a private playtest of roughly 50 players.
- Unity Analytics collection is comfortably inside its free tier at this scale (50,000 MAU and 500
  custom events per MAU per month). Dashboard reports can be exported as CSV or PNG.
- Unity raw event access is Data Access through a separately owned Snowflake account. Because that
  introduces an external billable warehouse and cannot be guaranteed cost-free, the analytics backend
  was not migrated under the project's zero-cost requirement.
- Live Events JSON remains useful for integration debugging, not durable analysis: the inspected export
  contained exactly 50 recent events and omitted the beginning of a long session. It must not be treated
  as a complete raw dataset.
