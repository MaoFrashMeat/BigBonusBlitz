using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// The game stays in one scene, so textures from Resources.Load would otherwise stay in memory
    /// for the whole session. Requests are coalesced because the scan walks every loaded object.
    /// </summary>
    public static class AdventureAssets
    {
        const float MinSecondsBetweenScans=20f;
        static bool pending;
        static int requestedFrame;
        static float lastScan=float.NegativeInfinity;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){pending=false;lastScan=float.NegativeInfinity;}
        public static void RequestUnload(){if(!Application.isPlaying)return;pending=true;requestedFrame=Time.frameCount;}
        /// <summary>Waits one frame so every owner has dropped its references before the scan.</summary>
        public static void Pump()
        {
            if(!pending||Time.frameCount==requestedFrame||Time.unscaledTime-lastScan<MinSecondsBetweenScans)return;
            pending=false;lastScan=Time.unscaledTime;Resources.UnloadUnusedAssets();
        }
    }
}
