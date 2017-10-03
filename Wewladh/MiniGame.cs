using System.Collections.Generic;

namespace Wewladh
{
    public abstract class MiniGame
    {
        public int ID { get; protected set; }
        public List<string> Rewards { get; private set; }
        public List<string> RequiredItem { get; private set; }
        public List<string> RequiredWeapon { get; private set; }
        public abstract void OnWin(Player p);
        public abstract void OnLose(Player p);
        public MiniGame()
        {
            this.Rewards = new List<string>();
            this.RequiredItem = new List<string>();
            this.RequiredWeapon = new List<string>();
        }
    }

    public class InsectMiniGame : MiniGame
    {
        public InsectMiniGame()
        {
            this.Rewards.Add("Item_Megaphone");
            this.RequiredItem.Add("Item_Sugar");
            this.RequiredWeapon.Add("Item_Net_Lev1");
        }
        public override void OnWin(Player p)
        {
            if (Program.Random(2) == 1)
            {
                int index = Program.Random(Rewards.Count);
                var reward = p.GameServer.CreateItem(Rewards[index]);
                if (reward != null)
                {
                    p.AddItem(reward);
                    p.Client.SendMessage(string.Format("[Insect Collection] You received {0}!", reward.Name));
                }
            }
            else
            {
                p.Client.SendMessage(string.Format("[Insect Collection] The insect got away. You earned nothing."));
            }
        }
        public override void OnLose(Player p)
        {
            p.Client.SendMessage(string.Format("[Insect Collection] You missed."));
        }
    }
}