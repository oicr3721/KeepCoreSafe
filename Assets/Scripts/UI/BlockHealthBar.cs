using KeepCoreSafe.Blocks;
using UnityEngine;

namespace KeepCoreSafe.UI
{
    [RequireComponent(typeof(Block))]
    public sealed class BlockHealthBar : MonoBehaviour
    {
        private const float BarWidth = 0.6f;
        private const float BarHeight = 0.1f;
        private const float BarYOffset = 0.68f;

        private static Sprite whiteSprite;

        private Block block;
        private Transform fillTransform;

        private void Awake()
        {
            block = GetComponent<Block>();

            if (whiteSprite == null)
                whiteSprite = CreateWhiteSprite();

            CreateVisuals();
        }

        private void OnEnable()
        {
            block.HealthChanged += UpdateBar;
            UpdateBar(block.CurrentHP, block.MaxHP);
        }

        private void OnDisable()
        {
            if (block != null)
                block.HealthChanged -= UpdateBar;
        }

        private void CreateVisuals()
        {
            CreateBarPart(
                "Background",
                new Color(0.15f, 0.15f, 0.15f),
                new Vector3(BarWidth + 0.08f, BarHeight + 0.06f, 1f),
                10);

            SpriteRenderer fill = CreateBarPart(
                "Fill",
                Color.green,
                new Vector3(BarWidth, BarHeight, 1f),
                11);

            fillTransform = fill.transform;
        }

        private SpriteRenderer CreateBarPart(
            string name,
            Color color,
            Vector3 scale,
            int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(0f, BarYOffset, 0f);
            obj.transform.localScale = scale;

            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private void UpdateBar(int currentHP, int maxHP)
        {
            float ratio = maxHP <= 0 ? 0f : (float)currentHP / maxHP;

            fillTransform.localScale = new Vector3(
                ratio * BarWidth,
                BarHeight,
                1f);

            fillTransform.localPosition = new Vector3(
                -(BarWidth - ratio * BarWidth) * 0.5f,
                BarYOffset,
                0f);
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, 1, 1),
                Vector2.one * 0.5f,
                1f);
        }
    }
}