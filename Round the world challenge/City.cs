using System.Drawing;

namespace Round_the_world_challenge
{
     class City
    {
        
        public PointF Location { get; set; }
        public int Bid { get; set; }
        public int Continent { get; set; }
        public City()
        {

        }
        public City(bool t)
        {
            Location = PointF.Empty;
            Bid = 0;
        }
        public City(PointF loc, int c)
        {
            this.Location = loc;
            this.Bid = c;
        }
        public City CreateEmpty()
        {
            Location = PointF.Empty;
            Bid = 0;
            return this;
        }
    }
}
