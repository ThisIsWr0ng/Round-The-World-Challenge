using Round_the_world_challenge.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
        private bool stopSignal = false;
        //initialize variables for continents and restrictions
        Continent C = new Continent();
        private DateTime startTime;
        private Continent[] continents = null;
        private PointF[,] restrictions = new PointF[0, 0];
        private bool keepMap = false;
        //Variables for routes
        private PointF startLoc = PointF.Empty;
        private PointF[] currentRoute;
        private PointF[] bestRoute;
        double distance;
        private Graphics grap = null;
        //Variables for Simmulated Annealing
        private double alpha = 0.95;
        private double temperature = 400.0;
        private double epsilon = 0.001;


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
            lblSAAlpha.Text = alpha.ToString();
            lblSATemp.Text = temperature.ToString();
            lblSAEpsilon.Text = epsilon.ToString();
            btnStop.Visible = false;

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
            grap = worldMap1.CreateGraphics();
            StartRace(numCities, numRestrConn);
        }

        private void StartRace(int numCities, int numRestrConn)
        {
            Reset();
            btnStop.Visible = true;
            stopSignal = false;
            startTime = DateTime.UtcNow;

            stopWatch.Start();
            if (!keepMap || continents == null)
            {
                continents = C.CreateContinents(worldMap1.Width, worldMap1.Height, numCities); //Create continents and cities
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



        private async void StartAnnealing(int numCities)
        {
            int iteration = -1;
            double proba;
            double delta;
            int noChange = 0; //tracking for how many iterations route was not updated


            PointF[] cities = ExtractCities(continents, numCities);
            GenerateRoute(cities);

            lblStartDist.Text = distance.ToString();
            //Draw it on a map
            await DisplayRoute(bestRoute);

            //while the temperature didnt reach epsilon
            while (temperature > epsilon || noChange < 50 && !stopSignal)
            {
                RefreshDisplay(iteration, temperature);
                if (temperature < epsilon && noChange > 50 || stopSignal)
                {
                    btnStop.Visible = false;
                    stopSignal = false;
                    return;
                }
                iteration++;
                noChange++;

                //perform 2-opt
                currentRoute = TwoOptChk();
                //compute the distance of the new permuted configuration
                delta = CalcDistance(currentRoute) - distance;

               
                if (delta == 0)//don't update the screen if there's no need for it
                {

                }
                else if (delta < 0) //if the new distance is better accept it and assign it
                {
                    grap.Clear(Color.Transparent);
                    bestRoute = currentRoute;
                    distance = delta + distance;
                    noChange = 0;
                    if(chkPerform.Checked == false)
                        await Task.Run(() => DisplayRoute(bestRoute));
                    else
                        DisplayRoute(bestRoute);
                }
                else
                {
                    proba = random.NextDouble();
                    //if the new distance is worse accept it with certain probability
                    if (proba < Math.Exp(-delta / temperature))
                    {
                        bestRoute = currentRoute;
                        distance = delta + distance;
                        noChange = 0;
                        await Task.Run(() => DisplayRoute(bestRoute));

                    }
                }
                //cooling proces on every iteration
                temperature *= alpha;



                //}
                //}
            }
            stopWatch.Stop();
            RefreshDisplay(iteration, temperature);
            btnStop.Visible = false;
            

        }


        private PointF[] TwoOpt(PointF[] route, int i, int k)
        {
            PointF[] best = route;
            PointF[] newRoute = new PointF[best.Length];
            for (int j = 0; j <= i; j++)
            {
                newRoute[j] = best[j];
            }
            int c = 0;
            for (int j = i + 1; j <= k; j++)
            {
                newRoute[j] = best[k - c];
                c++;
            }
            for (int j = k + 1; j < bestRoute.Length; j++)
            {
                newRoute[j] = best[j];
            }

            return newRoute;
        }
        private PointF[] TwoOptChk()
        {
            PointF[] best = bestRoute;

            int n = best.Length;


            for (int i = 0; i < best.Length - 2; i++)
            {
                for (int k = i + 1; k < best.Length - 1; k++)
                {
                    //double lengthDelta = - CalcDistance(best[i], best[i+1]) - CalcDistance(best[k], best[k+1]) + CalcDistance(best[i+1], best[k+1]) + CalcDistance(best[1], best[k]);
                    double lengthDelta = -CalcDistance(best[i], best[(i + 1) % n]) - CalcDistance(best[k], best[(k + 1) % n]) + CalcDistance(best[i], best[k]) + CalcDistance(best[(i + 1) % n], best[(k + 1) % n]);
                    if (lengthDelta < 0)
                    {
                        //distance += lengthDelta;

                        best = TwoOpt(best, i, k);
                        // DisplayRoute(best);

                    }
                }
            }



            return best;

        }


        private void RefreshDisplay(int iteration, double temperature)
        {
            lblTime.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";
            lblIter.Text = iteration.ToString();
            lblTemp.Text = Math.Floor(temperature).ToString();
            lblDistance.Text = Math.Floor(distance).ToString();

        }

        private PointF[] ComputeNext()//Swap two random connections
        {
            PointF[] best = bestRoute;
            int n1 = 0;
            int n2 = 0;
            do
            {
                n1 = random.Next(1, best.Length - 1);
                n2 = random.Next(1, best.Length - 1);
            } while (n1 == n2);
            PointF tmp;
            tmp = best[n1];
            best[n1] = best[n2];
            best[n2] = tmp;
            return best;
        }

        private async Task<int> DisplayRoute(PointF[] route)
        {
            //worldMap1.BackgroundImage = Resources.World_Map;

            grap.Clear(Color.White);





            List<Task<int>> tasks = new List<Task<int>>();
            if(chkCities.Checked == true)
                tasks.Add(Task.Run(() => DisplayCities()));
            if(chkNumbers.Checked == true)
                tasks.Add(Task.Run(() => DisplayCityNumbers(route)));
            tasks.Add(Task.Run(() => DisplayRestrictions()));
            tasks.Add(Task.Run(() => DisplayLines(route)));

            var results = await Task.WhenAll(tasks);
            return 1;


            //signal.Release();
            //worldMap1.Invalidate();
        }
        private async Task<int> DisplayCities()
        {
            Color[] contColours = { Color.DarkBlue, Color.DarkGreen, Color.DarkRed, Color.Orange, Color.DarkMagenta, Color.Black };
            Graphics g = worldMap1.CreateGraphics();

            //Draw cities on the map
            for (int i = 0; i < continents.Length; i++)
            {
                Pen pen = new Pen(contColours[i], 4);//Different colours for each continent
                //List<City> list = new List<City>();
                for (int j = 0; j < continents[i].Cities.Length; j++)
                {
                    g.DrawEllipse(pen, Convert.ToSingle(continents[i].Cities[j].X) - 5, Convert.ToSingle(continents[i].Cities[j].Y) - 5, 10, 10);

                }

            }

            //Display Start Point


            Pen pen2 = new Pen(Color.LimeGreen, 6);
            pen2.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            g.DrawEllipse(pen2, startLoc.X - 10, startLoc.Y - 10, 20, 20);
            return 1;
        }
        private async Task<int> DisplayCityNumbers(PointF[] route)
        {
            Font font = new Font("Times New Roman", 18, FontStyle.Bold);
            Graphics g = worldMap1.CreateGraphics();
            for (int i = 0; i < route.Length - 1; i++)
            {
                g.DrawString(i.ToString(), font, Brushes.Black, route[i]);
            }


            return 1;
        }
        private async Task<int> DisplayRestrictions()
        {
            //Display restrictions
            if (restrictions == null)
                return 1;
            Graphics g = worldMap1.CreateGraphics();
            Pen pen3 = new Pen(Color.Red, 3);
            pen3.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//dashed line
            for (int i = 0; i < restrictions.GetLength(0); i++)
            {
                g.DrawLine(pen3, restrictions[i, 0], restrictions[i, 1]);
            }
            return 1;
        }
        private async Task<int> DisplayLines(PointF[] route)
        {
            //display current route
            Pen pen4 = new Pen(Color.Blue, 3);
            Graphics g = worldMap1.CreateGraphics();
            g.DrawLines(pen4, route);
            return 1;
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
                if (distance > maxTotDistBar.Value || distance < minTotDistBar.Value)
                    overTotAllowance = true;
                tries++;
                if (tries > 2000)
                {
                    DisplayRoute(bestRoute);
                    MessageBox.Show($"Could not generate route within specified distances! distance= {distance}", "Unable to Complete Action!");
                    break;

                }
            } while (overTotAllowance || distance == -1);
        }

        private double CalcDistance(PointF[] route)//Calculate distance between points
        {
            double temp = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                double chk = CalcDistance(route[i], route[i + 1]);
                /*if (chk > maxHopDistanceBar.Value || chk < minHopDistanceBar.Value)
                {
                    return 9999999999;//return -1 if any hop is longer than permitted
                }
                else*/
                temp += chk;
            }
            return temp;
        }
        private static double CalcDistance(PointF p1, PointF p2) =>
     // Pythagoras
     Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y)) / 10);
        private double QuickDist(PointF[] route)
        {
            double temp = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                temp += CalcDistance(route[i], route[i + 1]);

            }
            return temp;
        }

        private PointF[] ExtractCities(Continent[] continents, int num)
        {
            int index = 0;
            PointF[] temp = new PointF[num];
            for (int i = 0; i < continents.GetLength(0); i++)
            {
                for (int j = 0; j < continents[i].Cities.Length; j++)
                {
                    temp[index++] = continents[i].Cities[j];
                }
            }
            return temp;
        }

        private void GenerateRestrictions(int numRestrConn)
        {
            restrictions = new PointF[numRestrConn, 2];
            for (int i = 0; i < numRestrConn; i++)
            {
                int rndCont = random.Next(continents.Length);
                restrictions[i, 0] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                rndCont = random.Next(continents.Length);
                restrictions[i, 1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                if (restrictions[i, 0] == restrictions[i, 1])
                {
                    rndCont = random.Next(continents.Length);
                    restrictions[i, 1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)];
                }
            }
        }

        private void Reset()//Clear previous stuff
        {
            stopWatch.Stop();
            stopWatch.Restart();



        }

        private void minHopDistanceBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMinHop.Text = minHopDistanceBar.Value.ToString();
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

        private async void worldMap_Paint(object sender, PaintEventArgs e)
        {
            if (bestRoute == null)
                return;
        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopSignal = true;
            btnStop.Visible = false;

        }

        private async void worldMap1_Paint(object sender, PaintEventArgs e)
        {
            if (bestRoute == null)
                return;

        }

        private void numCitiesSelector_ValueChanged(object sender, EventArgs e)
        {
            chkKeepMap.Checked = false;
        }

        private void lblIter_Click(object sender, EventArgs e)
        {

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

        }

        private void tbAlpha_ValueChanged(object sender, EventArgs e)
        {
            double a  = (double)tbAlpha.Value / 100;
            lblSAAlpha.Text = String.Format("{0:F2}", a);


            alpha = (double)tbAlpha.Value /100;
        }

        private void tbTemp_ValueChanged(object sender, EventArgs e)
        {
            lblSATemp.Text= tbTemp.Value.ToString();
            temperature= tbTemp.Value;
        }

        private void tbEpsilon_ValueChanged(object sender, EventArgs e)
        {
            double a = (double)tbEpsilon.Value / 1000;
            lblSAEpsilon.Text = String.Format("{0:F3}", a);
            epsilon = (double)tbEpsilon.Value /1000;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
