using System;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    [DisallowMultipleComponent]
    public sealed class BossSkillAnimationSync : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [Tooltip("SkillNode.Name contains this key to match the binding.")]
            public string skillKeyContains;

            [Tooltip("Animation object pool for this skill type. Assign all prepared Animator objects.")]
            public List<Animator> animators = new();

            [Tooltip("State name to play when the attack starts.")]
            public string attackStateName = "Attack";

            [Min(0)]
            public int layer;

            [Range(0.1f, 1f)]
            public float endNormalizedTime = 0.98f;

            [Tooltip("Hide animator visuals when not running and show only while this skill animation plays.")]
            public bool hideWhenIdle = true;
        }

        private sealed class RunningEntry
        {
            public Binding Binding;
            public Animator Animator;
        }

        [SerializeField] private List<Binding> bindings = new();

        private readonly Dictionary<string, RunningEntry> _running = new();
        private readonly Dictionary<Animator, Renderer[]> _rendererCache = new();

        private void Awake()
        {
            HideAllBoundAnimators();
        }

        private void OnDisable()
        {
            ClearAll();
            HideAllBoundAnimators();
        }

        public bool HasAnyRunning => _running.Count > 0;

        public bool HasBindingForSkill(string skillName)
        {
            return FindBinding(skillName) != null;
        }

        public bool TryPlay(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return false;
            }

            if (_running.ContainsKey(skillName))
            {
                return true;
            }

            var binding = FindBinding(skillName);
            if (binding == null)
            {
                Debug.LogWarning($"[BossSkillAnimationSync] No binding matched skill '{skillName}'.", this);
                return false;
            }

            var animator = FindAvailableAnimator(binding, skillName);
            if (animator == null)
            {
                Debug.LogWarning($"[BossSkillAnimationSync] No available animator for skill '{skillName}' in binding '{binding.skillKeyContains}'.", this);
                return false;
            }

            PrepareAnimatorForPlay(binding, animator);
            var layer = SanitizeLayer(binding, animator);
            animator.Play(binding.attackStateName, layer, 0f);
            animator.Update(0f);
            Debug.Log($"[BossSkillAnimationSync] Play skill '{skillName}' with animator '{animator.name}' (state='{binding.attackStateName}', layer={layer}).", this);
            _running[skillName] = new RunningEntry
            {
                Binding = binding,
                Animator = animator
            };

            return true;
        }

        public bool IsRunning(string skillName)
        {
            if (!_running.TryGetValue(skillName, out var entry))
            {
                return false;
            }

            if (!IsStillRunning(entry.Binding, entry.Animator))
            {
                StopEntry(entry);
                _running.Remove(skillName);
                return false;
            }

            return true;
        }

        public void Tick()
        {
            if (_running.Count == 0)
            {
                return;
            }

            var keys = new List<string>(_running.Keys);
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (!_running.TryGetValue(key, out var entry))
                {
                    continue;
                }

                if (!IsStillRunning(entry.Binding, entry.Animator))
                {
                    StopEntry(entry);
                    _running.Remove(key);
                }
            }
        }

        public void ClearAll()
        {
            foreach (var pair in _running)
            {
                StopEntry(pair.Value);
            }

            _running.Clear();
        }

        private Binding FindBinding(string skillName)
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b == null || string.IsNullOrWhiteSpace(b.skillKeyContains))
                {
                    continue;
                }

                if (skillName.IndexOf(b.skillKeyContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return b;
                }
            }

            return null;
        }

        private Animator FindAvailableAnimator(Binding binding, string skillName)
        {
            if (binding.animators == null || binding.animators.Count == 0)
            {
                return null;
            }

            // First pass: pick animator explicitly matched to the current skill name.
            for (var i = 0; i < binding.animators.Count; i++)
            {
                var animator = binding.animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (!IsPreferredAnimatorForSkill(animator, skillName))
                {
                    continue;
                }

                if (IsAnimatorReserved(animator))
                {
                    continue;
                }

                if (!IsStillRunning(binding, animator))
                {
                    return animator;
                }
            }

            // Fallback pass: pick any available animator from the pool.
            for (var i = 0; i < binding.animators.Count; i++)
            {
                var animator = binding.animators[i];
                if (animator == null)
                {
                    continue;
                }

                if (IsAnimatorReserved(animator))
                {
                    continue;
                }

                if (!IsStillRunning(binding, animator))
                {
                    return animator;
                }
            }

            return null;
        }

        private static bool IsPreferredAnimatorForSkill(Animator animator, string skillName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(skillName))
            {
                return false;
            }

            return animator.name.IndexOf(skillName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsAnimatorReserved(Animator animator)
        {
            foreach (var pair in _running)
            {
                if (pair.Value.Animator == animator)
                {
                    return true;
                }
            }

            return false;
        }

        private void StopEntry(RunningEntry entry)
        {
            if (entry == null || entry.Animator == null)
            {
                return;
            }

            if (entry.Binding != null && entry.Binding.hideWhenIdle)
            {
                SetAnimatorVisible(entry.Animator, false);
                entry.Animator.enabled = false;
            }
        }

        private void HideAllBoundAnimators()
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.hideWhenIdle || binding.animators == null)
                {
                    continue;
                }

                for (var j = 0; j < binding.animators.Count; j++)
                {
                    var animator = binding.animators[j];
                    if (animator == null)
                    {
                        continue;
                    }

                    SetAnimatorVisible(animator, false);
                    animator.enabled = false;
                }
            }
        }

        private void PrepareAnimatorForPlay(Binding binding, Animator animator)
        {
            if (binding == null || animator == null)
            {
                return;
            }

            if (!animator.gameObject.activeSelf)
            {
                animator.gameObject.SetActive(true);
            }

            if (binding.hideWhenIdle)
            {
                SetAnimatorVisible(animator, true);
                animator.enabled = true;
            }

            // Ensure clip time and bindings are reset before playing from 0.
            animator.Rebind();
            animator.Update(0f);
        }

        private void SetAnimatorVisible(Animator animator, bool visible)
        {
            if (animator == null)
            {
                return;
            }

            var renderers = GetCachedRenderers(animator);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }

        private Renderer[] GetCachedRenderers(Animator animator)
        {
            if (animator == null)
            {
                return Array.Empty<Renderer>();
            }

            if (_rendererCache.TryGetValue(animator, out var cached) && cached != null)
            {
                return cached;
            }

            var renderers = animator.GetComponentsInChildren<Renderer>(true);
            _rendererCache[animator] = renderers;
            return renderers;
        }

        private static bool IsStillRunning(Binding binding, Animator animator)
        {
            if (binding == null || animator == null || string.IsNullOrWhiteSpace(binding.attackStateName))
            {
                return false;
            }

            // Hidden/disabled animator should be treated as idle and available.
            if (!animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
            {
                return false;
            }

            var layer = SanitizeLayer(binding, animator);
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (!info.IsName(binding.attackStateName))
            {
                return false;
            }

            if (animator.IsInTransition(layer))
            {
                return true;
            }

            return info.normalizedTime < binding.endNormalizedTime;
        }

        private static int SanitizeLayer(Binding binding, Animator animator)
        {
            if (animator == null)
            {
                return 0;
            }

            var maxLayer = Mathf.Max(0, animator.layerCount - 1);
            var configured = binding == null ? 0 : binding.layer;
            return Mathf.Clamp(configured, 0, maxLayer);
        }
    }
}
