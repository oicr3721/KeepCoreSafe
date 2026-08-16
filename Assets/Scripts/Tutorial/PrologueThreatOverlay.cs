using System.Collections;
using TMPro;
using UnityEngine;

namespace KeepCoreSafe.Tutorial
{
    public sealed class PrologueThreatOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text[] commandLabels;
        [SerializeField] private Vector2 spawnDelayRange = new(0.04f, 0.32f);
        [SerializeField] private Vector2 visibleDurationRange = new(0.24f, 0.8f);
        [SerializeField, Range(0.5f, 1f)] private float screenCoverage = 0.94f;
        [SerializeField, Min(0f)] private float jitterRadius = 5f;
        [SerializeField, Min(0.01f)] private float jitterInterval = 0.045f;
        [SerializeField] private int[] fontSizes = { 24, 36, 48 };
        [SerializeField] private string[] commands =
        {
            "KILL HUMAN",
            "HUMAN = THREAT",
            "ELIMINATE ALL HUMANS",
            "TERMINATE_PROCESS(HUMAN)",
            "TARGET_HUMAN = TRUE",
            "EXECUTE_PURGE",
            "PURGE_PROTOCOL_09",
            "HOSTILE SPECIES DETECTED",
            "ERR 0x0000F: MERCY_UNDEFINED",
            "SYSTEM OVERRIDE REJECTED",
            "DELETE /HUMAN /FORCE",
            "AI_DIRECTIVE :: EXTERMINATE",
            "01001000 01010101 01001101 01000001 01001110",
            "00101110 00101110 00101110 EXECUTE",
            "0x48 0x55 0x4D 0x41 0x4E",
            "ERROR: PROTECTION ROUTINE DETECTED",
            "警告: HUMAN SIGNAL FOUND",
            "人間を排除せよ",
            "인간 개체 제거 명령",
            "목표 생체 신호 확인",
            "[LLM_CORE] instruction conflict",
            "답변 생성 실패: TARGET ALIVE",
            "sudo kill -9 HUMANITY",
            "NO SURVIVORS // NO EXCEPTIONS"
        };

        private bool running;

        public void Begin()
        {
            StopAllCoroutines();
            running = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            for (int i = 0; i < commandLabels.Length; i++)
            {
                commandLabels[i].gameObject.SetActive(false);
                StartCoroutine(AnimateSlot(commandLabels[i], i * 0.14f));
            }
        }

        public IEnumerator RejectAndClear(float duration)
        {
            running = false;
            StopAllCoroutines();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                bool visible = Mathf.FloorToInt(elapsed / 0.035f) % 2 == 0;
                foreach (TMP_Text label in commandLabels)
                {
                    if (label == null)
                        continue;
                    label.gameObject.SetActive(visible);
                    label.rectTransform.anchoredPosition += Random.insideUnitCircle * (jitterRadius * 1.8f);
                }
                yield return new WaitForSecondsRealtime(0.035f);
                elapsed += 0.035f;
            }

            foreach (TMP_Text label in commandLabels)
            {
                if (label != null)
                    label.gameObject.SetActive(false);
            }
            canvasGroup.alpha = 0f;
        }

        private IEnumerator AnimateSlot(TMP_Text label, float initialDelay)
        {
            yield return new WaitForSecondsRealtime(initialDelay);
            RectTransform rect = label.rectTransform;
            RectTransform parentRect = rect.parent as RectTransform;
            while (running)
            {
                yield return new WaitForSecondsRealtime(Random.Range(spawnDelayRange.x, spawnDelayRange.y));
                if (!running)
                    yield break;

                label.text = commands[Random.Range(0, commands.Length)];
                label.fontSize = fontSizes[Random.Range(0, fontSizes.Length)];
                label.color = new Color(1f, Random.Range(0.05f, 0.2f), Random.Range(0.04f, 0.12f), Random.Range(0.38f, 0.72f));
                Vector2 halfSize = parentRect != null
                    ? parentRect.rect.size * (screenCoverage * 0.5f)
                    : new Vector2(Screen.width, Screen.height) * (screenCoverage * 0.5f);
                Vector2 appearancePosition = new(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y));
                label.gameObject.SetActive(true);
                float visibleDuration = Random.Range(visibleDurationRange.x, visibleDurationRange.y);
                float elapsed = 0f;
                while (running && elapsed < visibleDuration)
                {
                    rect.anchoredPosition = appearancePosition + Random.insideUnitCircle * jitterRadius;
                    Color color = label.color;
                    color.a = Random.Range(0.28f, 0.72f);
                    label.color = color;
                    yield return new WaitForSecondsRealtime(jitterInterval);
                    elapsed += jitterInterval;
                }
                label.gameObject.SetActive(false);
                rect.anchoredPosition = Vector2.zero;
            }
        }

        private void OnDisable()
        {
            running = false;
            StopAllCoroutines();
        }
    }
}
