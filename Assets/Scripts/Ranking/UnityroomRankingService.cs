using System;
using UnityEngine;

#if UNITY_WEBGL && UNITYROOM
using unityroom.Api;
#endif

namespace KeepCoreSafe.Ranking
{
    public sealed class UnityroomRankingRuntime : MonoBehaviour
    {
        [SerializeField, Min(1)] private int boardNo = 1;

        private static UnityroomRankingRuntime instance;
        private int BoardNo => boardNo;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        internal static bool TrySubmit(int wave)
        {
#if UNITY_WEBGL && UNITYROOM
            if (instance == null)
            {
                Debug.LogWarning("[Unityroom Ranking] Runtime configuration is missing; score submission was skipped.");
                return false;
            }

            IUnityroomApiClient client = UnityroomApiClient.Instance;
            if (client == null)
            {
                Debug.LogWarning("[Unityroom Ranking] Official API client is unavailable; score submission was skipped.");
                return false;
            }

            client.SendScore(instance.BoardNo, wave, ScoreboardWriteMode.HighScoreDesc);
            return true;
#else
            return false;
#endif
        }
    }

    public static class UnityroomRankingService
    {
#if UNITY_WEBGL && UNITYROOM
        private static bool submittedThisRun;
#endif

        public static void BeginRun()
        {
#if UNITY_WEBGL && UNITYROOM
            submittedThisRun = false;
#endif
        }

        public static void SubmitGameOverWave(int wave)
        {
#if UNITY_WEBGL && UNITYROOM
            if (submittedThisRun)
                return;

            submittedThisRun = true;
            try
            {
                UnityroomRankingRuntime.TrySubmit(Mathf.Max(0, wave));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Unityroom Ranking] Score submission failed without affecting Game Over: {exception.Message}");
            }
#endif
        }
    }
}
