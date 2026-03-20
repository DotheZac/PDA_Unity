using System;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public class TelegraphTile : MonoBehaviour
    {
        public bool IsWarningVisible
            => (warningRenderer != null && warningRenderer.enabled)
               || (warningEffect != null && warningEffect.enabled);

        public bool IsDamageActive
            => damageCollider != null && damageCollider.enabled;

        [Header("Damage")]
        [SerializeField] private int damagePerHit = 1;
        [SerializeField] private float hitInterval = 0.35f;

        [SerializeField] private Collider2D damageCollider;
        [SerializeField] private Animator damageEffectAnimator;
        [SerializeField] private string damageEffectStateName = "Fire_Ground_Ani";
        [SerializeField] private int damageEffectLayer;
        [SerializeField] private bool hideDamageEffectWhenIdle = true;
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private Behaviour warningEffect;
        [SerializeField] private Color warningColor = Color.white;
        [SerializeField] private Color damageActiveColor = Color.blue;

        private bool _warningVisibleRequested;
        private bool _damageActiveRequested;
        private readonly Dictionary<int, float> _nextHitTimeByTarget = new();
        private Renderer[] _damageEffectRenderers = Array.Empty<Renderer>();
        private bool _laserDamageEffectRequested;
        private bool _damageEffectStopRequested;
        private int _damageEffectStopTargetLoop = 1;

        private void Awake()
        {
            AutoBindComponents();
            if (warningRenderer != null)
            {
                warningColor = warningRenderer.color;
            }

            _warningVisibleRequested = false;
            _damageActiveRequested = false;
            _laserDamageEffectRequested = false;
            ApplyVisualState();
            SetDamageCollider(false);
            CacheDamageEffectRenderers();
            ForceStopDamageEffect();
        }

        private void OnEnable()
        {
            _warningVisibleRequested = false;
            _damageActiveRequested = false;
            _laserDamageEffectRequested = false;
            _nextHitTimeByTarget.Clear();
            ApplyVisualState();
            SetDamageCollider(false);
            CacheDamageEffectRenderers();
            ForceStopDamageEffect();
        }

        private void OnDisable()
        {
            _nextHitTimeByTarget.Clear();
            ForceStopDamageEffect();
        }

        private void Update()
        {
            TickDamageEffectStop();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryApplyDamage(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var status = ResolvePlayerStatus(other);
            if (status == null)
            {
                return;
            }

            _nextHitTimeByTarget.Remove(status.GetInstanceID());
        }

        private void Reset()
        {
            AutoBindComponents();
        }

        private void OnValidate()
        {
            AutoBindComponents();
            if (damagePerHit < 0)
            {
                damagePerHit = 0;
            }

            if (hitInterval < 0f)
            {
                hitInterval = 0f;
            }

            if (damageEffectLayer < 0)
            {
                damageEffectLayer = 0;
            }
        }

        [ContextMenu("Auto Bind Components")]
        public void AutoBindComponents()
        {
            if (damageCollider == null)
            {
                damageCollider = GetComponent<Collider2D>();
                if (damageCollider == null)
                {
                    damageCollider = GetComponentInChildren<Collider2D>(true);
                }
            }

            if (warningRenderer == null)
            {
                warningRenderer = GetComponent<SpriteRenderer>();
                if (warningRenderer == null)
                {
                    warningRenderer = GetComponentInChildren<SpriteRenderer>(true);
                }
            }

            if (warningEffect == null)
            {
                warningEffect = GetComponent<SpriteBlink>();
                if (warningEffect == null)
                {
                    warningEffect = GetComponentInChildren<SpriteBlink>(true);
                }
            }

            if (damageEffectAnimator == null)
            {
                var animators = GetComponentsInChildren<Animator>(true);
                for (var i = 0; i < animators.Length; i++)
                {
                    var animator = animators[i];
                    if (animator == null)
                    {
                        continue;
                    }

                    if (string.Equals(animator.gameObject.name, "Fire_Ground", StringComparison.OrdinalIgnoreCase)
                        || animator.gameObject.name.IndexOf("Fire_Ground", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        damageEffectAnimator = animator;
                        break;
                    }
                }
            }
        }

        public void SetWarningVisible(bool visible)
        {
            _warningVisibleRequested = visible;
            ApplyVisualState();
        }

        public void SetDamageActive(bool active)
        {
            _damageActiveRequested = active;
            SetDamageCollider(active);
            ApplyVisualState();

            if (!active)
            {
                _nextHitTimeByTarget.Clear();
            }
        }

        public void SetLaserDamageEffectActive(bool active)
        {
            if (_laserDamageEffectRequested == active)
            {
                return;
            }

            _laserDamageEffectRequested = active;
            if (active)
            {
                SetDamageEffectActive(true, restart: true);
                return;
            }

            RequestDamageEffectStop();
        }

        private void SetDamageCollider(bool active)
        {
            if (damageCollider == null)
            {
                return;
            }

            damageCollider.enabled = active;
        }

        private void CacheDamageEffectRenderers()
        {
            if (damageEffectAnimator == null)
            {
                _damageEffectRenderers = Array.Empty<Renderer>();
                return;
            }

            _damageEffectRenderers = damageEffectAnimator.GetComponentsInChildren<Renderer>(true);
        }

        private void SetDamageEffectActive(bool active, bool restart)
        {
            if (damageEffectAnimator == null)
            {
                return;
            }

            if (!active)
            {
                ForceStopDamageEffect();
                return;
            }

            _damageEffectStopRequested = false;
            _damageEffectStopTargetLoop = 1;

            if (!damageEffectAnimator.gameObject.activeSelf)
            {
                damageEffectAnimator.gameObject.SetActive(true);
            }

            if (!damageEffectAnimator.enabled)
            {
                damageEffectAnimator.enabled = true;
            }

            if (!restart)
            {
                return;
            }

            if (!TryPlayDamageEffectState())
            {
                damageEffectAnimator.Rebind();
                damageEffectAnimator.Update(0f);
            }

            if (hideDamageEffectWhenIdle)
            {
                SetDamageEffectVisible(true);
            }
        }

        private void RequestDamageEffectStop()
        {
            if (damageEffectAnimator == null)
            {
                return;
            }

            if (!damageEffectAnimator.isActiveAndEnabled || !damageEffectAnimator.gameObject.activeInHierarchy)
            {
                ForceStopDamageEffect();
                return;
            }

            var layer = Mathf.Clamp(damageEffectLayer, 0, Mathf.Max(0, damageEffectAnimator.layerCount - 1));
            var info = damageEffectAnimator.GetCurrentAnimatorStateInfo(layer);
            _damageEffectStopTargetLoop = info.loop ? Mathf.FloorToInt(info.normalizedTime) + 1 : 1;
            _damageEffectStopRequested = true;
        }

        private void TickDamageEffectStop()
        {
            if (!_damageEffectStopRequested || damageEffectAnimator == null)
            {
                return;
            }

            if (IsDamageEffectFinishedCurrentCycle())
            {
                ForceStopDamageEffect();
            }
        }

        private bool IsDamageEffectFinishedCurrentCycle()
        {
            if (damageEffectAnimator == null)
            {
                return true;
            }

            if (!damageEffectAnimator.isActiveAndEnabled || !damageEffectAnimator.gameObject.activeInHierarchy)
            {
                return true;
            }

            var layer = Mathf.Clamp(damageEffectLayer, 0, Mathf.Max(0, damageEffectAnimator.layerCount - 1));
            if (damageEffectAnimator.IsInTransition(layer))
            {
                return false;
            }

            var info = damageEffectAnimator.GetCurrentAnimatorStateInfo(layer);
            if (!string.IsNullOrWhiteSpace(damageEffectStateName) && !info.IsName(damageEffectStateName))
            {
                // If another state took over, treat it as finished.
                return true;
            }

            if (info.loop)
            {
                return info.normalizedTime >= _damageEffectStopTargetLoop;
            }

            return info.normalizedTime >= 1f;
        }

        private void ForceStopDamageEffect()
        {
            _laserDamageEffectRequested = false;
            _damageEffectStopRequested = false;
            _damageEffectStopTargetLoop = 1;

            if (damageEffectAnimator == null)
            {
                return;
            }

            damageEffectAnimator.Rebind();
            damageEffectAnimator.Update(0f);

            if (hideDamageEffectWhenIdle)
            {
                SetDamageEffectVisible(false);
                damageEffectAnimator.enabled = false;
            }
        }

        private void SetDamageEffectVisible(bool visible)
        {
            if (_damageEffectRenderers == null || _damageEffectRenderers.Length == 0)
            {
                CacheDamageEffectRenderers();
            }

            for (var i = 0; i < _damageEffectRenderers.Length; i++)
            {
                var renderer = _damageEffectRenderers[i];
                if (renderer == null || renderer == warningRenderer)
                {
                    continue;
                }

                renderer.enabled = visible;
            }
        }

        private bool TryPlayDamageEffectState()
        {
            if (damageEffectAnimator == null)
            {
                return false;
            }

            var layer = Mathf.Clamp(damageEffectLayer, 0, Mathf.Max(0, damageEffectAnimator.layerCount - 1));
            if (!string.IsNullOrWhiteSpace(damageEffectStateName)
                && damageEffectAnimator.HasState(layer, Animator.StringToHash(damageEffectStateName)))
            {
                damageEffectAnimator.Play(damageEffectStateName, layer, 0f);
                damageEffectAnimator.Update(0f);
                return true;
            }

            return false;
        }

        private void TryApplyDamage(Collider2D other)
        {
            if (!IsDamageActive || damagePerHit <= 0)
            {
                return;
            }

            var status = ResolvePlayerStatus(other);
            if (status == null)
            {
                return;
            }

            var key = status.GetInstanceID();
            var now = Time.time;
            if (_nextHitTimeByTarget.TryGetValue(key, out var nextHitTime) && now < nextHitTime)
            {
                return;
            }

            if (!status.TakeDamage(damagePerHit))
            {
                return;
            }

            _nextHitTimeByTarget[key] = now + hitInterval;
        }

        private static PlayerStatus ResolvePlayerStatus(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            var status = other.GetComponent<PlayerStatus>();
            if (status != null)
            {
                return status;
            }

            return other.GetComponentInParent<PlayerStatus>();
        }

        private void ApplyVisualState()
        {
            if (warningRenderer != null)
            {
                warningRenderer.enabled = _warningVisibleRequested || _damageActiveRequested;

                var current = warningRenderer.color;
                var target = _damageActiveRequested ? damageActiveColor : warningColor;
                target.a = current.a;
                warningRenderer.color = target;
            }

            if (warningEffect != null)
            {
                warningEffect.enabled = _warningVisibleRequested && !_damageActiveRequested;
            }
        }
    }
}
