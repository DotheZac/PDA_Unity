using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    public class SpriteBlink : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private float speed = 2f;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInParent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            SetAlpha(1f);
        }

        private void Update()
        {
            if (targetRenderer == null)
            {
                return;
            }

            var alpha = Mathf.PingPong(Time.time * speed, 1f);
            SetAlpha(alpha);
        }

        private void OnDisable()
        {
            SetAlpha(1f);
        }

        private void SetAlpha(float alpha)
        {
            if (targetRenderer == null)
            {
                return;
            }

            var c = targetRenderer.color;
            c.a = alpha;
            targetRenderer.color = c;
        }
    }
}