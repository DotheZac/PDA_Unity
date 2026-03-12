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
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private Behaviour warningEffect;
        [SerializeField] private Color warningColor = Color.white;
        [SerializeField] private Color damageActiveColor = Color.blue;

        private bool _warningVisibleRequested;
        private bool _damageActiveRequested;
        private readonly Dictionary<int, float> _nextHitTimeByTarget = new();

        private void Awake()
        {
            AutoBindComponents();
            if (warningRenderer != null)
            {
                warningColor = warningRenderer.color;
            }

            _warningVisibleRequested = false;
            _damageActiveRequested = false;
            ApplyVisualState();
            SetDamageCollider(false);
        }

        private void OnEnable()
        {
            _warningVisibleRequested = false;
            _damageActiveRequested = false;
            _nextHitTimeByTarget.Clear();
            ApplyVisualState();
            SetDamageCollider(false);
        }

        private void OnDisable()
        {
            _nextHitTimeByTarget.Clear();
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

        private void SetDamageCollider(bool active)
        {
            if (damageCollider == null)
            {
                return;
            }

            damageCollider.enabled = active;
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
