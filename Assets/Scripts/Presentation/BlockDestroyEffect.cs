using UnityEngine;

namespace KeepCoreSafe.Presentation
{
    public sealed class BlockDestroyEffect : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private SpriteRenderer[] pieces = System.Array.Empty<SpriteRenderer>();
        [SerializeField] private Sprite[] pieceSprites = System.Array.Empty<Sprite>();

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float startRadius = 0.16f;
        [SerializeField] private Vector2 travelDistance = new(0.45f, 1.05f);
        [SerializeField] private Vector2 arcHeight = new(0.35f, 0.85f);
        [SerializeField] private Vector2 duration = new(0.45f, 0.72f);
        [SerializeField] private Vector2 pieceScale = new(0.8f, 1.25f);
        [SerializeField] private Vector2 rotationSpeed = new(-320f, 320f);

        private Vector3[] startPositions;
        private Vector3[] endPositions;
        private float[] heights;
        private float[] durations;
        private float[] elapsed;
        private float[] startRotations;
        private float[] angularSpeeds;
        private Color startColor;
        private int activePieceCount;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            int count = pieces != null ? pieces.Length : 0;
            startPositions = new Vector3[count];
            endPositions = new Vector3[count];
            heights = new float[count];
            durations = new float[count];
            elapsed = new float[count];
            startRotations = new float[count];
            angularSpeeds = new float[count];
            SetPiecesActive(false);
        }

        public bool Play(Vector3 worldPosition, Color blockColor)
        {
            if (pieces == null || pieces.Length == 0 || pieceSprites == null || pieceSprites.Length == 0)
                return false;

            transform.position = worldPosition;
            gameObject.SetActive(true);
            startColor = blockColor;
            startColor.a = 1f;
            activePieceCount = 0;

            for (int i = 0; i < pieces.Length; i++)
            {
                SpriteRenderer piece = pieces[i];
                if (piece == null)
                    continue;

                Vector2 startOffset = Random.insideUnitCircle * startRadius;
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.001f)
                    direction = Vector2.right;
                direction.Normalize();

                startPositions[i] = startOffset;
                endPositions[i] = startOffset + direction * Random.Range(
                    Mathf.Min(travelDistance.x, travelDistance.y),
                    Mathf.Max(travelDistance.x, travelDistance.y));
                heights[i] = Random.Range(
                    Mathf.Min(arcHeight.x, arcHeight.y),
                    Mathf.Max(arcHeight.x, arcHeight.y));
                durations[i] = Mathf.Max(0.01f, Random.Range(
                    Mathf.Min(duration.x, duration.y),
                    Mathf.Max(duration.x, duration.y)));
                elapsed[i] = 0f;
                startRotations[i] = Random.Range(0f, 360f);
                angularSpeeds[i] = Random.Range(
                    Mathf.Min(rotationSpeed.x, rotationSpeed.y),
                    Mathf.Max(rotationSpeed.x, rotationSpeed.y));

                piece.sprite = pieceSprites[Random.Range(0, pieceSprites.Length)];
                piece.color = startColor;
                piece.transform.localPosition = startPositions[i];
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, startRotations[i]);
                piece.transform.localScale = Vector3.one * Random.Range(
                    Mathf.Min(pieceScale.x, pieceScale.y),
                    Mathf.Max(pieceScale.x, pieceScale.y));
                piece.gameObject.SetActive(true);
                activePieceCount++;
            }

            IsPlaying = activePieceCount > 0;
            if (!IsPlaying)
                gameObject.SetActive(false);
            return IsPlaying;
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            for (int i = 0; i < pieces.Length; i++)
            {
                SpriteRenderer piece = pieces[i];
                if (piece == null || !piece.gameObject.activeSelf)
                    continue;

                elapsed[i] = Mathf.Min(durations[i], elapsed[i] + Time.deltaTime);
                float normalizedTime = elapsed[i] / durations[i];
                Vector3 position = Vector3.Lerp(startPositions[i], endPositions[i], normalizedTime);
                position.y += 4f * heights[i] * normalizedTime * (1f - normalizedTime);
                piece.transform.localPosition = position;
                piece.transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    startRotations[i] + angularSpeeds[i] * normalizedTime);

                if (normalizedTime >= 0.5f)
                {
                    float descent = Mathf.InverseLerp(0.5f, 1f, normalizedTime);
                    piece.color = Color.Lerp(startColor, new Color(0f, 0f, 0f, 0f), descent);
                }

                if (normalizedTime >= 1f)
                {
                    piece.gameObject.SetActive(false);
                    activePieceCount--;
                }
            }

            if (activePieceCount <= 0)
            {
                IsPlaying = false;
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            IsPlaying = false;
            activePieceCount = 0;
            SetPiecesActive(false);
        }

        private void SetPiecesActive(bool active)
        {
            if (pieces == null)
                return;

            foreach (SpriteRenderer piece in pieces)
            {
                if (piece != null && piece.gameObject.activeSelf != active)
                    piece.gameObject.SetActive(active);
            }
        }
    }
}
