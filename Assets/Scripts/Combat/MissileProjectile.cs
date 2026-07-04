using KeepCoreSafe.Blocks;
using KeepCoreSafe.Controllers;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Combat
{
    public sealed class MissileProjectile : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private LineRenderer trail;

        [Header("Trail")]
        [SerializeField] private Color trailStartColor = new Color(1f, 0.45f, 0.08f, 0.35f);
        [SerializeField] private Color trailEndColor = new Color(1f, 0.95f, 0.35f, 1f);
        [SerializeField, Range(0f, 1f)] private float endFadeAmount = 0.45f;
        [SerializeField, Min(0.01f)] private float minimumFlightDuration = 0.08f;

        private Block target;
        private Vector3 startPosition;
        private Vector3 previousPosition;
        private Vector2 arcDirection;
        private int damage;
        private float arcHeight;
        private float duration;
        private float elapsed;

        public void Launch(Block attackTarget, int attackDamage, float speed, float height)
        {
            target = attackTarget;
            damage = attackDamage;
            arcHeight = height;
            elapsed = 0f;
            startPosition = transform.position;
            previousPosition = startPosition;

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)startPosition).normalized;
            float side = Random.value < 0.5f ? -1f : 1f;
            arcDirection = new Vector2(-direction.y, direction.x) * side;
            duration = Mathf.Max(
                minimumFlightDuration,
                Vector2.Distance(startPosition, target.transform.position) / speed);

            if (trail != null)
            {
                trail.SetPosition(0, transform.position);
                trail.SetPosition(1, transform.position);
            }
        }

        private void Update()
        {
            if (GameManager.Phase != GamePhase.Combat || target == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 destination = target.transform.position;
            Vector3 linear = Vector3.Lerp(startPosition, destination, progress);
            Vector3 arc = (Vector3)(arcDirection * (Mathf.Sin(progress * Mathf.PI) * arcHeight));
            transform.position = linear + arc;

            if (trail != null)
            {
                trail.SetPosition(0, previousPosition);
                trail.SetPosition(1, transform.position);
                float fade = 1f - progress * endFadeAmount;
                Color startColor = trailStartColor;
                Color endColor = trailEndColor;
                startColor.a *= fade;
                endColor.a *= fade;
                trail.startColor = startColor;
                trail.endColor = endColor;
            }

            previousPosition = transform.position;
            if (progress < 1f)
                return;

            target.TakeDamage(damage);
            GameCameraController.Instance?.PlayImpactShake();
            Destroy(gameObject);
        }
    }
}
