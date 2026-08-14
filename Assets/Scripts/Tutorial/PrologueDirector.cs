using System.Collections;
using DG.Tweening;
using KeepCoreSafe.Localization;
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

        [SerializeField]
        private string[] paragraphKeys =
        {
            "prologue.1",
            "prologue.2",
            "prologue.3",
            "prologue.4",
            "prologue.5",
            "prologue.6"
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

            foreach (string paragraphKey in paragraphKeys)
            {
                advanceRequested = false;
                typewriter.Play(LocalizationManager.Get(paragraphKey));
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
