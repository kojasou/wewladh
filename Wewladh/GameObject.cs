using System.Collections.Generic;
using System.Dynamic;

namespace Wewladh
{
    public class GameObject : DynamicObject
    {
        public GameServer GameServer { get; set; }
        public uint ID { get; set; }
        public long GUID { get; set; }
        public string Name { get; set; }
        private Dictionary<string, object> session;
        public string TypeName
        {
            get { return GetType().Name; }
        }
        public GameObject()
        {
            this.session = new Dictionary<string, object>();
        }
        public bool GiveDialog(Character c, Dialog d)
        {
            if (c is Player && d != null)
            {
                var player = c as Player;
                if (player.DialogSession.IsOpen)
                    return false;
                d.GameObject = this;
                if (d is DialogB)
                {
                    player.DialogSession.IsOpen = true;
                    player.DialogSession.GameObject = this;
                    player.DialogSession.Dialog = d as DialogB;
                    player.DialogSession.Map = player.Map;
                }
                player.Client.Enqueue(d.ToPacket());
                return true;
            }
            return false;
        }
        public T Session<T>(string key)
        {
            if (session.ContainsKey(key) && session[key] is T)
                return (T)session[key];
            return default(T);
        }
        public void Session(string key, object value)
        {
            if (session.ContainsKey(key))
                session[key] = value;
            else
                session.Add(key, value);
        }
        public virtual void Update()
        {

        }
        public virtual void Save()
        {

        }

        public virtual void OnGameServerInsert(GameServer gs) { }
        public virtual void OnGameServerRemove(GameServer gs) { }
        public virtual void OnGameServerDelete(GameServer gs) { }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.ReturnType == typeof(int))
            {
                result = 0;
                return true;
            }

            result = new object();
            return true;
        }
    }

    public class VisibleObject : GameObject
    {
        public int Sprite { get; set; }
        public int Color { get; set; }

        private uint mapID;
        public Map Map
        {
            get
            {
                return GameServer.GameObject<Map>(mapID);
            }
            set
            {
                if (value != null)
                    mapID = value.ID;
                else
                    mapID = 0;
            }
        }
        public Point Point { get; set; }

        public VisibleObject()
        {
            this.Point = new Point(0, 0);
        }

        public virtual bool WithinRange(VisibleObject vo, int max)
        {
            return WithinRange(vo, 0, max);
        }
        public virtual bool WithinRange(VisibleObject vo, int min, int max)
        {
            return ((vo != null) && (vo.Map == Map) && (vo.Point.DistanceFrom(Point) >= min) && (vo.Point.DistanceFrom(Point) <= max));
        }

        public virtual void Display() { }
        public virtual void DisplayTo(VisibleObject obj) { }
    }
}