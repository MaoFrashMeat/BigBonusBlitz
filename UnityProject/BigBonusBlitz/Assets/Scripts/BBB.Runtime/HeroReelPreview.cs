using System;

namespace BBB.Runtime
{
    /// <summary>Preview of GameController's existing bell stop order, without a SlotMachine or gameplay writes.</summary>
    public sealed class HeroReelPreview
    {
        readonly HeroMotionPlayer player;
        bool bell;
        int stops;
        public bool Active {get;private set;}
        public int Stops=>stops;
        public HeroReelPreview(HeroMotionPlayer player){this.player=player;}
        public void Begin(bool isBell)
        {
            player.CancelToLoop();bell=isBell;stops=0;Active=true;
            player.Request(bell?"attack-f1":"walk");
        }
        public void Stop(int count)
        {
            if(count<1||count>3)throw new ArgumentOutOfRangeException(nameof(count));
            if(!Active||count<=stops)return;
            while(stops<count)
            {
                stops++;
                if(bell&&stops==1)player.Request("attack-f2");
                if(bell&&stops==2)player.Request("attack-f3-4");
            }
            if(stops==3)Active=false;
        }
        public void Cancel(){Active=false;stops=0;player.CancelToLoop();}
    }
}
