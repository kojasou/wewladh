using System;

namespace Wewladh
{
    public abstract class Reactor : VisibleObject
    {
        public DateTime NextTick { get; set; }
        public long TickCount { get; set; }
        public int BaseTickSpeed { get; protected set; }
        public int MinimumTickSpeed { get; protected set; }
        public int TickSpeedMod { get; set; }
        public int TickSpeed
        {
            get
            {
                int value = BaseTickSpeed + TickSpeedMod;
                if (value < MinimumTickSpeed)
                    return MinimumTickSpeed;
                return value;
            }
        }
        public Spawn SpawnControl { get; set; }
        public bool Dead { get; set; }
        public bool Alive
        {
            get { return !Dead; }
            set { Dead = !value; }
        }
        public abstract DialogB OnWalkover(Character c);
        public Reactor()
        {

        }
        public override void Update()
        {
            if (SpawnControl != null && SpawnControl.SpecificTime && SpawnControl.SpawnTime != GameServer.Time && !Dead)
            {
                Dead = true;
            }

            if (Dead)
            {
                foreach (Client c in GameServer.Clients)
                {
                    if (c.Player != null && c.Player.DialogSession.IsOpen && c.Player.DialogSession.GameObject == this)
                    {
                        c.Player.DialogSession.IsOpen = false;
                        c.Player.DialogSession.Dialog = null;
                        c.Player.DialogSession.GameObject = null;
                        c.Player.DialogSession.Map = null;
                        c.Enqueue(Dialog.ExitPacket());
                    }
                }
                GameServer.RemoveGameObject(this);
            }

            //if (DateTime.UtcNow > NextTick && !Dead)
            //{
            //    OnTick();
            //    TickCount++;
            //    NextTick = DateTime.UtcNow.AddMilliseconds(TickSpeed);
            //}
        }
        public override void DisplayTo(VisibleObject obj)
        {

        }
        public virtual void OnTick()
        {

        }
        public virtual void OnChatMessage(Character c, string message)
        {

        }
    }
}