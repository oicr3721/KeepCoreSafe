#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using KeepCoreSafe.Data;
using KeepCoreSafe.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeepCoreSafe.Editor
{
    public static class WaveDataStructureSetup
    {
        private const string WaveFolder = "Assets/Resources/Data/Waves";
        private const string MainDifficultyPath =
            "Assets/Resources/Data/Systems/WaveDifficultyData.asset";
        private const string TutorialDifficultyPath =
            "Assets/Resources/Data/Systems/TutorialDifficultyData.asset";
        private const string MeleeDataPath =
            "Assets/Resources/Data/Enemy/MeleeEnemyData.asset";
        private const string RangedDataPath =
            "Assets/Resources/Data/Enemy/RangedEnemyData.asset";
        private const string SuicideDataPath =
            "Assets/Resources/Data/Enemy/SuicideEnemyData.asset";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/TutorialScene.unity"
        };

        [MenuItem("Keep Core Safe/Setup/Wave Data Structure")]
        public static void Apply()
        {
            EnsureFolder(WaveFolder);
            EnemyData melee = LoadEnemy(MeleeDataPath);
            EnemyData ranged = LoadEnemy(RangedDataPath);
            EnemyData suicide = LoadEnemy(SuicideDataPath);

            WaveData basic = GetOrCreateWave(
                "BasicWave", "Basic Wave",
                "근접 적 중심의 기본 전투 리듬을 제공한다.",
                "안정적인 방어선과 공격 범위를 구성한다.",
                (melee, 65f), (ranged, 15f), (suicide, 20f));
            WaveData rangedPressure = GetOrCreateWave(
                "RangedPressure", "Ranged Pressure",
                "원거리 적의 비중을 높여 후방 위협 대응을 요구한다.",
                "사거리와 진입 경로를 고려해 우선 제거한다.",
                (melee, 40f), (ranged, 40f), (suicide, 20f));
            WaveData mixedAssault = GetOrCreateWave(
                "MixedAssault", "Mixed Assault",
                "세 적 유형을 혼합해 복합적인 방어 판단을 요구한다.",
                "자폭 적을 멀리 처리하면서 원거리 적도 견제한다.",
                (melee, 40f), (ranged, 20f), (suicide, 40f));
            WaveData explosiveAssault = GetOrCreateWave(
                "ExplosiveAssault", "Explosive Assault",
                "자폭 적을 통해 밀집된 Block 배치를 강하게 압박한다.",
                "자폭 적을 Core에서 먼 위치에서 제거한다.",
                (melee, 20f), (ranged, 20f), (suicide, 60f));
            WaveData rangedBarrage = GetOrCreateWave(
                "RangedBarrage", "Ranged Barrage",
                "다수의 원거리 적으로 화력 분산을 요구한다.",
                "원거리 적이 공격을 시작하기 전에 경로 전방에서 제거한다.",
                (melee, 30f), (ranged, 50f), (suicide, 20f));
            WaveData tutorial = GetOrCreateWave(
                "TutorialBasicWave", "Tutorial Basic Wave",
                "튜토리얼 진행을 보존하는 근접 적 전용 구성이다.",
                "기본 배치와 전투 흐름을 학습한다.",
                (melee, 100f));

            ConfigureDifficulty(
                MainDifficultyPath,
                new[] { basic, rangedPressure, mixedAssault },
                new[] { explosiveAssault, rangedBarrage },
                5);
            ConfigureDifficulty(
                TutorialDifficultyPath,
                new[] { tutorial },
                new[] { tutorial },
                0);

            foreach (string scenePath in ScenePaths)
                ConfigureSceneFallback(scenePath, melee);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("WAVE_DATA_STRUCTURE_SETUP_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Wave Data Structure")]
        public static void Validate()
        {
            ValidateDifficulty(MainDifficultyPath, true, 5);
            ValidateDifficulty(TutorialDifficultyPath, false, 0);

            WaveDifficultyData main = AssetDatabase.LoadAssetAtPath<WaveDifficultyData>(
                MainDifficultyPath);
            UnityEngine.Random.State randomState = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(20260815);
                WaveDifficultySnapshot first = main.Roll(1);
                WaveDifficultySnapshot second = main.Roll(2, first.WaveData);
                WaveDifficultySnapshot special = main.Roll(5, second.WaveData);
                if (first.WaveData == null
                    || second.WaveData == null
                    || first.WaveData == second.WaveData
                    || first.IsSpecialWave
                    || second.IsSpecialWave
                    || !special.IsSpecialWave
                    || !Contains(main.SpecialWaveList, special.WaveData))
                {
                    throw new InvalidOperationException(
                        "Normal/Special selection or consecutive-repeat prevention failed.");
                }

                List<EnemyData> composition = new();
                if (!first.WaveData.BuildComposition(20, composition)
                    || composition.Count != 20
                    || composition.Exists(data => data == null))
                {
                    throw new InvalidOperationException("WaveData weighted composition failed.");
                }
            }
            finally
            {
                UnityEngine.Random.state = randomState;
            }

            foreach (string scenePath in ScenePaths)
                ValidateSceneFallback(scenePath);

            Debug.Log("WAVE_DATA_STRUCTURE_VALIDATION_COMPLETE");
        }

        [MenuItem("Keep Core Safe/Validate/Wave Data Integration")]
        public static void ValidateIntegration()
        {
            Validate();
            FixedEnemyPathValidation.Validate();
            SuicideEnemyFeatureSetup.Validate();
            GridGameplayPhysicsRemovalSetup.Validate();
            Debug.Log("WAVE_DATA_INTEGRATION_VALIDATION_COMPLETE");
        }

        private static EnemyData LoadEnemy(string path)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
                throw new InvalidOperationException($"EnemyData is missing: {path}");
            return data;
        }

        private static WaveData GetOrCreateWave(
            string fileName,
            string waveName,
            string designIntent,
            string keyStrategy,
            params (EnemyData data, float weight)[] entries)
        {
            string path = $"{WaveFolder}/{fileName}.asset";
            WaveData data = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            bool created = data == null;
            if (created)
            {
                data = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new(data);
            SerializedProperty composition = serialized.FindProperty("enemyComposition");
            if (created || composition.arraySize == 0)
            {
                serialized.FindProperty("waveName").stringValue = waveName;
                serialized.FindProperty("designIntent").stringValue = designIntent;
                serialized.FindProperty("keyStrategy").stringValue = keyStrategy;
                composition.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    SerializedProperty entry = composition.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("enemyData").objectReferenceValue = entries[i].data;
                    entry.FindPropertyRelative("weight").floatValue = entries[i].weight;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }

            return data;
        }

        private static void ConfigureDifficulty(
            string path,
            IReadOnlyList<WaveData> normal,
            IReadOnlyList<WaveData> special,
            int interval)
        {
            WaveDifficultyData data = AssetDatabase.LoadAssetAtPath<WaveDifficultyData>(path);
            if (data == null)
                throw new InvalidOperationException($"WaveDifficultyData is missing: {path}");

            SerializedObject serialized = new(data);
            SetPoolIfEmpty(serialized.FindProperty("normalWaveList"), normal);
            SetPoolIfEmpty(serialized.FindProperty("specialWaveList"), special);
            serialized.FindProperty("specialWaveInterval").intValue = interval;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void SetPoolIfEmpty(
            SerializedProperty property,
            IReadOnlyList<WaveData> values)
        {
            if (property.arraySize > 0)
                return;

            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void ConfigureSceneFallback(string scenePath, EnemyData fallback)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveManager manager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(
                FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{scenePath} has no WaveManager.");

            SerializedObject serialized = new(manager);
            SerializedProperty property = serialized.FindProperty("fallbackEnemyData");
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = fallback;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void ValidateDifficulty(string path, bool expectsSpecial, int interval)
        {
            WaveDifficultyData data = AssetDatabase.LoadAssetAtPath<WaveDifficultyData>(path);
            if (data == null
                || data.NormalWaveList.Count == 0
                || data.SpecialWaveList.Count == 0
                || data.SpecialWaveInterval != interval)
            {
                throw new InvalidOperationException($"Wave pool configuration is incomplete: {path}");
            }

            foreach (WaveData wave in data.NormalWaveList)
            {
                if (wave == null || !wave.HasValidComposition())
                    throw new InvalidOperationException($"Normal WaveData is invalid: {path}");
            }
            foreach (WaveData wave in data.SpecialWaveList)
            {
                if (wave == null || !wave.HasValidComposition())
                    throw new InvalidOperationException($"Special WaveData is invalid: {path}");
            }

            WaveDifficultySnapshot snapshot = data.Roll(expectsSpecial ? interval : 1);
            if (snapshot.EnemyCount <= 0
                || snapshot.RequiredEnergy <= 0
                || snapshot.WaveData == null
                || snapshot.IsSpecialWave != expectsSpecial)
            {
                throw new InvalidOperationException($"Difficulty curve roll failed: {path}");
            }
        }

        private static void ValidateSceneFallback(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            WaveManager manager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(
                FindObjectsInactive.Include);
            if (manager == null
                || new SerializedObject(manager).FindProperty("fallbackEnemyData").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{scenePath} has no fallback EnemyData.");
            }
        }

        private static bool Contains(IReadOnlyList<WaveData> list, WaveData target)
        {
            foreach (WaveData item in list)
            {
                if (item == target)
                    return true;
            }
            return false;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
