using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Round_the_world_challenge
{
    public partial class MapView : Form
    {
        private readonly Random random;
        private readonly Stopwatch stopWatch;
        //initialize variables for continents and restrictions
        Continent C = new Continent();
        private DateTime startTime;
        private Continent[] continents = new Continent[6];
        private PointF[,] restrictions= new PointF[0,0];
        //Variables for routes
        private PointF startLoc;
        private PointF[] currentRoute;
        private PointF[] bestRoute;


        public MapView()
        {
            random = new Random();
            stopWatch = new Stopwatch();
            InitializeComponent();
   
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //populate text fields
            tbxMinHop.Text = minHopDistanceBar.Value.ToString();
            tbxMaxHop.Text = maxHopDistanceBar.Value.ToString();
            tbxMinTot.Text = minTotDistBar.Value.ToString();
            tbxMaxTot.Text = maxTotDistBar.Value.ToString();

        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            int numCities = (int)numCitiesSelector.Value;
            int minHop = (int)minHopDistanceBar.Value;
            int maxHop = (int)maxHopDistanceBar.Value;
            int numRestrConn = (int)numRestrSelector.Value;
            int minTot = (int)minTotDistBar.Value;
            int maxTot = (int)maxTotDistBar.Value;
     
            StartRace(numCities, minHop, maxHop, numRestrConn, minTot, maxTot);
        }

        private void StartRace(int numCities, int minHop, int maxHop, int numRestrConn, int minTot, int maxTot)
        {
            Reset();          
            
            startTime = DateTime.UtcNow;
            btnStart.Text = "Stop";
            btnStart.BackColor = Color.DarkRed;
            stopWatch.Start();
            
            continents = C.CreateContinents(worldMap.Width, worldMap.Height, numCities); //Create continents and cities
            GenerateRestrictions(numRestrConn);
            DisplayCities();
            DisplayRestrictions();
            SetStartPoint();
            StartAnnealing(numCities, minHop, maxHop, minTot, maxTot);

        }

        private void SetStartPoint()
        {
            //select starting point
            int strt = random.Next(continents.Length);
            startLoc = continents[strt].Cities[random.Next(continents[strt].Cities.Length)];
            //Draw it on a map
            var g = worldMap.CreateGraphics();
            Pen pen = new Pen(Color.LimeGreen, 6);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            g.DrawEllipse(pen, startLoc.X - 10, startLoc.Y - 10, 20,20);
        }

        private void StartAnnealing(int numCities, int minHop, int maxHop, int minTot, int maxTot)
        {
            int iteration = -1;
            double proba;
            double alpha = 0.999;
            double temperature = 400.0;
            double epsilon = 0.001;
            double delta;
            double distance;
            PointF[] cities = ExtractCities(continents, numCities);
            GenerateRoute(cities, minHop, maxHop, minTot, maxTot);
            DisplayRoute(currentRoute);
        }

        private void DisplayRoute(PointF[] route)
        {
            var g = worldMap.CreateGraphics();
            Pen pen = new Pen(Color.Blue, 3);
            
            
            g.DrawLines(pen, route);
            
        }

        private void GenerateRoute(PointF[] cities, int minHop, int maxHop, int minTot, int maxTot)
        {
            currentRoute = new PointF[cities.Length+1];
            currentRoute[0] = startLoc; //Assign start location to the first field
            PointF[] temp = cities;
            temp[Array.IndexOf<PointF>(temp, startLoc)] = PointF.Empty;//remove start location from array
            //
            for (int i = 1; i < currentRoute.Length; i++)
            {
                PointF city = PointF.Empty;
                //select one of available cities
                if(i < currentRoute.Length - 1)
                {
                    do
                        city = temp[random.Next(cities.Length)];
                    while (city == PointF.Empty);
                    currentRoute[i] = city;
                    temp[Array.IndexOf<PointF>(temp, city)] = PointF.Empty;
                }
                else
                {
                    currentRoute[i] = startLoc;
                }
                
            }
            // calculate distance

            //Check if route is valid
            
        }

        private PointF[] ExtractCities(Continent[] continents, int num)
        {
            int index = 0;
            PointF[] temp = new PointF[num];
            for (int i = 0; i < continents.GetLength(0); i++)
            {
                for (int  j = 0;  j < continents[i].Cities.Length;  j++)
                {
                    temp[index++] = continents[i].Cities[j];
                }
            }
            return temp;
        }

        private void DisplayRestrictions()
        {
            var g = worldMap.CreateGraphics();
            Pen pen = new Pen(Color.Red, 3);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//dashed line
            for (int i = 0; i < restrictions.GetLength(0); i++)
            {
                g.DrawLine(pen, restrictions[i, 0], restrictions[i, 1]);
            }
            
        }
       

        private void GenerateRestrictions(int numRestrConn)
        {
            restrictions = new PointF[numRestrConn,2];
            for (int i = 0; i < numRestrConn; i++)
            {
                int rndCont = random.Next(continents.Length);
                restrictions[i,0] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                rndCont = random.Next(continents.Length);
                restrictions[i,1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                if (restrictions[i,0] == restrictions[i, 1])
                {
                    rndCont = random.Next(continents.Length);
                    restrictions[i, 1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                }
            }
        }

        private void Reset()//Clear previous stuff
        {
            stopWatch.Stop();
            

        }
        private void NextRoute()
        {
   
        }
        private void DisplayCities()
        {
            
            Color[] contColours = {Color.DarkBlue, Color.DarkGreen, Color.DarkRed, Color.Orange, Color.DarkMagenta, Color.Black };
            var g = worldMap.CreateGraphics();
           //Draw cities on the map
            for (int i = 0; i < continents.Length; i++)
            {
                Pen pen = new Pen(contColours[i], 4);//Different colours for each continent
                //List<City> list = new List<City>();
                for (int j = 0; j < continents[i].Cities.Length; j++)
                {

                    g.DrawEllipse(pen, Convert.ToSingle(continents[i].Cities[j].X) -5, Convert.ToSingle(continents[i].Cities[j].Y) -5, 10, 10);
                }

            }
            



        }
   
        private void minHopDistanceBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMinHop.Text = minHopDistanceBar.Value.ToString();
            if(minHopDistanceBar.Value >= maxHopDistanceBar.Value)
            {
                btnStart.Enabled = false;
                tbxMinHop.BackColor= Color.Red;
                tbxMaxHop.BackColor= Color.Red;
            }
            else
            {
                btnStart.Enabled = true;
                tbxMinHop.BackColor = Color.White;
                tbxMaxHop.BackColor = Color.White;
            }
        }

        private void maxHopDistanceBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMaxHop.Text = maxHopDistanceBar.Value.ToString();
            if (minHopDistanceBar.Value >= maxHopDistanceBar.Value)
            {
                btnStart.Enabled = false;
                tbxMinHop.BackColor = Color.Red;
                tbxMaxHop.BackColor = Color.Red;
            }
            else
            {
                btnStart.Enabled = true;
                tbxMinHop.BackColor = Color.White;
                tbxMaxHop.BackColor = Color.White;
            }
        }

        private void minTotDistBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMinTot.Text = minTotDistBar.Value.ToString();
            
        }

        private void maxTotDistBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMaxTot.Text = maxTotDistBar.Value.ToString();
        }
    }
}
