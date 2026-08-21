using KeepCoreSafe.Analytics;
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private SceneType sceneType;

    public void LoadScene()
    {
        if (sceneType == SceneType.Game || sceneType == SceneType.Tutorial)
        {
            SceneType requestedScene = sceneType;
            AnalyticsConsentBootstrap.ContinueAfterDecision(
                () => SceneLoader.Load(requestedScene));
            return;
        }

        SceneLoader.Load(sceneType);
    }
}