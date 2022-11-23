using Round_the_world_challenge.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Round_the_world_challenge
{
    public partial class MapView : Form
    {
        private readonly Random random;
        private readonly Stopwatch stopWatch;
        //initialize variables for continents and restrictions
        Continent C = new Continent();
        private DateTime startTime;
        private Continent[] continents = null;
        private PointF[,] restrictions= new PointF[0,0];
        private bool keepMap = false;
        //Variables for routes
        private PointF startLoc = PointF.Empty;
        private PointF[] currentRoute;
        private PointF[] bestRoute;
        double distance;
        private Graphics grap = null;

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
            if (chkKeepMap.Checked == true)
            {
                keepMap = true;
            }
            else
                keepMap = false;
            grap = worldMap.CreateGraphics();
            StartRace(numCities, numRestrConn);
        }

        private void StartRace(int numCities, int numRestrConn)
        {
            Reset();          
            
            startTime = DateTime.UtcNow;
            btnStart.Text = "Stop";
            btnStart.BackColor = Color.DarkRed;
            stopWatch.Start();
            if(!keepMap || continents == null)
            {
                continents = C.CreateContinents(worldMap.Width, worldMap.Height, numCities); //Create continents and cities
                GenerateRestrictions(numRestrConn);
            }
            if (!keepMap || startLoc == PointF.Empty)
            {
                //select starting point
                int strt = random.Next(continents.Length);
                startLoc = continents[strt].Cities[random.Next(continents[strt].Cities.Length)];
            }
            StartAnnealing(numCities);

        }

       

        private void StartAnnealing(int numCities)
        {
            int iteration = -1;
            double proba;
            double alpha = 0.999;
            double temperature = 1200.0;
            double epsilon = 0.001;
            double delta = 0;
            
            PointF[] cities = ExtractCities(continents, numCities);
            GenerateRoute(cities);
            
            //Draw it on a map
            DisplayRoute(bestRoute);
            //while the temperature didnt reach epsilon
            while (temperature > epsilon )
            {
                
                iteration++;

                //get the next random permutation of distances 
                currentRoute = ComputeNext();
                //compute the distance of the new permuted configuration
                delta = CalcDistance(currentRoute) - distance;
                //if the new distance is better accept it and assign it
                if (delta < 0)
                {
                    bestRoute = currentRoute;
                    distance = delta + distance;
                }

                else
                {
                    proba = random.Next();
                    //if the new distance is worse accept it but with a probability level
                    // if the probability is less than E to the power -delta/temperature.
                    //otherwise the old value is kept
                    if (proba < Math.Exp(-delta / temperature))
                    {
                        bestRoute = currentRoute;
                        distance = delta + distance;
                    }
                }
                //cooling proces on every iteration
                temperature *= alpha;
                //print every 400 iterations
                if (iteration % 400 == 0)
                {
                    RefreshDisplay(iteration, temperature, delta);
                    DisplayRoute(bestRoute);
                }
                    
            }
        }

        private void RefreshDisplay(int iteration, double temperature, double delta)
        {
            lblDelta.Text = delta.ToString();
            lblIter.Text = iteration.ToString();
            lblTemp.Text = temperature.ToString();
            lblDistance.Text = distance.ToString();
        }

        private PointF[] ComputeNext()//Swap two random connections
        {
            PointF[] best = bestRoute;
            int n1 = 0;
            int n2 = 0;
            do
            {
                n1 = random.Next(1, best.Length -1);
                n2 = random.Next(1, best.Length -1);
            } while (n1 == n2);
            PointF tmp;
            tmp = best[n1];
            best[n1] = best[n2];
            best[n2] = tmp;
            return best;
        }

        private void DisplayRoute(PointF[] route)
        {
            worldMap.Invalidate();
        }

        private void GenerateRoute(PointF[] cities)
        {
            bool overTotAllowance = false;
            int tries = 0;
            do
            {
                distance = 0;
                overTotAllowance = false;

                bestRoute = new PointF[cities.Length + 1];
                bestRoute[0] = startLoc; //Assign start location to the first field
                PointF[] temp = cities;
                int[] check = new int[cities.Length];
                check[Array.IndexOf<PointF>(temp, startLoc)] = 1;//Mark start location as added to the route
                                                                           //
                for (int i = 1; i < bestRoute.Length; i++)
                {
                    PointF city = PointF.Empty;
                    //select one of available cities
                    if (i < bestRoute.Length - 1)
                    {
                        do
                        {
                            int k = random.Next(cities.Length);
                            if (check[k] == 1)
                                continue;
                            city = temp[k];
                        }//city = temp[random.Next(cities.Length)];
                        while (city.IsEmpty);
                        bestRoute[i] = city;
                        check[Array.IndexOf<PointF>(temp, city)] = 1;
                    }
                    else
                    {
                        bestRoute[i] = startLoc;
                    }

                }
                // calculate distance
                distance = CalcDistance(bestRoute);


                //Check if route is valid
                if(distance > maxTotDistBar.Value || distance < minTotDistBar.Value)
                    overTotAllowance = true;
                tries++;
                if(tries > 2000)
                {
                    DisplayRoute(bestRoute);
                    MessageBox.Show($"Could not generate route within specified distances! distance= {distance}", "Unable to Complete Action!");
                    break;
                    
                }
            } while (overTotAllowance || distance == -1);
        }

        private double CalcDistance(PointF[] currentRoute)//Calculate distance between points
        {
            double temp = 0;
            for (int i = 0; i < currentRoute.Length - 1; i++)
            {
                double chk = CalcDistance(currentRoute[i], currentRoute[i + 1]);
                if (chk > maxHopDistanceBar.Value || chk < minHopDistanceBar.Value)
                {
                    return 9999999999;//return -1 if any hop is longer than permitted
                }
                else
                    temp += chk;
            }
            return temp;
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
     
        private static double CalcDistance(PointF p1, PointF p2) =>
             // Pythagoras
             Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y)));


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

        private void worldMap_Paint(object sender, PaintEventArgs e)
        {
            if (currentRoute == null)
                return;

            Color[] contColours = { Color.DarkBlue, Color.DarkGreen, Color.DarkRed, Color.Orange, Color.DarkMagenta, Color.Black };

            //Draw cities on the map
            for (int i = 0; i < continents.Length; i++)
            {
                Pen pen = new Pen(contColours[i], 4);//Different colours for each continent
                //List<City> list = new List<City>();
                for (int j = 0; j < continents[i].Cities.Length; j++)
                {

                    grap.DrawEllipse(pen, Convert.ToSingle(continents[i].Cities[j].X) - 5, Convert.ToSingle(continents[i].Cities[j].Y) - 5, 10, 10);
                }

            }
            //Display Start Point
          

            Pen pen2 = new Pen(Color.LimeGreen, 6);
            pen2.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            grap.DrawEllipse(pen2, startLoc.X - 10, startLoc.Y - 10, 20, 20);
            //Display restrictions
            Pen pen3 = new Pen(Color.Red, 3);
            pen3.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//dashed line
            for (int i = 0; i < restrictions.GetLength(0); i++)
            {
                grap.DrawLine(pen3, restrictions[i, 0], restrictions[i, 1]);
            }
            //display current route
            Pen pen4 = new Pen(Color.Blue, 3);
            grap = worldMap.CreateGraphics();
            grap.DrawLines(pen4, bestRoute);
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }
    }
}
