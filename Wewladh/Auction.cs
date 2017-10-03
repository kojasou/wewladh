using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wewladh
{
    public class Auction : GameObject
    {
        public Item Item { get; set; }
        public string Seller { get; set; }
        public long CurrentBid { get; set; }
        public long BuyoutPrice { get; set; }
        public string HighestBidder { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public override void Update()
        {
            if (DateTime.UtcNow > EndTime)
            {
                if (string.IsNullOrEmpty(HighestBidder))
                {
                    GameServer.SendParcel(new Parcel("Auction House", Item, 0), Seller);
                }
                else
                {
                    GameServer.SendParcel(new Parcel("Auction House", Item, 0), HighestBidder);
                    GameServer.SendParcel(new Parcel("Auction House", null, CurrentBid), Seller);
                }
                GameServer.RemoveGameObject(this);
            }
        }
    }
}