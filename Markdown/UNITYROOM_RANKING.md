# Unityroom Score Ranking Setup

The project submits `GameManager.WaveIndex` once when Core destruction confirms Game Over in the
normal `GameScene`. It uses the official `naichilab/unityroom-client-library`, Board write mode
`HighScoreDesc`, and no in-game global ranking UI.

## One-time Unityroom setup

1. Open the game's management page on Unityroom and enable the gameplay API.
2. Generate an HMAC authentication key. Treat it as a release credential and do not paste it into a
   Scene, Prefab, ScriptableObject, source file, issue, or commit.
3. Create a score board. Set it to the descending/highest-score rule so a larger reached Wave ranks
   higher. Note its Board No.
4. In Unity, open `Tools > KeepCoreSafe > Unityroom Ranking > Local Build Settings`.
5. Enter the HMAC key and Board No, then select `Save Local Settings`. These values are stored in this
   machine's Unity `EditorPrefs`, outside the repository.

The official client does not require a Game ID field in project code. Requests run from the uploaded
Unityroom game page and use the configured HMAC key and Board No.

## Building and uploading

1. Open `File > Build Profiles`.
2. Activate `Unityroom WebGL - Desktop - Release`. This profile alone adds the `UNITYROOM` scripting
   symbol; the generic WebGL profile remains suitable for non-Unityroom hosting.
3. Build the WebGL player normally. The build is stopped with a clear error if the local HMAC key is
   missing. During this build only, the official API client and ranking configuration are injected
   into the build-scene copy; project Scenes and Prefabs are not modified.
4. Upload the resulting WebGL build through the Unityroom game management page and publish it for the
   intended testers.

For an automated build machine, set both variables instead of using the settings window:

```text
UNITYROOM_HMAC_KEY=<Unityroom HMAC authentication key>
UNITYROOM_BOARD_NO=<positive board number, for example 1>
```

Environment variables take precedence over `EditorPrefs`. Never print the HMAC value in CI logs.

## Server verification

1. Sign in to a Unityroom player account. Only signed-in players can participate in the score ranking.
2. Start the uploaded game, progress through several Waves, and allow the Core to reach real Game Over.
   Returning to Title early must not create a score.
3. Open the score ranking on the Unityroom game page and confirm that the reached Wave appears.
4. Repeat with a lower score and confirm that descending high-score mode preserves the better record;
   then repeat with a higher score and confirm it replaces the previous one.

Network, authentication, or API failure is non-fatal. Game Over and its result UI continue normally;
use the browser developer console and Unityroom configuration to diagnose a missing score.

## Platform behavior

| Environment | Ranking behavior |
|---|---|
| Unity Editor | No Unityroom call; gameplay and Game Over remain normal |
| Windows build | No Unityroom call; GameAnalytics remains independent |
| Generic WebGL profile | No Unityroom call |
| `Unityroom WebGL - Desktop - Release` | Submit reached Wave once at confirmed Game Over |

## Credential limitation

Unityroom's official WebGL client signs requests in the browser, so its HMAC credential is necessarily
present in the generated WebGL player even though it is kept out of Git and project assets. A client
credential cannot be made cryptographically secret from a determined player. Keep the source setting
private, distribute only the built game, and rotate/reissue the key in Unityroom if a build or key is
exposed unexpectedly.
