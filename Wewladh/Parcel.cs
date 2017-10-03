
namespace Wewladh
{
    public class Parcel
    {
        public string Sender { get; set; }
        public Item Item { get; set; }
        public long Gold { get; set; }
        public Parcel(string sender, Item item, long gold)
        {
            this.Sender = sender;
            this.Item = item;
            this.Gold = gold;
        }
    }
}