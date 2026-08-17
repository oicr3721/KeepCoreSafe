using UnityEngine;

namespace KeepCoreSafe.Managers
{
    public static class BestWaveRecord
    {
        private const string PrefsKey = "KeepCoreSafe.BestWave";

        public static int BestWave => Mathf.Max(0, PlayerPrefs.GetInt(PrefsKey, 0));
        public static bool LastGameOverWasNewBest { get; private set; }
        public static int LastGameOverWave { get; private set; }

        public static void BeginRun()
        {
            LastGameOverWasNewBest = false;
            LastGameOverWave = 0;
        }

        public static void RegisterGameOver(int wave)
        {
            int completedWave = Mathf.Max(0, wave);
            int previousBest = BestWave;
            LastGameOverWave = completedWave;
            LastGameOverWasNewBest = completedWave > previousBest;
            if (!LastGameOverWasNewBest)
                return;

            PlayerPrefs.SetInt(PrefsKey, completedWave);
            PlayerPrefs.Save();
        }
    }
}
