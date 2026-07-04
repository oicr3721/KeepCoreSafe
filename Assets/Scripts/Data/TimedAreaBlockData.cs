using UnityEngine;

namespace KeepCoreSafe.Data
{
    public abstract class TimedAreaBlockData : AreaBlockData
    {
        [SerializeField, Min(0.01f)]
        private float actionCooldown = 1f;

        public float ActionCooldown => actionCooldown;
    }
}
