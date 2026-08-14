using System.Collections;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Localization;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Tutorial
{
    [DefaultExecutionOrder(100)]
    public sealed class TutorialDirector : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private PlacementController placementController;
        [SerializeField] private BlockSupplyController supplyController;
        [SerializeField] private PreparationUI preparationUI;
        [SerializeField] private WaveManager waveManager;

        [Header("Dialogue")]
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private TypewriterText typewriter;
        [SerializeField] private TutorialGridHighlight gridHighlight;
        [SerializeField] private TutorialGlitchTransition glitchTransition;

        [Header("Tutorial Data")]
        [SerializeField] private BasicBlockData redBlock;
        [SerializeField] private BasicBlockData greenBlock;

        private static bool resumeFromBuildStep;
        private bool advanceRequested;
        private bool healerCreated;
        private bool attackCreated;
        private bool wrongGreenPlacement;
        private Block wrongGreenBlock;
        private bool wrongGreenBlockDismantled;
        private int redBlocksPlaced;
        private ClearType clearType;
        private bool gameOver;
        private Vector2Int greenTarget;
        private Coroutine invalidDismantleWarningRoutine;

        private void Start()
        {
            typewriter.AdvanceRequested += HandleAdvance;
            placementController.BlockPlaced += HandleBlockPlaced;
            placementController.BlockDismantleRequested += CanDismantleBlock;
            placementController.BlockDismantled += HandleBlockDismantled;
            placementController.SkillBlockCreated += HandleSkillCreated;
            GameManager.StageCleared += HandleStageCleared;
            GameManager.PhaseChanged += HandlePhaseChanged;
            preparationUI.SetStartWaveAllowed(false);
            dialogueRoot.SetActive(false);
            gridHighlight.Hide();
            StartCoroutine(RunTutorial());
        }

        private IEnumerator RunTutorial()
        {
            bool resume = resumeFromBuildStep;
            resumeFromBuildStep = false;
            if (!resume)
            {
                yield return SayKey("tutorial.intro.1");
                yield return SayKey("tutorial.intro.2");
                yield return SayKey("tutorial.intro.3");
            }

            yield return new WaitUntil(() => placementController.PlacementInputEnabled);
            Vector2Int core = GridManager.Instance.Grid.Core.GridPosition;
            greenTarget = core + Vector2Int.right + Vector2Int.down;
            yield return SayKey("tutorial.green.connect");
            gridHighlight.Show(greenTarget);

            while (!healerCreated)
            {
                if (wrongGreenPlacement)
                {
                    wrongGreenPlacement = false;
                    gridHighlight.Hide();
                    yield return SayKey("tutorial.green.wrong");
                    yield return new WaitUntil(() => wrongGreenBlockDismantled || wrongGreenBlock == null);
                    wrongGreenBlock = null;
                    wrongGreenBlockDismantled = false;
                    supplyController.AddGrantedBlock(greenBlock, false);
                    gridHighlight.Show(greenTarget);
                }
                yield return null;
            }

            gridHighlight.Hide();
            yield return SayKey("tutorial.green.success");
            yield return SayKey("tutorial.red.connect");
            while (redBlocksPlaced < 2)
            {
                Vector2Int target = core + Vector2Int.up + (redBlocksPlaced == 0 ? Vector2Int.left : Vector2Int.right);
                gridHighlight.Show(target);
                yield return null;
            }
            gridHighlight.Hide();

            if (attackCreated)
                yield return SayKey("tutorial.attack.success");
            else
                yield return SayKey("tutorial.attack.fail");

            preparationUI.SetStartWaveAllowed(true);
            yield return new WaitUntil(() => GameManager.Phase == GamePhase.Combat || gameOver);
            yield return new WaitUntil(() => GameManager.Phase != GamePhase.Combat || gameOver);

            if (gameOver)
            {
                yield return SayKey("tutorial.gameover");
                resumeFromBuildStep = true;
                SceneManager.LoadScene("TutorialScene");
                yield break;
            }

            if (clearType == ClearType.ShockWave)
                yield return SayKey("tutorial.clear.shockwave");
            else
                yield return SayKey("tutorial.clear.killall");

            dialogueRoot.SetActive(true);
            typewriter.Play(LocalizationManager.Get("tutorial.glitch.last"));
            yield return new WaitForSecondsRealtime(0.65f);
            glitchTransition.Play();
        }

        private IEnumerator SayKey(string key)
        {
            return Say(LocalizationManager.Get(key));
        }

        private IEnumerator Say(string line)
        {
            dialogueRoot.SetActive(true);
            advanceRequested = false;
            typewriter.Play(line);
            yield return new WaitUntil(() => !typewriter.IsTyping);
            yield return new WaitUntil(() => advanceRequested);
            dialogueRoot.SetActive(false);
        }

        private void HandleBlockPlaced(Block block, Vector2Int position)
        {
            if (block.Data == greenBlock && !healerCreated && position != greenTarget)
            {
                wrongGreenPlacement = true;
                wrongGreenBlock = block;
                wrongGreenBlockDismantled = false;
            }
            if (block.Data == redBlock)
                redBlocksPlaced++;
        }

        private void HandleBlockDismantled(Block block, Vector2Int _)
        {
            if (block == wrongGreenBlock)
                wrongGreenBlockDismantled = true;
        }

        private bool CanDismantleBlock(Block block, Vector2Int _)
        {
            if (block != null && block == wrongGreenBlock)
                return true;

            ShowInvalidDismantleWarning();
            return false;
        }

        private void ShowInvalidDismantleWarning()
        {
            if (invalidDismantleWarningRoutine != null
                || dialogueRoot.activeSelf)
            {
                return;
            }

            invalidDismantleWarningRoutine =
                StartCoroutine(ShowInvalidDismantleWarningRoutine());
        }

        private IEnumerator ShowInvalidDismantleWarningRoutine()
        {
            yield return SayKey("tutorial.dismantle.blocked");
            invalidDismantleWarningRoutine = null;
        }

        private void HandleSkillCreated(Block block, Vector2Int _)
        {
            if (block is HealerBlock)
                healerCreated = true;
            if (block is AttackBlock)
                attackCreated = true;
        }

        private void HandleStageCleared(int _, ClearType clearType)
        {
            this.clearType = clearType;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.GameOver)
                gameOver = true;
        }

        private void HandleAdvance() => advanceRequested = true;

        private void OnDestroy()
        {
            if (typewriter != null) typewriter.AdvanceRequested -= HandleAdvance;
            if (placementController != null)
            {
                placementController.BlockPlaced -= HandleBlockPlaced;
                placementController.BlockDismantleRequested -= CanDismantleBlock;
                placementController.BlockDismantled -= HandleBlockDismantled;
                placementController.SkillBlockCreated -= HandleSkillCreated;
            }
            GameManager.StageCleared -= HandleStageCleared;
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }
    }
}
