using KeepCoreSafe.Managers;
using UnityEngine;

namespace KeepCoreSafe.Blocks
{
    /// <summary>
    /// Base for Blocks that need a per-frame Combat simulation tick.
    /// Passive Blocks deliberately do not inherit this component so Unity does not invoke
    /// an empty Update for every wall, supply, and Core instance.
    /// </summary>
    public abstract class CombatBlock : Block
    {
        protected virtual void Update()
        {
            if (GameManager.Phase == GamePhase.Combat)
                OnCombatUpdate(Time.deltaTime);
        }

        protected abstract void OnCombatUpdate(float deltaTime);
    }
}
