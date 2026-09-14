using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>Character-led Atelier loadout, shared by town and adventure.</summary>
    public static class EquipScreen
    {
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onChanged, System.Action onClose, System.Action onStats = null, System.Action onCurses = null)
            => AtelierEquip.Build(stage, m, audio, onChanged, onClose, onStats, onCurses);
    }
}
