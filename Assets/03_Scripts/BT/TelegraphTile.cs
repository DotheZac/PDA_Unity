using System.Collections;
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

        [SerializeField] private Collider2D damageCollider;
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private Behaviour warningEffect;
        [SerializeField] private Color warningColor = Color.white;
        [SerializeField] private Color damageActiveColor = Color.blue;

        private bool _warningVisibleRequested;
        private bool _damageActiveRequested;

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
            ApplyVisualState();
            SetDamageCollider(false);
        }

        private void Reset()
        {
            AutoBindComponents();
        }

        private void OnValidate()
        {
            AutoBindComponents();
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
        }

        private void SetDamageCollider(bool active)
        {
            if (damageCollider == null)
            {
                return;
            }

            damageCollider.enabled = active;
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
