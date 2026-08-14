using KeepCoreSafe.Localization;
using UnityEngine;

namespace KeepCoreSafe.Data
{
    [CreateAssetMenu(fileName = "BlockColor", menuName = "Keep Core Safe/Block System/Color")]
    public sealed class BlockColorData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Color color = Color.white;

        public string DisplayName => LocalizationManager.Get(displayName, displayName);
        public string DisplayNameKey => displayName;
        public Color Color => color;
    }
}
