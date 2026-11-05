using BBB.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BBB.Runtime
{
    /// <summary>Non-saving P2 review surface. Kept separate from the production game's art and balance.</summary>
    public sealed class HeroPresentationLab : MonoBehaviour
    {
        public HeroPresentationView Hero {get;private set;}
        public EnemyPresentationView Enemy {get;private set;}
        public HeroReelPreview Reels {get;private set;}
        public RectTransform Stage {get;private set;}
        public ParallaxBackground Background {get;private set;}
        public bool Paused;
        public float Speed=1;
        public int EnemyIndex {get;private set;}
        Text state,weaponLabel,enemyName,cueLabel;
        Image cuePlate;
        float cueHold;
        bool soundEnabled;
        AudioManager previewAudio;
        public static HeroPresentationLab Open()
        {
            // DontSave previews are excluded by FindFirstObjectByType, including in Edit Mode.
            foreach(var previous in Resources.FindObjectsOfTypeAll<HeroPresentationLab>())
                if(previous.gameObject.scene.IsValid())return previous;
            bool ownsEvents=Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()==null;
            UiFactory.EnsureEventSystem();
            var root=new GameObject("SaliaMotionPreview");root.hideFlags=HideFlags.DontSave;
            if(ownsEvents)Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>().transform.SetParent(root.transform,false);
            var lab=root.AddComponent<HeroPresentationLab>();lab.Build();return lab;
        }
        Text Label(Transform parent,string name,float x,float y,float w,float h,string value,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=UiFactory.Label(parent,name,new Vector2(x,y),new Vector2(w,h),value,size,align,color);t.font=AzureMapSkin.Serif;return t;
        }
        void Build()
        {
            var canvas=UiFactory.CreateCanvas("HeroPreviewCanvas");canvas.sortingOrder=1100;canvas.transform.SetParent(transform,false);
            UiFactory.Panel(canvas.transform,"Backdrop",Vector2.zero,new Vector2(5000,5000),UiSkin.Hex("#07111f"));
            Stage=SafeStage.Create(canvas,1170,540).Stage;
            Label(Stage,"Kicker",-322,237,480,25,"CHARACTER MOTION  /  P2 PROTOTYPE",12,AzureMapSkin.Gold);
            Label(Stage,"Title",-322,202,480,48,"サリア ― 剣と足音",30,AzureMapSkin.Paper);
            AzureMapSkin.Button(Stage,"Close",523,235,82,"閉じる",Close,false,44,null,13);
            AzureMapSkin.Button(Stage,"EnemyAttack",65,202,145,"敵の攻撃",()=>RequestEnemy("attack"),false,44,null,14);
            AzureMapSkin.Button(Stage,"EnemyHit",220,202,145,"敵の被弾",()=>RequestEnemy("hit"),false,44,null,14);
            AzureMapSkin.Button(Stage,"EnemyDefeat",375,202,145,"敵の決着",()=>RequestEnemy("defeat"),false,44,null,14);
            var field=UiFactory.Panel(Stage,"Forest",new Vector2(-150,-3),new Vector2(830,348),Color.clear);field.gameObject.AddComponent<RectMask2D>();
            Background=ParallaxBackground.Create(field);Background.SetStage("C-1",true);Background.SetHour(10,0);Background.AutoCycle=false;
            Hero=HeroPresentationView.Create(field,new Vector2(-140,-145));
            Reels=new HeroReelPreview(Hero.Player);
            AzureMapSkin.Button(Stage,"ReelBet",0,202,110,"BET・構え",()=>{Reels.Begin(true);Hero.Render();Refresh();},false,44,null,12);
            for(int stop=1;stop<=3;stop++)
            {
                int count=stop;
                AzureMapSkin.Button(Stage,"ReelStop"+stop,stop*120,202,110,"第"+stop+"停止",()=>{Reels.Stop(count);Hero.Render();Refresh();},false,44,null,13);
            }
            var supportIds=new[]{"focus","guard","cast"};var supportNames=new[]{"集中","防御","詠唱"};
            for(int i=0;i<supportIds.Length;i++)
            {
                string id=supportIds[i];
                AzureMapSkin.Button(Stage,"Support_"+id,65+i*155,202,145,supportNames[i],()=>Request(id),false,44,null,14);
            }
            ShowReelControls(false);
            Enemy=EnemyPresentationView.Create(field,new Vector2(245,-145));
            var namePlate=UiSkin.Img(field,"EnemyNamePlate",new Vector2(232,124),new Vector2(250,34),AzureMapSkin.Sprite("button-navy"),Color.white);
            namePlate.type=Image.Type.Sliced;namePlate.pixelsPerUnitMultiplier=8;
            enemyName=Label(namePlate.transform,"EnemyName",0,0,218,30,"",13,AzureMapSkin.Paper,TextAnchor.MiddleCenter);
            cuePlate=UiSkin.Img(field,"CuePlate",new Vector2(-120,123),new Vector2(250,34),AzureMapSkin.Sprite("button-navy"),Color.white);
            cuePlate.type=Image.Type.Sliced;cuePlate.pixelsPerUnitMultiplier=8;
            cueLabel=Label(cuePlate.transform,"Cue",0,0,218,30,"",15,AzureMapSkin.Gold,TextAnchor.MiddleCenter);cuePlate.gameObject.SetActive(false);
            SetEnemy(0);
            var side=UiFactory.Panel(Stage,"Inspector",new Vector2(420,-7),new Vector2(282,362),Color.clear);
            AzureShopSkin.Surface(side,"Paper",0,0,282,362,false);
            Label(side,"Heading",0,141,230,32,"動きと装備の見本",20,AzureMapSkin.Ink);
            weaponLabel=Label(side,"Weapon",0,102,230,38,"",15,AzureMapSkin.Ink);
            AzureMapSkin.Button(side,"Sword",-58,58,112,"剣",()=>Equip("w_blade"),false,44,null,14);
            AzureMapSkin.Button(side,"Spear",58,58,112,"槍",()=>Equip("w_spear"),false,44,null,14);
            state=Label(side,"State",0,10,230,40,"",13,AzureMapSkin.Ink);
            Label(side,"SpeedCaption",0,-33,230,22,"速度  0.25 ～ 1.5 倍",12,AzureMapSkin.Ink);
            AzureUiControls.Slider(side,"Speed",0,-60,216,.6f,v=>Speed=Mathf.Lerp(.25f,1.5f,v));
            AzureMapSkin.Button(side,"Pause",-58,-105,112,"停止 / 再開",()=>Paused=!Paused,false,44,null,12);
            AzureMapSkin.Button(side,"Grip",58,-105,112,"握り位置",()=>Hero.ShowGrip(!Hero.transform.Find("GripMarker").gameObject.activeSelf),false,44,null,12);
            AzureMapSkin.Button(side,"PreviewSound",0,-153,230,Application.isPlaying?"動作音 OFF":"音は再生モードで確認",()=>
            {
                if(!Application.isPlaying)return;
                soundEnabled=!soundEnabled;
                if(soundEnabled&&previewAudio==null)
                {
                    bool owns=Object.FindFirstObjectByType<AudioManager>()==null;
                    previewAudio=AudioManager.Create();
                    if(owns)previewAudio.transform.SetParent(transform,false);
                }
                Stage.Find("Inspector/PreviewSound").GetComponentInChildren<Text>().text=soundEnabled?"動作音 ON":"動作音 OFF";
            },false,44,null,12);
            var ids=new[]{"idle","walk","attack","hit","victory"};var names=new[]{"構える","歩く","攻撃","被弾","勝利"};
            for(int i=0;i<ids.Length;i++){string id=ids[i];AzureMapSkin.Button(Stage,"Motion_"+id,-492+i*155,-218,145,names[i],()=>Request(id),false,46,null,15);}
            AzureMapSkin.Button(Stage,"NextEnemy",420,-218,250,"相手を切り替える  →",()=>SetEnemy((EnemyIndex+1)%HeroPresentation.Config.enemies.Length),false,46,null,14);
            Label(Stage,"Footer",0,-255,1120,22,"霧の森  /  武器は体と別描画  /  攻撃中の武器変更は復帰時に反映",11,AzureMapSkin.Gold,TextAnchor.MiddleCenter);
            Hero.Player.Cue+=OnCue;Refresh();
        }
        void OnCue(string cue)
        {
            PlaySound("hero",cue);
            cueLabel.text=cue=="contact"?"踏み込み → 接触":cue=="anticipation"?"ため":cue=="footstep"?"接地":cue=="swing"?"振り始め":cue=="guard"?"防御":cue=="magic"?"詠唱 → 放つ":cue=="hurt"?"被弾":cue=="victory"?"勝利":"戻り";cueHold=.35f;cuePlate.gameObject.SetActive(true);
            if(cue=="contact")Enemy.Request("hit"); // Preview reaction only; no combat result is generated.
        }
        void OnEnemyCue(string cue){PlaySound("enemy",cue);if(cue=="contact")Request("hit");}
        void PlaySound(string source,string cue)
        {
            if(!Application.isPlaying||!soundEnabled||previewAudio==null||previewAudio.SeVolume<=0)return;
            var sound=HeroPresentation.Sound(source,cue);if(sound==null)return;
            if(!string.IsNullOrEmpty(sound.key)&&previewAudio.TryPlay(sound.key,sound.gain))return;
            if(System.Enum.TryParse<MotionCue>(sound.fallback,out var fallback))previewAudio.Motion(fallback);
        }
        public void RequestEnemy(string id){Enemy.Request(id);Refresh();}
        public void ShowReelControls(bool enabled)
        {
            if(!enabled&&Reels!=null&&Reels.Active){Reels.Cancel();Hero.Render();}
            foreach(string name in new[]{"EnemyAttack","EnemyHit","EnemyDefeat"})Stage.Find(name).gameObject.SetActive(!enabled);
            foreach(string name in new[]{"ReelBet","ReelStop1","ReelStop2","ReelStop3"})Stage.Find(name).gameObject.SetActive(enabled);
            foreach(string name in new[]{"focus","guard","cast"})Stage.Find("Support_"+name).gameObject.SetActive(false);
        }
        public void ShowSupportControls()
        {
            ShowReelControls(false);
            foreach(string name in new[]{"EnemyAttack","EnemyHit","EnemyDefeat"})Stage.Find(name).gameObject.SetActive(false);
            foreach(string name in new[]{"focus","guard","cast"})Stage.Find("Support_"+name).gameObject.SetActive(true);
        }
        public void Request(string id)
        {
            if(Reels.Active)Reels.Cancel();
            Hero.Player.Request(id);Hero.Render();Background.IsWalking=id=="walk";Refresh();
        }
        public void Equip(string id){Hero.Player.Equip(new EquipItem{baseId=id,slot=EquipSlot.Weapon});Hero.Render();Refresh();}
        public void SetEnemy(int index)
        {
            EnemyIndex=index;var def=HeroPresentation.Config.enemies[index];
            if(Enemy.Player!=null)Enemy.Player.Cue-=OnEnemyCue;
            Enemy.SetEnemy(def.id);Enemy.Player.Cue+=OnEnemyCue;enemyName.text=def.name+" / 待機";
        }
        public void Tick(float delta)
        {
            if(!Paused){Hero.Step(delta*Speed);Enemy.Step(delta*Speed);}
            cueHold-=delta;if(cueHold<=0){cueLabel.text="";cuePlate.gameObject.SetActive(false);}Refresh();
        }
        void Update(){Tick(Time.unscaledDeltaTime);}
        void Refresh()
        {
            weaponLabel.text="装備外見: "+(Hero.Player.WeaponId=="w_spear"?"槍":"剣")+(Hero.Player.PendingWeapon!=null?"  （変更待ち）":"");
            state.text=$"{Hero.Player.State}  /  コマ {Hero.Player.PoseIndex+1}\n{(Paused?"停止中":"再生中")}   {Speed:0.00}×";
            enemyName.text=HeroPresentation.Config.enemies[EnemyIndex].name+" / "+Enemy.Player.Phase;
        }
        public void Close(){if(Application.isPlaying)Destroy(gameObject);else DestroyImmediate(gameObject);}
    }
}
