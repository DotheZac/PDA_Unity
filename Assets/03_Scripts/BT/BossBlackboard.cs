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

        [Header("Click Hitbox")]
        [SerializeField] private bool enableClickHitbox = true;
        [SerializeField, Min(0f)] private float clickDamage = 1f;
        [SerializeField] private BoxCollider2D clickHitbox;
        [SerializeField] private bool autoCreateClickHitbox = true;
        [SerializeField] private Vector2 clickHitboxSize = new Vector2(4f, 4f);
        [SerializeField] private Vector2 clickHitboxOffset = Vector2.zero;
        [SerializeField] private bool clickHitboxIsTrigger = true;

        [Header("Hit Ready Visual")]
        [SerializeField] private SpriteRenderer hitReadySpriteRenderer;
        [SerializeField] private bool autoFindHitReadySpriteRenderer = true;
        [SerializeField] private string hitReadySpriteName = "Snipe";

        [Header("Animation Sync")]
        [SerializeField] private BossSkillAnimationSync animationSync;

        [Header("TV Laser")]
        [SerializeField] private Animator tvAnimator;
        [SerializeField] private Animator tvLaserEffectAnimator;
        [SerializeField] private string tvLaserBoolParameter = "IsLaser";
        [SerializeField] private string tvLaserFireTriggerParameter = "Fire";
        [SerializeField] private int tvLaserLayer;
        [SerializeField] private string tvLaserChargingStateName = "charging";
        [SerializeField] private string tvLaserFireStateName = "fire";
        [SerializeField] private bool hideTvWhenIdle = true;

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
        [SerializeField] private float idleCooldown = 3.0f;
        [SerializeField] private float elapsedIdleTime;
        [SerializeField] private bool canBeHit = true;

        [Header("Skill Timing")]
        [SerializeField, Min(0.01f)] private float armSmashMoveDuration = 0.7f;

        private readonly Dictionary<string, int[]> _rangeMap = new();
        private readonly float[] _skillWeights = { 1f, 1f, 1f, 1f, 1f };
        private readonly float[] _skillChances = new float[5];
        private Renderer[] _tvRenderers = Array.Empty<Renderer>();
        private Renderer[] _tvLaserRenderers = Array.Empty<Renderer>();
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
        public float ArmSmashMoveDuration => Mathf.Max(0.01f, armSmashMoveDuration);

        public int CurrentPhase => currentPhase;

        private void Awake()
        {
            EnsureClickHitbox();
            EnsureHitReadySpriteRenderer();

            _rangeMap.Clear();
            foreach (var range in ranges)
            {
                if (range == null || string.IsNullOrWhiteSpace(range.Key))
                {
                    continue;
                }

                _rangeMap[range.Key] = range.Indices;
            }

            if (tvAnimator == null)
            {
                tvAnimator = FindNamedAnimatorInChildren(transform, "TV");
            }

            if (tvLaserEffectAnimator == null)
            {
                tvLaserEffectAnimator = FindNamedAnimatorInChildren(transform, "Laser");
            }

            // Backward-compatible fallback when a dedicated laser animator is not assigned.
            if (tvLaserEffectAnimator == null && tvAnimator != null)
            {
                tvLaserEffectAnimator = tvAnimator;
            }

            CacheTvRenderers();
            CacheTvLaserRenderers();
            SetTvLaserActive(false);
            SetCanBeHit(canBeHit);
        }

        private void Reset()
        {
            EnsureClickHitbox();
        }

        private void OnValidate()
        {
            if (clickDamage < 0f)
            {
                clickDamage = 0f;
            }

            clickHitboxSize.x = Mathf.Max(0.1f, clickHitboxSize.x);
            clickHitboxSize.y = Mathf.Max(0.1f, clickHitboxSize.y);

            SyncClickHitboxSettingsFromCollider();
            EnsureHitReadySpriteRenderer();
            SetHitReadyVisible(canBeHit);
        }

        private void OnMouseDown()
        {
            TryApplyClickDamage();
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

        public void TickAnimationSync()
        {
            animationSync?.Tick();
        }

        public bool TryPlayAttackAnimation(string skillName)
        {
            return animationSync != null && animationSync.TryPlay(skillName);
        }

        public bool HasAttackAnimationBinding(string skillName)
        {
            return animationSync != null && animationSync.HasBindingForSkill(skillName);
        }

        public bool IsAttackAnimationRunning(string skillName)
        {
            return animationSync != null && animationSync.IsRunning(skillName);
        }

        public bool HasAnyRunningAttackAnimation()
        {
            return animationSync != null && animationSync.HasAnyRunning;
        }

        public void ClearAllAttackAnimations()
        {
            animationSync?.ClearAll();
        }

        public void SetTvLaserActive(bool isActive)
        {
            if (tvAnimator == null && tvLaserEffectAnimator == null)
            {
                return;
            }

            if (tvAnimator != null && !tvAnimator.gameObject.activeSelf)
            {
                tvAnimator.gameObject.SetActive(true);
            }

            if (tvLaserEffectAnimator != null && !tvLaserEffectAnimator.gameObject.activeSelf)
            {
                tvLaserEffectAnimator.gameObject.SetActive(true);
            }

            if (HasAnimatorBoolParameter(tvAnimator, tvLaserBoolParameter))
            {
                tvAnimator.SetBool(tvLaserBoolParameter, isActive);
            }

            if (isActive && tvLaserEffectAnimator != null)
            {
                if (!PlayAnimatorState(tvLaserEffectAnimator, tvLaserChargingStateName))
                {
                    // If a charge state name is mismatched in the inspector, fall back to controller default.
                    tvLaserEffectAnimator.Rebind();
                    tvLaserEffectAnimator.Update(0f);
                }
            }

            ApplyTvLaserEffectVisibility(isActive);
        }

        public void TriggerTvLaserFire()
        {
            var laserAnimator = tvLaserEffectAnimator != null ? tvLaserEffectAnimator : tvAnimator;
            if (laserAnimator == null)
            {
                return;
            }

            if (HasAnimatorTriggerParameter(laserAnimator, tvLaserFireTriggerParameter))
            {
                laserAnimator.ResetTrigger(tvLaserFireTriggerParameter);
                laserAnimator.SetTrigger(tvLaserFireTriggerParameter);
                return;
            }

            // Fallback when controller has no trigger parameter.
            PlayAnimatorState(laserAnimator, tvLaserFireStateName);
        }

        private void CacheTvRenderers()
        {
            if (tvAnimator == null)
            {
                _tvRenderers = Array.Empty<Renderer>();
                return;
            }

            _tvRenderers = tvAnimator.GetComponentsInChildren<Renderer>(true);
        }

        private void CacheTvLaserRenderers()
        {
            if (tvLaserEffectAnimator == null)
            {
                _tvLaserRenderers = Array.Empty<Renderer>();
                return;
            }

            _tvLaserRenderers = tvLaserEffectAnimator.GetComponentsInChildren<Renderer>(true);
        }

        private void ApplyTvLaserEffectVisibility(bool isVisible)
        {
            if (!hideTvWhenIdle)
            {
                return;
            }

            if (_tvLaserRenderers == null || _tvLaserRenderers.Length == 0)
            {
                CacheTvLaserRenderers();
            }

            for (var i = 0; i < _tvLaserRenderers.Length; i++)
            {
                var renderer = _tvLaserRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = isVisible;
            }
        }

        private bool PlayAnimatorState(Animator animator, string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            var layer = Mathf.Clamp(tvLaserLayer, 0, Mathf.Max(0, animator.layerCount - 1));
            if (!animator.HasState(layer, Animator.StringToHash(stateName)))
            {
                return false;
            }

            animator.Play(stateName, layer, 0f);
            animator.Update(0f);
            return true;
        }

        private static bool HasAnimatorBoolParameter(Animator animator, string paramName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                {
                    return true;
                }
            }

            return false;
        }

        private static Animator FindNamedAnimatorInChildren(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            var animators = root.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (string.Equals(animator.gameObject.name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return animator;
                }
            }

            return null;
        }

        private static SpriteRenderer FindNamedSpriteRendererInChildren(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (string.Equals(renderer.gameObject.name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return renderer;
                }
            }

            return null;
        }

        private static bool HasAnimatorTriggerParameter(Animator animator, string paramName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
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

        public bool TryApplyClickDamage()
        {
            if (!enableClickHitbox || clickDamage <= 0f || !canBeHit)
            {
                return false;
            }

            QueueDamage(clickDamage);
            return ProcessPendingHit();
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
                SetCanBeHit(true);
                ProcessPendingHit();
                return false;
            }

            SetCanBeHit(false);
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

        private void EnsureClickHitbox()
        {
            if (!enableClickHitbox)
            {
                return;
            }

            if (clickHitbox == null)
            {
                clickHitbox = GetComponent<BoxCollider2D>();
            }

            if (clickHitbox == null && autoCreateClickHitbox && !Application.isPlaying)
            {
                clickHitbox = gameObject.AddComponent<BoxCollider2D>();
                ApplyClickHitboxSettings();
            }
        }

        private void ApplyClickHitboxSettings()
        {
            if (clickHitbox == null)
            {
                return;
            }

            clickHitbox.size = new Vector2(
                Mathf.Max(0.1f, clickHitboxSize.x),
                Mathf.Max(0.1f, clickHitboxSize.y));
            clickHitbox.offset = clickHitboxOffset;
            clickHitbox.isTrigger = clickHitboxIsTrigger;
        }

        private void SyncClickHitboxSettingsFromCollider()
        {
            if (clickHitbox == null)
            {
                return;
            }

            clickHitboxSize = clickHitbox.size;
            clickHitboxOffset = clickHitbox.offset;
            clickHitboxIsTrigger = clickHitbox.isTrigger;
        }

        private void EnsureHitReadySpriteRenderer()
        {
            if (hitReadySpriteRenderer != null)
            {
                return;
            }

            if (!autoFindHitReadySpriteRenderer)
            {
                return;
            }

            hitReadySpriteRenderer = FindNamedSpriteRendererInChildren(transform, hitReadySpriteName);
        }

        private void SetCanBeHit(bool value)
        {
            canBeHit = value;
            SetHitReadyVisible(canBeHit);
        }

        private void SetHitReadyVisible(bool visible)
        {
            if (hitReadySpriteRenderer == null)
            {
                return;
            }

            hitReadySpriteRenderer.enabled = visible;
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
