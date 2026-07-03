using KeepCoreSafe.Blocks;
using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Combat
{
    public sealed class MissileProjectile : MonoBehaviour
    {
        private static Material sharedMaterial;

        private Block target;
        private LineRenderer trail;
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
            startPosition = transform.position;
            previousPosition = startPosition;

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)startPosition).normalized;
            float side = Random.value < 0.5f ? -1f : 1f;
            arcDirection = new Vector2(-direction.y, direction.x) * side;
            duration = Mathf.Max(0.08f, Vector2.Distance(startPosition, target.transform.position) / speed);
            CreateTrail();
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

            trail.SetPosition(0, previousPosition);
            trail.SetPosition(1, transform.position);
            previousPosition = transform.position;

            float fade = 1f - progress * 0.45f;
            trail.startColor = new Color(1f, 0.45f, 0.08f, fade * 0.35f);
            trail.endColor = new Color(1f, 0.95f, 0.35f, fade);

            if (progress >= 1f)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }

        private void CreateTrail()
        {
            trail = gameObject.AddComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.positionCount = 2;
            trail.numCapVertices = 4;
            trail.startWidth = 0.04f;
            trail.endWidth = 0.12f;
            trail.sortingOrder = 15;

            if (sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    sharedMaterial = new Material(shader) { name = "Missile Trail Material" };
            }

            trail.sharedMaterial = sharedMaterial;
            trail.SetPosition(0, transform.position);
            trail.SetPosition(1, transform.position);
        }
    }
}
