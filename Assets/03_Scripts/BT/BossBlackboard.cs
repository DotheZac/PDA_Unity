using System;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public enum PatternExecutionMode
    {
        Normal,
        WarningOnly,
        AttackOnly
    }

    public class BossBlackboard : MonoBehaviour
    {
        [Header("Telegraph")]
        [SerializeField] private TelegraphTile[] telegraphs;

        [Header("Pattern Range")]
        [SerializeField] private List<PatternRange> ranges;

        [Header("Phase")]
        [SerializeField] private float bossCurrentHp = 100f;
        [SerializeField] private int currentPhase = 0;
        [SerializeField] private bool phase3Triggered;
        [SerializeField] private float phase2Threshold = 50f;
        [SerializeField] private float phase2To3Delay = 20f;
        [SerializeField] private float phase2ElapsedTime;

        [Header("Pattern Cooldown")]
        [SerializeField] private float idleCooldown = 1.0f;
        [SerializeField] private float elapsedIdleTime;
        [SerializeField] private bool canBeHit = true;

        private readonly Dictionary<string, int[]> _rangeMap = new();
        private readonly float[] _skillWeights = { 1f, 1f, 1f, 1f, 1f };
        private readonly float[] _skillChances = new float[5];
        private float _randomValue;
        private float _pendingDamage;
        private PatternExecutionMode _executionMode = PatternExecutionMode.Normal;

        public TelegraphTile[] Telegraphs => telegraphs;
        public float BossCurrentHp { get => bossCurrentHp; set => bossCurrentHp = value; }
        public bool CanBeHit => canBeHit;
        public bool IsPhase3Triggered => phase3Triggered;
        public float Phase2Threshold => phase2Threshold;
        public float Phase2To3Delay => phase2To3Delay;
        public float Phase2ElapsedTime => phase2ElapsedTime;
        public PatternExecutionMode ExecutionMode => _executionMode;

        public int CurrentPhase => currentPhase;

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

        public void SetPatternExecutionMode(PatternExecutionMode mode)
        {
            _executionMode = mode;
        }

        public void SetCurrentPhase(int phase)
        {
            if (currentPhase == phase)
            {
                return;
            }

            currentPhase = phase;
            if (phase == 2)
            {
                phase2ElapsedTime = 0f;
                phase3Triggered = false;
                return;
            }

            if (phase == 1)
            {
                phase2ElapsedTime = 0f;
                phase3Triggered = false;
            }
        }

        public void SetPhase3Triggered(bool triggered)
        {
            phase3Triggered = triggered;
        }

        public void TickPhaseTimer(float deltaTime)
        {
            if (currentPhase != 2 || phase3Triggered)
            {
                return;
            }

            phase2ElapsedTime += deltaTime;
            if (phase2ElapsedTime >= phase2To3Delay)
            {
                phase3Triggered = true;
            }
        }

        public bool TryGetRange(string key, out int[] indices)
        {
            return _rangeMap.TryGetValue(key, out indices);
        }

        public void ClearAllWarnings()
        {
            if (telegraphs == null)
            {
                return;
            }

            foreach (var tile in telegraphs)
            {
                if (tile == null)
                {
                    continue;
                }

                tile.SetWarningVisible(false);
            }
        }

        public void ClearAllDamage()
        {
            if (telegraphs == null)
            {
                return;
            }

            foreach (var tile in telegraphs)
            {
                if (tile == null)
                {
                    continue;
                }

                tile.SetDamageActive(false);
            }
        }

        public bool HasAnyActiveTelegraphState()
        {
            if (telegraphs == null)
            {
                return false;
            }

            foreach (var tile in telegraphs)
            {
                if (tile == null)
                {
                    continue;
                }

                if (tile.IsWarningVisible || tile.IsDamageActive)
                {
                    return true;
                }
            }

            return false;
        }

        public void QueueDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            _pendingDamage += damage;
        }

        public bool ProcessPendingHit()
        {
            if (!canBeHit)
            {
                _pendingDamage = 0f;
                return false;
            }

            if (_pendingDamage <= 0f)
            {
                return false;
            }

            bossCurrentHp = Mathf.Max(0f, bossCurrentHp - _pendingDamage);
            _pendingDamage = 0f;
            return true;
        }

        public bool TickCooldownAndPreparePattern(float deltaTime, int skillCount)
        {
            elapsedIdleTime += deltaTime;
            if (elapsedIdleTime < idleCooldown)
            {
                canBeHit = true;
                ProcessPendingHit();
                return false;
            }

            canBeHit = false;
            elapsedIdleTime = 0f;
            BuildSkillChanceTable(skillCount);
            _randomValue = UnityEngine.Random.Range(0f, 1f);
            return true;
        }

        public bool TryConsumeSkillChance(int skillIndex, int skillCount)
        {
            if (skillIndex < 1 || skillIndex > skillCount || skillCount < 1 || skillCount > _skillChances.Length)
            {
                return false;
            }

            return _randomValue < _skillChances[skillIndex - 1];
        }

        public void OnSkillSelected(int skillIndex, int skillCount)
        {
            if (skillIndex < 1 || skillIndex > skillCount || skillCount < 1 || skillCount > _skillWeights.Length)
            {
                return;
            }

            _skillWeights[skillIndex - 1] = 1f;

            for (var i = 0; i < skillCount; i++)
            {
                if (i == skillIndex - 1)
                {
                    continue;
                }

                _skillWeights[i] += 1f;
            }

            for (var i = skillCount; i < _skillWeights.Length; i++)
            {
                _skillWeights[i] = 1f;
                _skillChances[i] = 0f;
            }
        }

        private void BuildSkillChanceTable(int skillCount)
        {
            var sum = 0f;
            for (var i = 0; i < skillCount; i++)
            {
                sum += _skillWeights[i];
            }

            if (sum <= Mathf.Epsilon)
            {
                for (var i = 0; i < skillCount; i++)
                {
                    _skillWeights[i] = 1f;
                }

                sum = skillCount;
            }

            var cumulative = 0f;
            for (var i = 0; i < skillCount; i++)
            {
                cumulative += _skillWeights[i] / sum;
                _skillChances[i] = cumulative;
            }

            for (var i = skillCount; i < _skillChances.Length; i++)
            {
                _skillChances[i] = 0f;
            }
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
