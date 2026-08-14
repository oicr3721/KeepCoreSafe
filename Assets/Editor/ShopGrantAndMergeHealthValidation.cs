using System;
using System.Collections.Generic;
using System.Reflection;
using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Data;
using UnityEditor;
using UnityEngine;

namespace KeepCoreSafe.Editor
{
    public static class ShopGrantAndMergeHealthValidation
    {
        private const string SupplyDataPath = "Assets/Resources/Data/Systems/BlockSupplyData.asset";
        private const string PurchasedBlockPath = "Assets/Resources/Data/Block/AttackData.asset";

        [MenuItem("Keep Core Safe/Validate Shop Grant And Merge Health")]
        public static void Validate()
        {
            ValidateGuaranteedSupplySlots();
            ValidateMergedHealthRatio();
            Debug.Log("SHOP_GRANT_AND_MERGE_HEALTH_VALIDATION_COMPLETE");
        }

        private static void ValidateGuaranteedSupplySlots()
        {
            BlockSupplyData supplyData = AssetDatabase.LoadAssetAtPath<BlockSupplyData>(SupplyDataPath);
            BlockData purchasedBlock = AssetDatabase.LoadAssetAtPath<BlockData>(PurchasedBlockPath);
            if (supplyData == null || purchasedBlock == null)
                throw new InvalidOperationException("Supply or purchased Block data is missing.");

            GameObject owner = new("Supply Validation");
            try
            {
                BlockSupplyController controller = owner.AddComponent<BlockSupplyController>();
                SerializedObject serialized = new(controller);
                serialized.FindProperty("supplyData").objectReferenceValue = supplyData;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                for (int i = 0; i < supplyData.MaximumBlocks; i++)
                {
                    if (!controller.QueueGuaranteedBlockForNextPreparation(purchasedBlock))
                        throw new InvalidOperationException("A valid guaranteed supply slot was rejected.");
                }

                if (controller.QueueGuaranteedBlockForNextPreparation(purchasedBlock))
                    throw new InvalidOperationException("Guaranteed supply exceeded the configured maximum.");

                MethodInfo dealBlocks = typeof(BlockSupplyController).GetMethod(
                    "DealBlocks",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (dealBlocks == null)
                    throw new InvalidOperationException("BlockSupplyController.DealBlocks was not found.");
                dealBlocks.Invoke(controller, null);

                if (controller.GrantedBlocks.Count != supplyData.MaximumBlocks)
                    throw new InvalidOperationException("The final grant count did not respect the configured maximum.");
                foreach (BlockSupplyController.GrantedBlock granted in controller.GrantedBlocks)
                {
                    if (granted.Data != purchasedBlock)
                        throw new InvalidOperationException("A guaranteed purchased Block did not occupy its grant slot.");
                }

                for (int iteration = 0; iteration < 8; iteration++)
                {
                    if (!controller.QueueGuaranteedBlockForNextPreparation(purchasedBlock))
                        throw new InvalidOperationException("A purchased Block could not reserve the next grant.");
                    dealBlocks.Invoke(controller, null);

                    if (controller.GrantedBlocks.Count < supplyData.MinimumBlocks
                        || controller.GrantedBlocks.Count > supplyData.MaximumBlocks)
                    {
                        throw new InvalidOperationException("A mixed grant fell outside its configured count range.");
                    }

                    bool containsPurchasedBlock = false;
                    foreach (BlockSupplyController.GrantedBlock granted in controller.GrantedBlocks)
                        containsPurchasedBlock |= granted.Data == purchasedBlock;
                    if (!containsPurchasedBlock)
                        throw new InvalidOperationException("A mixed grant lost its guaranteed purchased Block.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static void ValidateMergedHealthRatio()
        {
            List<Block> sources = new();
            float[] ratios = { 1f, 0.6f, 0.8f };
            try
            {
                foreach (float ratio in ratios)
                {
                    GameObject sourceObject = new("Merge Source Validation");
                    WallBlock source = sourceObject.AddComponent<WallBlock>();
                    source.HP.Initialize(ratio * 100f, 100f);
                    sources.Add(source);
                }

                MethodInfo calculator = typeof(PlacementController).GetMethod(
                    "CalculateAverageHealthRatio",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (calculator == null)
                    throw new InvalidOperationException("Merge health-ratio calculator was not found.");

                float result = (float)calculator.Invoke(null, new object[] { sources });
                if (!Mathf.Approximately(result, 0.8f))
                    throw new InvalidOperationException($"Expected merged health ratio 0.8 but received {result}.");
            }
            finally
            {
                foreach (Block source in sources)
                {
                    if (source != null)
                        UnityEngine.Object.DestroyImmediate(source.gameObject);
                }
            }
        }
    }
}
