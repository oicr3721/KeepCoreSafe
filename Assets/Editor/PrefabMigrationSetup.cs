using System.IO;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Combat;
using KeepCoreSafe.Data;
using KeepCoreSafe.Enemies;
using KeepCoreSafe.GridSystem;
using KeepCoreSafe.Managers;
using KeepCoreSafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class PrefabMigrationSetup
    {
        private const string ScenePath = "Assets/Scenes/FoundationTestScene.unity";
        private const string MaterialPath = "Assets/Materials/SpriteLine.mat";
        private const string WhiteSpritePath = "Assets/Sprites/WhiteSquare.png";
        private const string HealthBarPath = "Assets/Prefabs/UI/BlockHealthBar.prefab";
        private const string MissilePath = "Assets/Prefabs/Combat/MissileProjectile.prefab";
        private const string GridLinePath = "Assets/Prefabs/Grid/GridLine.prefab";

        [MenuItem("Keep Core Safe/Migrate Data and Create Prefabs")]
        public static void Run()
        {
            EnsureFolders();
            Sprite whiteSprite = EnsureWhiteSprite();
            Material lineMaterial = EnsureLineMaterial();
            BlockHealthBar healthBarPrefab = CreateHealthBarPrefab(whiteSprite);
            MissileProjectile missilePrefab = CreateMissilePrefab(lineMaterial);
            LineRenderer gridLinePrefab = CreateGridLinePrefab(lineMaterial);

            AttackBlock attackPrefab = CreateBlockPrefab<AttackBlock>(
                "AttackBlock",
                "Assets/Prefabs/Blocks/AttackBlock.prefab",
                healthBarPrefab,
                lineMaterial);
            CoreBlock corePrefab = CreateBlockPrefab<CoreBlock>(
                "CoreBlock",
                "Assets/Prefabs/Blocks/CoreBlock.prefab",
                healthBarPrefab,
                lineMaterial);
            HealerBlock healerPrefab = CreateBlockPrefab<HealerBlock>(
                "HealerBlock",
                "Assets/Prefabs/Blocks/HealerBlock.prefab",
                healthBarPrefab,
                lineMaterial);
            SupportBlock supportPrefab = CreateBlockPrefab<SupportBlock>(
                "SupportBlock",
                "Assets/Prefabs/Blocks/SupportBlock.prefab",
                healthBarPrefab,
                lineMaterial);
            WallBlock wallPrefab = CreateBlockPrefab<WallBlock>(
                "WallBlock",
                "Assets/Prefabs/Blocks/WallBlock.prefab",
                healthBarPrefab,
                lineMaterial);

            MeleeEnemy meleePrefab = CreateEnemyPrefab<MeleeEnemy>(
                "MeleeEnemy",
                "Assets/Prefabs/Enemies/MeleeEnemy.prefab");
            RangedEnemy rangedPrefab = CreateEnemyPrefab<RangedEnemy>(
                "RangedEnemy",
                "Assets/Prefabs/Enemies/RangedEnemy.prefab");

            AssignPrefab("Assets/Resources/Data/Block/AttackData.asset", attackPrefab);
            AssignPrefab("Assets/Resources/Data/Block/CoreData.asset", corePrefab);
            AssignPrefab("Assets/Resources/Data/Block/HealerData.asset", healerPrefab);
            AssignPrefab("Assets/Resources/Data/Block/SupportData.asset", supportPrefab);
            AssignPrefab("Assets/Resources/Data/Block/WallData.asset", wallPrefab);
            AssignPrefab("Assets/Resources/Data/Enemy/MeleeEnemyData.asset", meleePrefab);
            AssignPrefab("Assets/Resources/Data/Enemy/RangedEnemyData.asset", rangedPrefab);

            RangedEnemyData rangedData = AssetDatabase.LoadAssetAtPath<RangedEnemyData>(
                "Assets/Resources/Data/Enemy/RangedEnemyData.asset");
            SetReference(rangedData, "projectilePrefab", missilePrefab);
            ConfigureScene(gridLinePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Data migration and prefab setup complete.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Sprites");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Blocks");
            EnsureFolder("Assets/Prefabs/Enemies");
            EnsureFolder("Assets/Prefabs/Combat");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureFolder("Assets/Prefabs/Grid");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite EnsureWhiteSprite()
        {
            if (!File.Exists(WhiteSpritePath))
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                File.WriteAllBytes(WhiteSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(WhiteSpritePath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(WhiteSpritePath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }

        private static Material EnsureLineMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "Sprite Line" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static BlockHealthBar CreateHealthBarPrefab(Sprite sprite)
        {
            GameObject root = new GameObject("BlockHealthBar");
            BlockHealthBar healthBar = root.AddComponent<BlockHealthBar>();
            GameObject visualRoot = new GameObject("Visual Root");
            visualRoot.transform.SetParent(root.transform, false);
            SpriteRenderer background = CreateSpritePart("Background", visualRoot.transform, sprite, 10);
            SpriteRenderer fill = CreateSpritePart("Fill", visualRoot.transform, sprite, 11);

            SerializedObject serialized = new SerializedObject(healthBar);
            serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("backgroundRenderer").objectReferenceValue = background;
            serialized.FindProperty("fillRenderer").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HealthBarPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<BlockHealthBar>();
        }

        private static SpriteRenderer CreateSpritePart(
            string name,
            Transform parent,
            Sprite sprite,
            int sortingOrder)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static T CreateBlockPrefab<T>(
            string name,
            string path,
            BlockHealthBar healthBarPrefab,
            Material lineMaterial) where T : Block
        {
            GameObject root = new GameObject(name);
            T block = root.AddComponent<T>();
            DamageFeedback feedback = root.AddComponent<DamageFeedback>();
            SpriteRenderer visual = CreateSpritePart("Visual", root.transform, null, 1);

            SerializedObject serialized = new SerializedObject(block);
            serialized.FindProperty("visualRenderer").objectReferenceValue = visual;
            serialized.FindProperty("damageFeedback").objectReferenceValue = feedback;
            serialized.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;

            if (block is AttackBlock)
            {
                GameObject laserObject = new GameObject("Laser");
                laserObject.transform.SetParent(root.transform, false);
                LineRenderer laser = laserObject.AddComponent<LineRenderer>();
                ConfigureLine(laser, lineMaterial, 0.08f, 0.035f, 12);
                laser.numCapVertices = 4;
                laser.enabled = false;
                serialized.FindProperty("laser").objectReferenceValue = laser;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        private static T CreateEnemyPrefab<T>(string name, string path) where T : Enemy
        {
            GameObject root = new GameObject(name);
            root.transform.localScale = Vector3.one * 0.45f;
            T enemy = root.AddComponent<T>();
            DamageFeedback feedback = root.AddComponent<DamageFeedback>();
            SpriteRenderer visual = CreateSpritePart("Visual", root.transform, null, 2);
            Animator animator = visual.gameObject.AddComponent<Animator>();

            SerializedObject serialized = new SerializedObject(enemy);
            serialized.FindProperty("visualRenderer").objectReferenceValue = visual;
            serialized.FindProperty("damageFeedback").objectReferenceValue = feedback;
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<T>();
        }

        private static MissileProjectile CreateMissilePrefab(Material lineMaterial)
        {
            GameObject root = new GameObject("MissileProjectile");
            MissileProjectile missile = root.AddComponent<MissileProjectile>();
            LineRenderer trail = root.AddComponent<LineRenderer>();
            ConfigureLine(trail, lineMaterial, 0.04f, 0.12f, 15);
            trail.numCapVertices = 4;
            SetReference(missile, "trail", trail);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MissilePath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MissileProjectile>();
        }

        private static LineRenderer CreateGridLinePrefab(Material lineMaterial)
        {
            GameObject root = new GameObject("GridLine");
            LineRenderer line = root.AddComponent<LineRenderer>();
            ConfigureLine(line, lineMaterial, 0.03f, 0.03f, -1);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, GridLinePath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LineRenderer>();
        }

        private static void ConfigureLine(
            LineRenderer line,
            Material material,
            float startWidth,
            float endWidth,
            int sortingOrder)
        {
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.sortingOrder = sortingOrder;
        }

        private static void AssignPrefab<T>(string dataPath, T prefab) where T : Object
        {
            Object data = AssetDatabase.LoadMainAssetAtPath(dataPath);
            SetReference(data, "prefab", prefab);
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            if (target == null)
                return;

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"{target.name} has no serialized property named {propertyName}.", target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureScene(LineRenderer gridLinePrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GridVisualizer visualizer = Object.FindFirstObjectByType<GridVisualizer>(FindObjectsInactive.Include);
            SetReference(visualizer, "linePrefab", gridLinePrefab);

            WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
            if (waveManager != null)
            {
                SetReference(
                    waveManager,
                    "fallbackEnemyData",
                    AssetDatabase.LoadAssetAtPath<MeleeEnemyData>(
                        "Assets/Resources/Data/Enemy/MeleeEnemyData.asset"));
            }

            PlacementController placement = Object.FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
            SetReference(
                placement,
                "coreBlockData",
                AssetDatabase.LoadAssetAtPath<CoreBlockData>(
                    "Assets/Resources/Data/Block/CoreData.asset"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
