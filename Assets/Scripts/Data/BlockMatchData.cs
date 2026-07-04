using System;
using System.Collections.Generic;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BlockMatchData", menuName = "Keep Core Safe/Block System/Match Rules")]
    public sealed class BlockMatchData : ScriptableObject
    {
        [Serializable]
        public sealed class Rule
        {
            [SerializeField] private BlockColorData sourceColor;
            [SerializeField] private BlockData resultBlock;
            [SerializeField, Min(2)] private int requiredCount = 3;

            public BlockColorData SourceColor => sourceColor;
            public BlockData ResultBlock => resultBlock;
            public int RequiredCount => requiredCount;
        }

        [SerializeField] private List<Rule> rules = new();

        public IReadOnlyList<Rule> Rules => rules;

        public bool TryGetRule(BlockColorData color, out Rule rule)
        {
            rule = rules.Find(candidate => candidate.SourceColor == color);
            return rule != null && rule.ResultBlock != null;
        }
    }
}
