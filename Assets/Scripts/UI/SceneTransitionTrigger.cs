using System;
using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private SceneType sceneType;

    public void LoadScene()
    {
        SceneLoader.Load(sceneType);
    }
}
