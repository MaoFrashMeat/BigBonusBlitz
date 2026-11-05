#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BBB.Core;
using BBB.Runtime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BBB.Tests
{
    public sealed class AdventureHeroJourneyTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameController game;
        static string output;
        static Camera camera;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
        static void AdvanceDialogue()
        {
            foreach(var op in UnityEngine.Object.FindObjectsByType<OpeningPlayer>(FindObjectsSortMode.None))
            {
                var dialogue=op.GetComponentInChildren<DialoguePresenter>();
                if(dialogue!=null&&dialogue.Root.gameObject.activeInHierarchy)dialogue.Root.Find("Background").GetComponent<Button>().onClick.Invoke();
            }
        }
        static void Capture(string name)
        {
            if(camera==null){camera=new GameObject("JourneyQACamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=270;camera.backgroundColor=AzureMapSkin.Navy;}
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
            foreach(var c in canvases)
            {
                var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)UnityEngine.Object.DestroyImmediate(scaler);
                c.renderMode=RenderMode.WorldSpace;c.transform.position=Vector3.zero;c.transform.localScale=Vector3.one;c.worldCamera=camera;
                foreach(var safe in c.GetComponentsInChildren<SafeStage>()){safe.enabled=false;safe.Stage.SetParent(c.transform,false);}
            }
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2556,1179)})
            {
                float w=540f*size.x/size.y;
                foreach(var c in canvases){((RectTransform)c.transform).sizeDelta=new Vector2(w,540);foreach(Transform child in c.transform)if(child.name=="Stage")child.localScale=Vector3.one*Mathf.Min(1,w/((RectTransform)child).sizeDelta.x);}
                Canvas.ForceUpdateCanvases();foreach(var c in canvases)foreach(var t in c.GetComponentsInChildren<Text>())t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);
                for(int pass=0;pass<3;pass++){foreach(var c in canvases)foreach(var g in c.GetComponentsInChildren<Graphic>()){g.SetAllDirty();g.Rebuild(CanvasUpdate.PreRender);g.canvasRenderer.cull=false;}Canvas.ForceUpdateCanvases();}
                var rt=RenderTexture.GetTemporary(size.x,size.y,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+"-"+size.x+".png"),image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
            }
        }
        [UnityTest]
        public IEnumerator OpeningEscapeGateAndTownReentryKeepHeroAlive()
        {
            if(Application.productName!="shop-qa"||!Application.dataPath.Replace('\\','/').Contains("/work/shop-qa/"))Assert.Ignore("Isolated QA required");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            yield return new EnterPlayMode();
            output=Environment.GetEnvironmentVariable("HERO_JOURNEY_OUTPUT");Assert.IsNotEmpty(output);Directory.CreateDirectory(output);
            foreach(var title in UnityEngine.Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(title.gameObject);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
            var audio=AudioManager.Create();audio.SeVolume=0;audio.BgmVolume=0;
            var machine=GameDataLoader.CreateMachine(new SystemRandom(928));machine.Credit=3000;machine.Adv.opDone=false;machine.Adv.opStep="";
            machine.Equip.Worn[EquipSlot.Weapon]=new EquipItem{baseId="w_spear",slot=EquipSlot.Weapon,name="QA spear",crafted=true};
            game=new GameObject("JourneyQA").AddComponent<GameController>();game.enabled=false;
            typeof(GameController).GetField("_m",Private).SetValue(game,machine);typeof(GameController).GetField("_audio",Private).SetValue(game,audio);
            Call(game,"BuildUi");Call(game,"RefreshUi");Call(game,"PlayCharacter","walk");
            game.StartCoroutine((IEnumerator)Call(game,"OpeningEscapeRoutine"));
            yield return null;
            Assert.IsTrue(OpeningPlayer.IsDialogueVisible);Capture("opening-dialogue");
            int pausedPose=Field<AdventureHeroPresenter>(game,"_adventureHero").View.Player.PoseIndex;
            for(int i=0;i<6;i++){Call(game,"Update");yield return null;}
            Assert.AreEqual(pausedPose,Field<AdventureHeroPresenter>(game,"_adventureHero").View.Player.PoseIndex,"Dialogue did not pause hero");
            float deadline=Time.realtimeSinceStartup+40;
            while(!Field<bool>(game,"_opEscape")&&Time.realtimeSinceStartup<deadline){AdvanceDialogue();Call(game,"Update");yield return null;}
            Assert.IsTrue(Field<bool>(game,"_opEscape"));Assert.IsTrue(OpeningPlayer.IsShowing);Assert.IsFalse(OpeningPlayer.IsDialogueVisible);
            var opHost=Field<OpeningPlayer>(game,"_op");Assert.IsFalse(Field<Image>(opHost,"_dim").raycastTarget,"Hidden OP dim steals adventure clicks");
            var hero=Field<AdventureHeroPresenter>(game,"_adventureHero");int before=hero.View.Player.PoseIndex;bool moved=false;deadline=Time.realtimeSinceStartup+.5f;
            while(Time.realtimeSinceStartup<deadline){Call(game,"Update");moved|=before!=hero.View.Player.PoseIndex;yield return null;}
            Assert.IsTrue(moved,"Playable escape froze hero behind hidden OP host");Capture("playable-escape");
            bool visitedTown=false,visitedGate=false;deadline=Time.realtimeSinceStartup+120;
            while(Time.realtimeSinceStartup<deadline)
            {
                AdvanceDialogue();
                var map=UnityEngine.Object.FindFirstObjectByType<MapScreen>();if(map!=null)visitedTown=true;
                if(game==null)game=UnityEngine.Object.FindFirstObjectByType<GameController>();
                if(game!=null)
                {
                    var m=Field<SlotMachine>(game,"_m");
                    if(m!=null)
                    {
                        if(!game.enabled)Call(game,"Update");
                        if(Field<bool>(game,"_opGate"))visitedGate=true;
                        if(m.Adv.opDone&&!Field<bool>(game,"_inputLocked"))break;
                        if((Field<bool>(game,"_opEscape")||Field<bool>(game,"_opGate"))&&!Field<bool>(game,"_inputLocked"))
                        {
                            if(!m.IsGameActive)Field<Button>(game,"_btnBet").onClick.Invoke();
                            else if(Time.time>=Field<float>(game,"_stopUnlockTime"))
                            {
                                for(int i=2;i>=0;i--)if(m.Stopped[i]==null){typeof(GameController).GetMethod("StopReel",Private,null,new[]{typeof(int)},null).Invoke(game,new object[]{i});break;}
                            }
                        }
                    }
                }
                yield return null;
            }
            Assert.IsTrue(visitedTown&&visitedGate,"OP did not pass through town and gate");Assert.IsNotNull(game);
            Assert.IsTrue(Field<SlotMachine>(game,"_m").Adv.opDone,"OP did not finish");
            Assert.AreEqual("w_spear",Field<AdventureHeroPresenter>(game,"_adventureHero").View.Player.WeaponId);
            Capture("opening-complete");
            Call(game,"OnBackToTown");yield return null;yield return null;
            var town=UnityEngine.Object.FindFirstObjectByType<MapScreen>();Assert.IsNotNull(town,"Return did not open town");Capture("returned-town");
            Call(town,"GoAdventure");deadline=Time.realtimeSinceStartup+20;
            while(Time.realtimeSinceStartup<deadline)
            {
                game=UnityEngine.Object.FindFirstObjectByType<GameController>();
                if(game!=null&&Field<AdventureHeroPresenter>(game,"_adventureHero")!=null)break;
                yield return null;
            }
            Assert.IsNotNull(game);Assert.AreEqual("w_spear",Field<AdventureHeroPresenter>(game,"_adventureHero").View.Player.WeaponId);
            Assert.IsTrue(Field<SlotMachine>(game,"_m").Adv.opDone);
            // Creation precedes MapScreen's fade-out. Capture the visible destination, not its black transition.
            deadline=Time.realtimeSinceStartup+5;
            while(GameObject.Find("MapFadeOut")!=null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNull(GameObject.Find("MapFadeOut"),"Town reentry transition did not finish");
            yield return null;Capture("reentered-adventure");
            File.WriteAllText(Path.Combine(output,"journey.json"),"{\"passed\":true,\"stills\":10,\"openingEscapeTownGate\":true,\"hiddenOpeningHeroResumes\":true,\"hiddenDimDoesNotRaycast\":true,\"townReentryWeapon\":true,\"realMouseVerified\":false}");
            yield return new ExitPlayMode();
        }
    }
}
#endif
