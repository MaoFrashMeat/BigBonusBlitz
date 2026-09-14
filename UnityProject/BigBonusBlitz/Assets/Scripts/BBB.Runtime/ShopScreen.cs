using BBB.Core;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// 街のショップ。ソウルでスキルと装備を強化する。買うとレベルが上がり効果量が増える。
    /// 品揃えと効果量は game_config.json の shop で決まる。
    /// </summary>
    public static class ShopScreen
    {
        /// <summary>ショップを開く。返した GameObject を Destroy すれば閉じる。</summary>
        public static GameObject Build(Transform stage, SlotMachine m, AudioManager audio, System.Action onSoulsChanged, System.Action onClose)
            => AtelierShop.Build(stage, m, audio, onSoulsChanged, onClose);
    }
}
