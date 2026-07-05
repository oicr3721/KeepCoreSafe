using System.Collections;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
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
        private bool waveCompleted;
        private bool shockwaveCompleted;
        private bool gameOver;
        private Vector2Int greenTarget;

        private void Start()
        {
            typewriter.AdvanceRequested += HandleAdvance;
            placementController.BlockPlaced += HandleBlockPlaced;
            placementController.BlockDismantled += HandleBlockDismantled;
            placementController.SkillBlockCreated += HandleSkillCreated;
            waveManager.WaveCompleted += HandleWaveCompleted;
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
                yield return Say("안녕. 나는 Lily야. 혼자서는 방어 시스템을 완성하기 어려워. 나와 함께 코어를 지켜줄래?");
                yield return Say("이번에는 빨강 두 개와 초록 하나를 준비했어. 먼저 배치 목록을 확정해 보자.");
            }

            yield return new WaitUntil(() => placementController.PlacementInputEnabled);
            Vector2Int core = GridManager.Instance.Grid.Core.GridPosition;
            greenTarget = core + Vector2Int.right + Vector2Int.down;
            yield return Say("초록 블록을 기존 초록 블록 사이에 연결해 봐. 세 개가 이어지면 회복 블록이 만들어질 거야.");
            gridHighlight.Show(greenTarget);

            while (!healerCreated)
            {
                if (wrongGreenPlacement)
                {
                    wrongGreenPlacement = false;
                    gridHighlight.Hide();
                    yield return Say("실수한 거지? 괜찮아. 잘못 배치한 블록은 철거할 수 있어. 철거하면 포인트를 받을 수 있지만 다시 배치할 수는 없으니까 신중하게 설치해야 해.");
                    yield return new WaitUntil(() => wrongGreenBlockDismantled || wrongGreenBlock == null);
                    wrongGreenBlock = null;
                    wrongGreenBlockDismantled = false;
                    supplyController.AddGrantedBlock(greenBlock, false);
                    gridHighlight.Show(greenTarget);
                }
                yield return null;
            }

            gridHighlight.Hide();
            yield return Say("잘했어! 초록 블록이 3개 연결되면 이렇게 회복 블록으로 변한단다.");
            yield return Say("이번에는 빨강 블록 두 개를 위쪽의 빨강 블록과 이어 공격 블록을 만들어 보자.");
            while (redBlocksPlaced < 2)
            {
                Vector2Int target = core + Vector2Int.up + (redBlocksPlaced == 0 ? Vector2Int.left : Vector2Int.right);
                gridHighlight.Show(target);
                yield return null;
            }
            gridHighlight.Hide();

            if (attackCreated)
                yield return Say("잘했어! 이제 방어를 시작해 보자.");
            else
                yield return Say("응. 아직은 벽이 부족할지도 몰라. 특수 블록은 일반 블록 세 개를 사용하는 만큼, 당장 코어를 지켜야 한다면 생성을 미루는 것도 훌륭한 전략이야. 좋아, 그럼 이제 시작해 보자.");

            preparationUI.SetStartWaveAllowed(true);
            yield return new WaitUntil(() => GameManager.Phase == GamePhase.Combat || gameOver);
            yield return new WaitUntil(() => GameManager.Phase != GamePhase.Combat || gameOver);

            if (gameOver)
            {
                yield return Say("이런이런... 처음부터 다시 해볼까?");
                resumeFromBuildStep = true;
                SceneManager.LoadScene("TutorialScene");
                yield break;
            }

            if (shockwaveCompleted)
                yield return Say("이렇게 충격파가 충전될 때까지 버틸 수만 있다면 모든 적을 한 번에 제거할 수 있어.");
            else
                yield return Say("좋아! 적을 모두 처치했네. 물론 이렇게 적을 먼저 쓰러뜨릴 수도 있지만, 그렇지 못하더라도 충격파가 충전될 때까지만 버티면 모든 적을 제거할 수 있어. 하지만 최고의 방어는 최고의 공격이기도 하지.");

            dialogueRoot.SetActive(true);
            typewriter.Play("잘했어! 이렇게 금방 배우다니 너는 역시 대단...");
            yield return new WaitForSecondsRealtime(0.65f);
            glitchTransition.Play();
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

        private void HandleSkillCreated(Block block, Vector2Int _)
        {
            if (block is HealerBlock)
                healerCreated = true;
            if (block is AttackBlock)
                attackCreated = true;
        }

        private void HandleWaveCompleted()
        {
            waveCompleted = true;
        }

        private void HandleStageCleared(int _)
        {
            shockwaveCompleted = true;
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
                placementController.BlockDismantled -= HandleBlockDismantled;
                placementController.SkillBlockCreated -= HandleSkillCreated;
            }
            if (waveManager != null) waveManager.WaveCompleted -= HandleWaveCompleted;
            GameManager.StageCleared -= HandleStageCleared;
            GameManager.PhaseChanged -= HandlePhaseChanged;
        }
    }
}
