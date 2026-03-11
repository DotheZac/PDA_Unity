using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public class BossBlackboard : MonoBehaviour
    {
        [Header("Telegraph")]
        [SerializeField] private TelegraphTile[] telegraphs;

        [Header("Pattern Range")]
        [SerializeField] private List<PatternRange> ranges;

        private readonly Dictionary<string, int[]> _rangeMap = new();

        private void Awake()
        {
            _rangeMap.Clear();
            foreach (var range in ranges)
            {
                if (range == null || string.IsNullOrWhiteSpace(range.Key))
                {
                    continue;
                }

                _rangeMap[range.Key] = range.Indices;
            }
        }

        public TelegraphTile[] Telegraphs => telegraphs;

        public bool TryGetRange(string key, out int[] indices)
        {
            return _rangeMap.TryGetValue(key, out indices);
        }

        [ContextMenu("Auto Collect Telegraph Tiles (Children)")]
        public void AutoCollectTelegraphsFromChildren()
        {
            telegraphs = GetComponentsInChildren<TelegraphTile>(true);
            Debug.Log($"[BossBlackboard] Auto collected {telegraphs.Length} telegraph tiles.", this);
        }

        public void LogBindingSummary(IReadOnlyList<string> requiredRangeKeys)
        {
            var telegraphCount = telegraphs == null ? 0 : telegraphs.Length;
            Debug.Log($"[BossBlackboard] Telegraph count: {telegraphCount}", this);

            if (requiredRangeKeys == null)
            {
                return;
            }

            foreach (var key in requiredRangeKeys)
            {
                if (!TryGetRange(key, out var indices) || indices == null || indices.Length == 0)
                {
                    Debug.LogWarning($"[BossBlackboard] Missing or empty range key: {key}", this);
                    continue;
                }

                var valid = 0;
                var invalid = 0;
                foreach (var index in indices)
                {
                    if (index >= 0 && index < telegraphCount)
                    {
                        valid++;
                    }
                    else
                    {
                        invalid++;
                    }
                }

                Debug.Log($"[BossBlackboard] Range '{key}': total={indices.Length}, valid={valid}, invalid={invalid}", this);
            }
        }

        [Serializable]
        public class PatternRange
        {
            public string Key;
            public int[] Indices;
        }
    }
}
