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
        Continent C = new Continent();
        private readonly Stopwatch stopWatch;
        private readonly List<long> avgList;
        private DateTime startTime;
        private Continent[] continents = new Continent[6];
        private City[,] restrictions= new City[0,0];


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
     
            StartRace(numCities, minHop, maxHop, numRestrConn);
        }

        private void StartRace(int numCities, int minHop, int maxHop, int numRestrConn)
        {
            Reset();          
            
            startTime = DateTime.UtcNow;
            btnStart.Text = "Stop";
            btnStart.BackColor = Color.DarkRed;
            stopWatch.Start();
            
            continents = C.CreateContinents(worldMap.Width, worldMap.Height, numCities); //Create continents and cities
            GenerateRestrictions(numRestrConn);
            DisplayCities();
            NextRoute();

        }

        private void GenerateRestrictions(int numRestrConn)
        {
            restrictions = new City[numRestrConn,2];
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

                    g.DrawEllipse(pen, Convert.ToSingle(continents[i].Cities[j].X), Convert.ToSingle(continents[i].Cities[j].Y), 10, 10);
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
