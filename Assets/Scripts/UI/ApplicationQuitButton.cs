using UnityEngine;
using UnityEngine.UI;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ApplicationQuitButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button?.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
