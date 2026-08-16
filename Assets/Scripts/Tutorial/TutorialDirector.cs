using System.Collections;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using KeepCoreSafe.Localization;
using KeepCoreSafe.Managers;
using KeepCoreSafe.Settings;
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
        [SerializeField] private TutorialColorblindPrompt colorblindPrompt;
        [SerializeField] private Transform lilyTransform;
        [SerializeField] private Animator lilyAnimator;
        [SerializeField] private Vector2Int lilyOffsetFromCore = new(4, 0);
        [SerializeField, Min(0.1f)] private float happyReactionDuration = 0.9f;

        [Header("Tutorial Data")]
        [SerializeField] private BasicBlockData redBlock;
        [SerializeField] private BasicBlockData greenBlock;

        private static bool resumeFromBuildStep;
        private bool advanceRequested;
        private bool healerCreated;
        private bool attackCreated;
        private bool wrongFirstPlacement;
        private Block wrongFirstBlock;
        private bool wrongFirstBlockDismantled;
        private int greenBlocksPlaced;
        private ClearType clearType;
        private bool gameOver;
        private Vector2Int firstTarget;
        private Coroutine invalidDismantleWarningRoutine;
        private Coroutine colorblindGuidanceRoutine;
        private bool firstLessonSelectionRequired;
        private Coroutine lilyReactionRoutine;
        private Coroutine lilyPlacementWarningRoutine;
        private Vector2Int lilyCell;

        private static readonly int IdleTrigger = Animator.StringToHash("Idle");
        private static readonly int HappyTrigger = Animator.StringToHash("Happy");

        private void Start()
        {
            typewriter.AdvanceRequested += HandleAdvance;
            placementController.BlockPlaced += HandleBlockPlaced;
            placementController.GrantedBlockSelectionRequested += CanSelectGrantedBlock;
            placementController.BlockPlacementValidationRequested += CanPlaceBlockAtLilyCell;
            placementController.BlockPlacementRejected += HandleBlockPlacementRejected;
            placementController.BlockDismantleRequested += CanDismantleBlock;
            placementController.BlockDismantled += HandleBlockDismantled;
            placementController.SkillBlockCreated += HandleSkillCreated;
            GameManager.StageCleared += HandleStageCleared;
            GameManager.PhaseChanged += HandlePhaseChanged;
            preparationUI.SetStartWaveAllowed(false);
            dialogueRoot.SetActive(false);
            gridHighlight.Hide();
            PositionLilyOnGrid();
            SetLilyTrigger(IdleTrigger);
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
            firstTarget = core + Vector2Int.right + Vector2Int.down;
            firstLessonSelectionRequired = true;
            placementController.ClearSelection();
            yield return SayKey("tutorial.first.red.connect");
            gridHighlight.Show(firstTarget);

            while (!attackCreated)
            {
                if (wrongFirstPlacement)
                {
                    wrongFirstPlacement = false;
                    gridHighlight.Hide();
                    yield return SayKey("tutorial.first.red.wrong");
                    yield return new WaitUntil(() => wrongFirstBlockDismantled || wrongFirstBlock == null);
                    wrongFirstBlock = null;
                    wrongFirstBlockDismantled = false;
                    supplyController.AddGrantedBlock(redBlock, false);
                    gridHighlight.Show(firstTarget);
                }
                yield return null;
            }

            gridHighlight.Hide();
            firstLessonSelectionRequired = false;
            yield return SayKey("tutorial.first.attack.success");
            yield return SayKey("tutorial.second.green.connect");
            while (greenBlocksPlaced < 2)
            {
                Vector2Int target = core
                    + (greenBlocksPlaced == 0 ? Vector2Int.right + Vector2Int.up : Vector2Int.right);
                gridHighlight.Show(target);
                yield return null;
            }
            gridHighlight.Hide();

            if (healerCreated)
                yield return SayKey("tutorial.second.healer.success");
            else
                yield return SayKey("tutorial.second.healer.fail");

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

            PlayHappyReaction();
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
            if (block.Data == redBlock && !attackCreated && position != firstTarget)
            {
                wrongFirstPlacement = true;
                wrongFirstBlock = block;
                wrongFirstBlockDismantled = false;
            }
            if (block.Data == greenBlock)
                greenBlocksPlaced++;
        }

        private void PositionLilyOnGrid()
        {
            if (lilyTransform == null
                || GridManager.Instance == null
                || GridManager.Instance.Grid?.Core == null)
            {
                Debug.LogError("Tutorial Lily or Core is not ready for Grid positioning.", this);
                return;
            }

            lilyCell = GridManager.Instance.Grid.Core.GridPosition + lilyOffsetFromCore;
            if (!GridManager.Instance.Grid.IsWithinBounds(lilyCell))
            {
                Debug.LogError($"Tutorial Lily cell {lilyCell} is outside the Grid.", this);
                return;
            }

            lilyTransform.position = GridManager.Instance.GridToWorld(lilyCell);
        }

        private bool CanPlaceBlockAtLilyCell(BlockData _, Vector2Int position)
        {
            return lilyTransform == null || position != lilyCell;
        }

        private void HandleBlockPlacementRejected(BlockData _, Vector2Int position)
        {
            if (lilyTransform == null || position != lilyCell)
                return;

            PlayHappyReaction();
            if (lilyPlacementWarningRoutine == null && !dialogueRoot.activeSelf)
                lilyPlacementWarningRoutine = StartCoroutine(ShowLilyPlacementWarning());
        }

        private IEnumerator ShowLilyPlacementWarning()
        {
            yield return SayKey("tutorial.lily.placement_blocked");
            lilyPlacementWarningRoutine = null;
        }

        private bool CanSelectGrantedBlock(
            int _,
            BlockSupplyController.GrantedBlock grant)
        {
            if (!firstLessonSelectionRequired || attackCreated || grant.Data != greenBlock)
                return true;

            if (colorblindGuidanceRoutine == null)
                colorblindGuidanceRoutine = StartCoroutine(ShowColorblindGuidance());
            return false;
        }

        private IEnumerator ShowColorblindGuidance()
        {
            yield return SayKey("tutorial.colorblind.question");

            if (colorblindPrompt == null)
            {
                Debug.LogError("Tutorial Colorblind Prompt is not configured.", this);
                colorblindGuidanceRoutine = null;
                yield break;
            }

            bool resolved = false;
            bool enableMode = false;
            colorblindPrompt.Show(choice =>
            {
                enableMode = choice;
                resolved = true;
            });
            yield return new WaitUntil(() => resolved);
            AccessibilitySettings.SetColorblindModeEnabled(enableMode);
            colorblindGuidanceRoutine = null;
        }

        private void HandleBlockDismantled(Block block, Vector2Int _)
        {
            if (block == wrongFirstBlock)
                wrongFirstBlockDismantled = true;
        }

        private bool CanDismantleBlock(Block block, Vector2Int _)
        {
            if (block != null && block == wrongFirstBlock)
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
            {
                healerCreated = true;
                PlayHappyReaction();
            }
            if (block is AttackBlock)
            {
                attackCreated = true;
                firstLessonSelectionRequired = false;
                PlayHappyReaction();
            }
        }

        private void PlayHappyReaction()
        {
            if (lilyAnimator == null)
                return;

            if (lilyReactionRoutine != null)
                StopCoroutine(lilyReactionRoutine);
            lilyReactionRoutine = StartCoroutine(PlayHappyReactionRoutine());
        }

        private IEnumerator PlayHappyReactionRoutine()
        {
            SetLilyTrigger(HappyTrigger);
            yield return new WaitForSecondsRealtime(happyReactionDuration);
            SetLilyTrigger(IdleTrigger);
            lilyReactionRoutine = null;
        }

        private void SetLilyTrigger(int trigger)
        {
            if (lilyAnimator == null)
                return;

            lilyAnimator.ResetTrigger(IdleTrigger);
            lilyAnimator.ResetTrigger(HappyTrigger);
            lilyAnimator.SetTrigger(trigger);
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
                placementController.GrantedBlockSelectionRequested -= CanSelectGrantedBlock;
                placementController.BlockPlacementValidationRequested -= CanPlaceBlockAtLilyCell;
                placementController.BlockPlacementRejected -= HandleBlockPlacementRejected;
                placementController.BlockDismantleRequested -= CanDismantleBlock;
                placementController.BlockDismantled -= HandleBlockDismantled;
                placementController.SkillBlockCreated -= HandleSkillCreated;
            }
            GameManager.StageCleared -= HandleStageCleared;
            GameManager.PhaseChanged -= HandlePhaseChanged;
            if (lilyReactionRoutine != null)
                StopCoroutine(lilyReactionRoutine);
        }
    }
}
