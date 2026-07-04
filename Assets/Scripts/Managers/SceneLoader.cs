using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SceneType
{
    Title,
    Game,
    Tutorial
}

public static class SceneLoader
{
    private static readonly Dictionary<SceneType, string> sceneTable = new()
    {
        { SceneType.Title, "TitleScene" },
        { SceneType.Game, "GameScene" },
        { SceneType.Tutorial, "TutorialScene" },
    };

    public static void Load(SceneType scene)
    {
        if (!sceneTable.TryGetValue(scene, out string sceneName))
        {
            Debug.LogError($"등록되지 않은 Scene : {scene}");
            return;
        }

        SceneTransition.Instance.Load(sceneName);
    }
}