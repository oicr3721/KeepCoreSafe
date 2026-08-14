#if UNITY_EDITOR
using System;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KeepCoreSafe.Editor
{
    public static class RerollFeatureSetup
    {
        private const string GaugePrefabPath = "Assets/Prefabs/UI/DelayedFillGauge.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup/Reroll Feature")]
        public static void Apply()
        {
            ConfigureSupplyData();
            foreach (string scenePath in ScenePaths)
                ConfigureScene(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Reroll Energy feature setup completed.");
        }

        [MenuItem("Keep Core Safe/Validate/Reroll Feature")]
        public static void Validate()
        {
            BlockSupplyData supply = AssetDatabase.LoadAssetAtPath<BlockSupplyData>(
                "Assets/Resources/Data/Systems/BlockSupplyData.asset");
            if (supply == null || !Mathf.Approximately(
                    supply.GetRareBlockChance(3) - supply.GetRareBlockChance(0),
                    0.03f))
            {
                throw new InvalidOperationException("Rare chance does not increase by 0.01 per reroll.");
            }

            ValidateSignedEnergyFlow();
            foreach (string scenePath in ScenePaths)
                ValidateScene(scenePath);

            Debug.Log("Reroll Energy feature validation passed.");
        }

        private static void ConfigureSupplyData()
        {
            BlockSupplyData supply = AssetDatabase.LoadAssetAtPath<BlockSupplyData>(
                "Assets/Resources/Data/Systems/BlockSupplyData.asset");
            if (supply == null)
                throw new InvalidOperationException("BlockSupplyData asset is missing.");

            SerializedObject serialized = new(supply);
            serialized.FindProperty("rareChanceIncreasePerReroll").floatValue = 0.01f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(supply);
        }

        private static void ValidateSignedEnergyFlow()
        {
            GameObject testObject = new("Reroll Energy Validation");
            try
            {
                CoreEnergyController energy = testObject.AddComponent<CoreEnergyController>();
                energy.BeginPreparation(10);
                for (int cost = 1; cost <= 4; cost++)
                {
                    if (!energy.TryApplyRerollCost(cost))
                        throw new InvalidOperationException($"Energy rejected valid reroll cost {cost}.");
                }

                if (energy.Energy.CurrentValue != -10 || energy.CanApplyRerollCost(5))
                    throw new InvalidOperationException("Energy debt or reroll cap is incorrect.");

                energy.BeginWave(10);
                energy.Energy.AddValue(11);
                if (energy.Energy.CurrentValue != 1)
                    throw new InvalidOperationException("Combat Energy did not repay debt before positive charge.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testObject);
            }
        }

        private static void ConfigureScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PreparationUI preparation = UnityEngine.Object.FindFirstObjectByType<PreparationUI>(
                FindObjectsInactive.Include);
            ShockwaveCountdownUI shockwave = UnityEngine.Object.FindFirstObjectByType<ShockwaveCountdownUI>(
                FindObjectsInactive.Include);
            if (preparation == null || shockwave == null)
                throw new InvalidOperationException($"{scene.name}: required Energy UI components are missing.");

            RemoveNextWaveEnergyLabels(preparation);
            ConfigureShockwaveUI(scene, shockwave);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        internal static void ConfigureShockwaveUI(Scene scene, ShockwaveCountdownUI shockwave)
        {
            DelayedFillGauge normal = null;
            foreach (DelayedFillGauge candidate in shockwave.GetComponentsInChildren<DelayedFillGauge>(true))
            {
                if (candidate.gameObject.name != "Minus Fill Gauge")
                {
                    normal = candidate;
                    break;
                }
            }

            if (normal == null)
                throw new InvalidOperationException($"{scene.name}: normal Energy gauge is missing.");

            DelayedFillGauge minus = null;
            foreach (DelayedFillGauge candidate in shockwave.GetComponentsInChildren<DelayedFillGauge>(true))
            {
                if (candidate.gameObject.name == "Minus Fill Gauge")
                {
                    minus = candidate;
                    break;
                }
            }

            if (minus == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GaugePrefabPath);
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                instance.name = "Minus Fill Gauge";
                instance.transform.SetParent(normal.transform.parent, false);
                CopyRect(normal.transform as RectTransform, instance.transform as RectTransform);
                instance.transform.SetSiblingIndex(normal.transform.GetSiblingIndex() + 1);
                minus = instance.GetComponent<DelayedFillGauge>();
            }

            ConfigureMinusGauge(minus);

            TMP_Text styleSource = shockwave.GetComponentInChildren<TMP_Text>(true);
            TMP_Text current = FindText(shockwave.transform, "Current Energy Text")
                               ?? CreateEnergyText(shockwave.transform, "Current Energy Text", styleSource, -28f);
            TMP_Text required = FindText(shockwave.transform, "Required Energy Text")
                                ?? CreateEnergyText(shockwave.transform, "Required Energy Text", styleSource, 28f);

            SerializedObject serialized = new(shockwave);
            serialized.FindProperty("visualRoot").objectReferenceValue = shockwave.gameObject;
            serialized.FindProperty("normalFillGauge").objectReferenceValue = normal;
            serialized.FindProperty("minusFillGauge").objectReferenceValue = minus;
            serialized.FindProperty("currentEnergy").objectReferenceValue = current;
            serialized.FindProperty("requiredEnergy").objectReferenceValue = required;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            shockwave.gameObject.SetActive(true);
        }

        private static void ConfigureMinusGauge(DelayedFillGauge minus)
        {
            Color red = new(1f, 0.18f, 0.12f, 1f);
            foreach (Image image in minus.GetComponentsInChildren<Image>(true))
            {
                image.color = image.gameObject == minus.gameObject
                    ? new Color(0f, 0f, 0f, 0f)
                    : red;
                image.raycastTarget = false;
            }

            SerializedObject serialized = new(minus);
            serialized.FindProperty("increaseColor").colorValue = red;
            serialized.FindProperty("decreaseColor").colorValue = new Color(0.65f, 0.05f, 0.03f, 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text CreateEnergyText(Transform parent, string name, TMP_Text styleSource, float x)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.layer = LayerMask.NameToLayer("UI");
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
            if (styleSource != null)
            {
                label.font = styleSource.font;
                label.fontSharedMaterial = styleSource.fontSharedMaterial;
                label.fontSize = styleSource.fontSize;
                label.color = styleSource.color;
            }

            label.text = name == "Current Energy Text" ? "0" : "/0";
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(x, -52f);
            rect.sizeDelta = new Vector2(72f, 38f);
            return label;
        }

        private static TMP_Text FindText(Transform root, string name)
        {
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.gameObject.name == name)
                    return label;
            }
            return null;
        }

        private static void RemoveNextWaveEnergyLabels(PreparationUI preparation)
        {
            foreach (Transform child in preparation.GetComponentsInChildren<Transform>(true))
            {
                if (child != preparation.transform && child.gameObject.name == "Next Wave Energy Text")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void CopyRect(RectTransform source, RectTransform destination)
        {
            destination.anchorMin = source.anchorMin;
            destination.anchorMax = source.anchorMax;
            destination.pivot = source.pivot;
            destination.anchoredPosition = source.anchoredPosition;
            destination.sizeDelta = source.sizeDelta;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        private static void ValidateScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PreparationUI preparation = UnityEngine.Object.FindFirstObjectByType<PreparationUI>(
                FindObjectsInactive.Include);
            ShockwaveCountdownUI shockwave = UnityEngine.Object.FindFirstObjectByType<ShockwaveCountdownUI>(
                FindObjectsInactive.Include);
            if (preparation == null || shockwave == null)
                throw new InvalidOperationException($"{scene.name}: required Energy UI components are missing.");

            foreach (Transform child in preparation.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.name == "Next Wave Energy Text")
                    throw new InvalidOperationException($"{scene.name}: obsolete next-wave Energy label remains.");
            }

            SerializedObject serialized = new(shockwave);
            UnityEngine.Object normal = serialized.FindProperty("normalFillGauge").objectReferenceValue;
            UnityEngine.Object minus = serialized.FindProperty("minusFillGauge").objectReferenceValue;
            if (normal == null || minus == null || normal == minus)
                throw new InvalidOperationException($"{scene.name}: normal/minus Energy gauges are not configured.");
            if (serialized.FindProperty("currentEnergy").objectReferenceValue == null
                || serialized.FindProperty("requiredEnergy").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scene.name}: Energy text references are not configured.");
            }
        }
    }
}
#endif
