#if UNITY_EDITOR
using System;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class OffersEventHoverSetup
    {
        private const string ScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Keep Core Safe/Setup/Offers Event Hover Fade")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject visual = FindVisual();
            Image image = visual.GetComponent<Image>();
            CanvasGroup group = visual.GetComponent<CanvasGroup>();
            if (image == null || group == null)
            {
                throw new InvalidOperationException(
                    "Offer Event UI/Visual must already contain its Image and CanvasGroup.");
            }

            image.raycastTarget = true;
            if (visual.GetComponent<HoverCanvasGroupFade>() == null)
                visual.AddComponent<HoverCanvasGroupFade>();

            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(visual);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Validate();
            Debug.Log("OFFERS_EVENT_HOVER_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Offers Event Hover Fade")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject visual = FindVisual();
            Image image = visual.GetComponent<Image>();
            CanvasGroup group = visual.GetComponent<CanvasGroup>();
            HoverCanvasGroupFade fade = visual.GetComponent<HoverCanvasGroupFade>();
            if (image == null
                || !image.raycastTarget
                || visual.GetComponents<Image>().Length != 1
                || group == null
                || !group.interactable
                || !group.blocksRaycasts
                || fade == null
                || !Mathf.Approximately(fade.HoverAlpha, 0.001f)
                || fade.FadeDuration <= 0f
                || visual.GetComponent<Collider2D>() != null
                || visual.GetComponent<Collider>() != null)
            {
                throw new InvalidOperationException(
                    "Offer Event UI hover fade configuration is incomplete.");
            }

            Debug.Log("OFFERS_EVENT_HOVER_VALIDATION_COMPLETE");
        }

        private static GameObject FindVisual()
        {
            ShopEventUI shop = UnityEngine.Object.FindFirstObjectByType<ShopEventUI>(
                FindObjectsInactive.Include);
            if (shop == null)
                throw new InvalidOperationException("GameScene has no ShopEventUI.");

            SerializedObject serialized = new(shop);
            GameObject visual = serialized.FindProperty("visualRoot").objectReferenceValue as GameObject;
            if (visual == null || visual.name != "Visual")
                throw new InvalidOperationException("ShopEventUI Visual reference is missing.");
            return visual;
        }
    }
}
#endif
