using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public class TelegraphTile : MonoBehaviour
    {
        [SerializeField] private Collider2D damageCollider;
        [SerializeField] private SpriteRenderer warningRenderer;
        [SerializeField] private Behaviour warningEffect;

        private void Awake()
        {
            AutoBindComponents();
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
            if (warningRenderer != null)
            {
                warningRenderer.enabled = visible;
            }

            if (warningEffect != null)
            {
                warningEffect.enabled = visible;
            }
        }

        public void SetDamageActive(bool active)
        {
            if (damageCollider != null)
            {
                damageCollider.enabled = active;
            }
        }
    }
}
