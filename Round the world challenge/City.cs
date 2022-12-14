using System.Drawing;

namespace Round_the_world_challenge
{
     class City
    {
        private PointF location = new PointF() { X = 0, Y = 0 };
        private int cost = 0;

        public PointF Location
        {
            get { return location; }
            set { location = new PointF(value.X, value.Y); }
        }
        public int Cost
        {
            get { return cost; }
            set { cost = value; }
        }
        public City()
        {

        }
        public City(bool t)
        {
            location = PointF.Empty;
            cost = 0;
        }
        public City(PointF loc, int c)
        {
            this.location = loc;
            this.cost = c;
        }
        public City CreateEmpty()
        {
            Location = PointF.Empty;
            Cost = 0;
            return this;
        }
    }
}
