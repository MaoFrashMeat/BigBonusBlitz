using BBB.Runtime;
using UnityEditor;
using UnityEngine;

public static class HeroPrototypeMenu
{
    static HeroPresentationLab lab;
    static double last;
    [MenuItem("BBB/サリアのモーション・武器見本")]
    public static void Open()
    {
        lab=HeroPresentationLab.Open();last=EditorApplication.timeSinceStartup;
        lab.ShowReelControls(false);
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    [MenuItem("BBB/サリアのリール連動見本")]
    public static void OpenReels(){Open();lab.ShowReelControls(true);}
    [MenuItem("BBB/サリアの集中・防御・詠唱見本")]
    public static void OpenSupport(){Open();lab.ShowSupportControls();}
    static void Tick()
    {
        if(lab==null){EditorApplication.update-=Tick;return;}
        double now=EditorApplication.timeSinceStartup;
        if(!Application.isPlaying){lab.Tick((float)(now-last));EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();}
        last=now;
    }
}
