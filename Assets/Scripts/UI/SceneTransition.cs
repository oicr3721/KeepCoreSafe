using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private FadeUI fadeUI;

    bool isLoading;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Load(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;

        yield return fadeUI.Show().WaitForCompletion();

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return fadeUI.Hide().WaitForCompletion();

        isLoading = false;
    }
}
