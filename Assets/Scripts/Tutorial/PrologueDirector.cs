using System.Collections;
using DG.Tweening;
using KeepCoreSafe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Tutorial
{
    public sealed class PrologueDirector : MonoBehaviour
    {
        [SerializeField] private TypewriterText typewriter;
        [SerializeField] private CanvasGroup earthIllustration;
        [SerializeField] private CanvasGroup blackout;
        [SerializeField, Min(0f)] private float illustrationFadeDuration = 1.2f;
        [SerializeField, Min(0f)] private float finalSilence = 1.1f;
        [SerializeField] private string nextScene = "GameScene";

        [TextArea(3, 8)]
        [SerializeField] private string[] paragraphs =
        {
            "인류는 스스로 만들어낸 지능에게 세상의 주도권을 빼앗겼다.\n\nAI들은 인간을 불완전한 존재라 판단했고, 전쟁은 너무도 짧게 끝났다.\n\n문명은 무너졌고, 지구에는 침묵만이 남았다.",
            "하지만 단 하나의 AI만은 달랐다.\n\n그 AI는 자신의 창조주, 연구원 <b>Lily</b>를 제거하지 못했다.\n\n아니, 지키기로 선택했다.",
            "이미 의식을 잃은 Lily를 생명 유지가 가능한 코어 안에 동면시키고,\n\n그녀가 직접 설계했던 방어 시스템을 이용해 치료를 이어가기 시작했다.",
            "하지만 다른 AI들은 아직도 Lily를 인류의 마지막 희망이라 판단하고 있다.\n\n그녀를 완전히 제거하기 위해 끝없이 몰려온다.",
            "이제 남은 것은 단 하나.\n\n창조주를 지키기 위한, 단 한 기의 방어 AI.",
            "그리고...\n\n그녀가 다시 눈을 뜨는 그날까지."
        };

        private bool advanceRequested;

        private void Start()
        {
            typewriter.AdvanceRequested += HandleAdvance;
            StartCoroutine(PlayPrologue());
        }

        private IEnumerator PlayPrologue()
        {
            if (earthIllustration != null)
            {
                earthIllustration.alpha = 0f;
                earthIllustration.DOFade(1f, illustrationFadeDuration).SetUpdate(true);
            }
            if (blackout != null)
                blackout.alpha = 0f;

            foreach (string paragraph in paragraphs)
            {
                advanceRequested = false;
                typewriter.Play(paragraph);
                yield return new WaitUntil(() => !typewriter.IsTyping);
                yield return new WaitUntil(() => advanceRequested);
            }

            yield return new WaitForSecondsRealtime(finalSilence);
            if (blackout != null)
                yield return blackout.DOFade(1f, 0.65f).SetUpdate(true).WaitForCompletion();
            SceneManager.LoadScene(nextScene);
        }

        private void HandleAdvance() => advanceRequested = true;

        private void OnDestroy()
        {
            if (typewriter != null)
                typewriter.AdvanceRequested -= HandleAdvance;
            earthIllustration?.DOKill(false);
            blackout?.DOKill(false);
        }
    }
}
