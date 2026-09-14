using System;
using BBB.Core;
using UnityEngine;
namespace BBB.Runtime
{
    public static class AtelierResult
    {
        /// <summary>Rewards have already been committed by SlotMachine. This screen never grants them again.</summary>
        public static GameObject Build(Transform stage,SlotMachine m,GameResult result,Action continueToTown)
        {
            var body=AtelierUi.Screen(stage,"ChapterResult",UiSkin.Hex("#301e2d"),null,out var root);
            var clip=AtelierUi.Panel(body,"CharacterPanel",-194,0,572,540,UiSkin.Hex("#bd575d"));clip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            AtelierUi.Text(clip,"Victory",0,155,540,130,"VICTORY",74,UiSkin.Hex("#d88480"),true);
            AtelierUi.Art(clip,"Salia",0,-104,1130,636,AtelierUi.Sprite("SaliaRig/source"));
            AtelierUi.Text(body,"Chapter",-243,234,430,24,$"CHAPTER {Mathf.Max(1,m.Adv.chapter-1)} / MISSION COMPLETE",13,AtelierUi.Light,true);
            AtelierUi.Text(body,"MVP",-230,-220,430,42,"Our next chapter.",32,AtelierUi.Light,true);
            const float x=282,w=336;
            AtelierUi.Text(body,"Eyebrow",x,196,w,24,"THE LIGHT RETURNS",11,AtelierUi.Gold);
            AtelierUi.Text(body,"Title",x,135,w,88,"霧を晴らす、一閃。",34,AtelierUi.Light,true);
            AtelierUi.Text(body,"Subtitle",x,67,w,34,$"第{Mathf.Max(1,m.Adv.chapter-1)}章を踏破しました",17,AtelierUi.Light);
            AtelierUi.Text(body,"Setbacks",x,12,w,40,$"引き返し {result.chapterSetbacks} 回\n到達レベル {m.PlayerLevel}",17,AtelierUi.Sub);
            AtelierUi.Text(body,"RewardLabel",x,-45,w,24,"獲得した章クリア報酬",15,AtelierUi.Gold,true);
            AtelierUi.Art(body,"Soul",146,-98,60,60,AtelierUi.Icon("soul"));
            AtelierUi.Text(body,"Reward",315,-98,260,56,$"+{result.chapterSouls:N0} SOUL",31,AtelierUi.Light,true);
            AtelierUi.Text(body,"Balance",x,-148,w,30,$"所持 {m.Wallet.Souls:N0} ソウル  /  受け取り済み",14,AtelierUi.Sub);
            bool leaving=false;
            AtelierUi.Button(body,"Continue",x,-205,w,"街へ戻る  →",()=>{if(leaving)return;leaving=true;continueToTown?.Invoke();},AtelierUi.Gold,AtelierUi.Ink);
            AtelierUi.Text(body,"Saved",x,-249,w,20,"冒険の成果は保存されました",11,AtelierUi.Sub);
            return root;
        }
    }
}
