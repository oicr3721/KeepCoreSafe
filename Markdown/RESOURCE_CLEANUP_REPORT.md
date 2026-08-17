# Resource Cleanup Report

## Scope and method

The complete `Assets` tree was audited through Unity's `AssetDatabase` dependency graph. The roots
were all four enabled build scenes and every asset under `Resources` or `StreamingAssets`, which keeps
serialized references and possible string-based runtime loads conservative. Project Settings,
package configuration, source-code references, TMP fallbacks, and scene-template links were checked
separately. No identical asset payload duplicates were found.

Only assets proven unreachable and obsolete were deleted. Unity's `AssetDatabase.DeleteAsset` was
used so each corresponding `.meta` file was removed together with its asset. The removed runtime
resources total 7,349,016 bytes in the repository (about 7.01 MiB), before Unity build compression.

## Deleted animations

- `Assets/Animation/Melee_Idle.anim` — no scene, prefab, controller, or data reference.
- `Assets/Animation/Ranged_Idle.anim` — no scene, prefab, controller, or data reference.

## Deleted audio

- `Assets/Audio/BGM/arctsound-dark-suspense-documentary-205465.mp3` — unused former BGM.
- `Assets/Audio/Clips/ShockWave.wav` — unreferenced effect variant.
- `Assets/Audio/Clips/Shockwave 2.wav` — unreferenced effect variant.
- `Assets/Audio/Clips/ShockwaveCharge.wav` — unreferenced effect variant.
- `Assets/Audio/Clips/bleep001.wav` — unreferenced UI sound.
- `Assets/Audio/Clips/button03a.mp3` — unreferenced UI sound.
- `Assets/Audio/Clips/button06.mp3` — unreferenced UI sound.
- `Assets/Audio/Clips/cancel.wav` — unreferenced UI sound.
- `Assets/Audio/Clips/coin02.mp3` — unreferenced UI sound.
- `Assets/Audio/Clips/shockwave2.wav` — unreferenced effect variant.

## Deleted fonts

- `Assets/Fonts/MonaS8x12 SDF.asset` and `Assets/Fonts/MonaS8x12.ttf` — no serialized reference or
  localization/fallback role.
- `Assets/Fonts/SaH1Outline SDF.asset` and `Assets/Fonts/SaH1Outline.ttf` — no serialized reference or
  localization/fallback role.

## Deleted one-time Editor scripts

The following completed setup/migration tools and temporary validation suites had no runtime role and
were removed together with their `.meta` files:

- `AdditionalCombatFeedbackSetup`, `BlockDestroyEffectFeatureSetup`, `BlockEffectVisualizerSetup`,
  `BlockHealthVisualFeatureSetup`, `CombatFinishFeatureSetup`, `CoreLoopRefactorSetup`,
  `CorePrefabSeparationValidation`, `EnemySpawnIndicatorFeatureSetup`,
  `FeedbackAndSupplyFeatureSetup`, `FixedEnemyPathValidation`, `GameManagerStructureRefactorSetup`,
  `GridGameplayPhysicsRemovalSetup`.
- `InteractiveProloguePlayModeValidation`, `InteractivePrologueSetup`,
  `MainGameCorePlayModeValidation`, `MergePresentationFeatureSetup`,
  `OfferExpansionAndSelectionSetup`, `OffersEventHoverSetup`, `ParticleAndSupplySequenceSetup`,
  `PrefabMigrationSetup`, `PresentationSceneSetup`, `PrototypeSceneFeatureSetup`,
  `RerollFeatureSetup`, `SceneMusicProviderSetup`.
- `ShopGrantAndMergeHealthValidation`, `SuicideEnemyFeatureSetup`, `SupplyEnergySystemSetup`,
  `TutorialFinalePlayModeValidation`, `TutorialLilyPlayModeValidation`, `TutorialPrologueSetup`,
  `TutorialRedGreenReferenceSwapSetup`, `TutorialTaskSetup`, `WaveDataStructureSetup`.
- Temporary cleanup-only dependency audit and pre-build validation scripts were also deleted after
  completing their checks.

Recurring tools retained in `Assets/Editor` are `LocalizationDatabaseSync` and
`LocalizationFontAtlasBuilder`.

No sprite, texture, prefab, material, shader, or VFX asset was deleted: the dependency audit did not
find a candidate in those groups whose removal could be proven safe. In particular, assets below
`Resources` were retained conservatively for possible path-based loads.

## Review required (retained)

- `Assets/DefaultVolumeProfile.asset` — referenced by the URP global settings.
- `Assets/InputSystem_Actions.inputactions` — referenced by Editor Build Settings configuration.
- `Assets/Settings/Lit2DSceneTemplate.scenetemplate` and
  `Assets/Settings/Scenes/URP2DSceneTemplate.unity` — linked Editor scene-template pair; unused by the
  player build, but retained because automated inspection cannot prove the Editor workflow obsolete.
- `Assets/Settings/Renderer2D.asset` — referenced by the active URP asset.
- `Assets/Settings/UniversalRP.asset` — referenced by Quality Settings.
- `Assets/UniversalRenderPipelineGlobalSettings.asset` — referenced by Graphics Settings.

## Localization font atlas builder

Use `Tools > Localization > Build Font Atlases` after editing or adding a JSON file in
`Assets/Resources/i18n` and before a release build. The tool automatically discovers locales, excludes
the `_meta` object through the runtime parser, deduplicates Unicode code points, prepares the existing
Dynamic TMP font assets, saves their atlas data, and logs missing fonts or glyphs.

All locales currently share the UI font set because the runtime does not switch fonts per language:

- `Assets/Fonts/Mona12 SDF.asset`
- `Assets/Fonts/Mona12-Bold SDF.asset`
- `Assets/Fonts/MonaS10x12 SDF.asset` (with `Mona12 SDF` fallback)

After adding the localized playtest-consent disclosure, final scan results were English 69,
Japanese 323, Korean 305, and Simplified Chinese 389 unique code points. `Mona12` contains 991
characters, `Mona12-Bold` 874, and the compact font 151 direct
characters with the remaining CJK text served by its existing fallback. No localized code point is
missing after fallback lookup. A repeated builder run added zero characters after an Editor restart.

## Reference repairs

The audit exposed an older missing sprite GUID in the three Enemy data assets. Their sprite references
were repaired from the sprites already used by the matching live Enemy prefabs:

- `MeleeEnemyData` → melee sprite.
- `RangedEnemyData` → ranged sprite.
- `SuicideEnemyData` → suicide/melee-base sprite.

This was a pre-existing missing reference, not a consequence of deleting resources.

## Validation results

- Unity dependency validation: 4 build scenes, 34 prefabs, and 55 gameplay ScriptableObjects scanned;
  zero missing scripts or gameplay asset references after repair.
- Localization: all four JSON files parsed, every value glyph covered on the first launch through the
  prepared font or fallback, and repeat atlas generation remained idempotent.
- Windows 64-bit StrictMode build: succeeded with 4 scenes, 179,558,188-byte output, in 43.41 seconds;
  no compile, serialization, missing-asset, shader, or font errors.
- Built-player smoke launch: engine, Input System, physics, and Title Scene initialized without an
  exception or missing localization/font error.
- The smoke launch reported that GameAnalytics has no WindowsPlayer game/secret keys. This is an
  external dashboard credential/configuration item and does not prevent the game from starting, but
  it should be configured before collecting release analytics.

The temporary build output was moved to the Windows Recycle Bin after validation and is recoverable.
A before-cleanup build artifact was not available, so no reliable before/after player-size comparison
is reported; the exact removed source-resource size is recorded above instead.
