using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Round_the_world_challenge
{
    public partial class MapView : Form
    {
        private readonly Random random;
        private readonly Stopwatch stopWatch;
        private bool stopSignal = false;
        private int iteration = 0;
        private int totLoops = 0;
        private const int MapWidth = 40075;
        private const double PixelToKM = 2.645833E-7;


        //initialize variables for continents and restrictions
        private Continent C = new Continent();
        private DateTime startTime;
        private Continent[] continents = null;
        private PointF[,] restrictions = new PointF[0, 0];
        private bool keepMap = false;
        private int multiplier = 0;

        //Variables for routes
        private PointF startLoc = PointF.Empty;
        private PointF[] currentRoute;
        private PointF[] bestRoute;
        private double distance;
        private Graphics grap = null;
        private List<PointF[]> routes = new List<PointF[]>();
        private List<int[]> details = new List<int[]>();

        //Variables for Simmulated Annealing
        private double alpha = 0.98;
        private double temperature = 10;
        private double epsilon = 0.477;

        public MapView()
        {
            random = new Random();
            stopWatch = new Stopwatch();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //populate text fields
            multiplier = (int)numMult.Value;
            tbxMinHop.Text = minHopDistanceBar.Value.ToString();
            tbxMaxHop.Text = maxHopDistanceBar.Value.ToString();
            tbxMinTot.Text = minTotDistBar.Value.ToString();
            tbxMaxTot.Text = maxTotDistBar.Value.ToString();
            lblSAAlpha.Text = alpha.ToString();
            lblSATemp.Text = temperature.ToString();
            lblSAEpsilon.Text = epsilon.ToString();

            //Adjust distances to the window size
            SyncDistancesToWindow();

            //Make stop button visible
            btnStop.Visible = false;
            lblLengths.Visible = false;

            //select 2-OPT as default 
            cbxAlgo.SelectedIndex = 0;

            //update simulated annealing track bar values
            tbAlpha.Value = (int)(alpha * 100);
            tbTemp.Value = (int)temperature;
            tbEpsilon.Value = (int)(epsilon * 1000);
            
            
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Set simulated annealing settings
            temperature = tbTemp.Value;
            alpha = (double)tbAlpha.Value / 100;
            epsilon = (double)tbEpsilon.Value / 1000;
            //Keep the same map if chosen
            if (chkKeepMap.Checked == true)
            {
                keepMap = true;
            }
            else
                keepMap = false;
            //Prepare drawing
            grap = worldMap1.CreateGraphics();
            grap.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            grap.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            
            StartRace((int)numCitiesSelector.Value, (int)numRestrSelector.Value);
            
            
        }

        private void StartRace(int numCities, int numRestrConn)
        {
            //Reset previous run data
            Reset();
            
            //Start measuring time
            stopWatch.Start();
            //Generate new map and restrictions
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
            int startTemp = (int)temperature;
            double proba;
            double delta = 0;
            int noChange = 0; //tracking for how many iterations route was not updated
            int max = maxTotDistBar.Value;
            int min = minTotDistBar.Value;

            PointF[] cities = ExtractCities(continents, numCities);
            GenerateRoute(cities);//Generate first route
            routes.Add(bestRoute);//Add it to the list of routes

            lblStartDist.Text = ((int)distance).ToString() + " KM";
            //Draw it on a map
            await DisplayRoute(bestRoute);

            //while the temperature didnt reach epsilon
            while ((temperature >= epsilon /*&& noChange < 10*/) & !stopSignal)
            {
                //Perform selected algorithm
                if (cbxAlgo.SelectedIndex == 0)//perform 2-opt
                {
                    
                    await Task.Run(() => TwoOptCheck(max, min));//Run 2-Opt
                    RefreshDisplay(iteration, (int)temperature); //refresh labels

                }
                else if (cbxAlgo.SelectedIndex == 1)//Perforn 3-Opt
                {
                    //currentRoute = await Task.Run(() => ThreeOpt());
                    PointF[] best = bestRoute;
                    List<int[]> segments = GenSegments(best.Length);//Generate all possible segments for the route

                    foreach (var i in segments)//go through all the segments
                    {
                        if (stopSignal | temperature < epsilon)
                            break;
                        delta += ThreeOptSwap(best, i[0], i[1], i[2]);//Calculate and swap if route is better
                    }
                    delta = CalcDistance(currentRoute) - distance;
                    if (delta >= 0)
                    {
                        proba = random.NextDouble();
                        //if the new distance is worse accept it with certain probability
                        if (proba < Math.Exp(-delta / temperature))
                        {
                            distance = delta + distance;
                            bestRoute = currentRoute;
                            //noChange = 0;
                        }
                    }
                    else
                    {
                        bestRoute = currentRoute;
                        distance = delta + distance;
                        //noChange = 0;
                    }
                    //Display route every 200 iterations
                    if (chkPerform.Checked == false)
                        if (iteration % 200 == 1)
                            DisplayRoute(bestRoute);

                    //noChange++;
                    RefreshDisplay(iteration, (int)temperature);
                    routes.Add(bestRoute);
                    iteration++;
                    temperature *= alpha;//cooling proces on each iteration
                }
                else if (cbxAlgo.SelectedIndex == 2)
                {
                    currentRoute = ComputeNext();// perforn random route generation

                    
                    //compute the distance of the new permuted configuration
                    delta = CalcDistance(currentRoute) - distance;

                    if (delta < 0) //if the new distance is better accept it and assign it
                    {
                        bestRoute = currentRoute;
                        distance = delta + distance;
                        noChange = 0;
                        routes.Add(currentRoute);
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
                            routes.Add(currentRoute);
                        }
                    }
                    if (chkPerform.Checked == false)
                        if (iteration % 200 == 1)
                            DisplayRoute(bestRoute);
                    //cooling proces on each iteration
                    temperature *= alpha;
                    iteration++;
                    noChange++;
                    RefreshDisplay(iteration, (int)temperature);
                }
            }
            UpdateTrackBar(routes.Count);
            DisplayRoute(bestRoute);
            stopWatch.Stop();
            RefreshDisplay(iteration, (int)temperature);
            btnStop.Visible = false;
            trkProgress.Enabled = true;

            if (CalcDistance(bestRoute) == 99999999999999) //If route is over limits
            {
                Font font = new Font("Times New Roman", 30, FontStyle.Bold);
                grap.DrawString("Route Invalid / Change Hop Distances", font, Brushes.Red, (int)Math.Floor(worldMap1.Width * 0.2), 13);
            }
        }

        private void TwoOptCheck(int maxHop, int minHop)
        {
            PointF[] best = bestRoute;

            int n = best.Length;

            for (int i = 0; i < best.Length - 2; i++)
            {
                for (int k = i + 1; k < best.Length - 1; k++)
                {
                    //Calculate delta of lengths between i and k to find crossed connections
                    double delta = -CalcDistance(best[i], best[(i + 1) % n], maxHop, minHop) - CalcDistance(best[k], best[(k + 1) % n], maxHop, minHop)
                        + CalcDistance(best[i], best[k], maxHop, minHop) + CalcDistance(best[(i + 1) % n], best[(k + 1) % n], maxHop, minHop);
                    if (delta < 0)//If the route is shorter, then swap cities
                    {
                        best = ReverseSwap(best, i, k);
                        bestRoute = best;
                        distance += delta;
                        //Store details
                        routes.Add(best);
                        int[] d = new int[] {(int)distance, (int)temperature, iteration };
                        details.Add(d);
                    }
                    else//if the new distance is worse accept it with certain probability
                    {
                        double proba = random.NextDouble();

                        if (proba < Math.Exp(-delta / temperature))
                        {
                            best = ReverseSwap(best, i, k);
                            bestRoute = best;
                            distance += delta;
                            //Store details
                            routes.Add(best);
                            int[] d = new int[] { (int)distance, (int)temperature, iteration };
                            details.Add(d);
                        }
                    }
                    if (chkPerform.Checked == false)//Display route every 200 iterations if checkbox is checked
                        if (iteration % 200 == 1)
                            DisplayRoute(bestRoute);

                   
                }
                iteration++;
                temperature *= alpha;//Cooling down
            }
        }

        private double ThreeOptSwap(PointF[] routes, int i, int j, int k)
        {
            PointF[] route = routes;
            PointF A, B, C, D, E, F;
            A = route[i - 1];
            B = route[i];
            C = route[j - 1];
            D = route[j];
            E = route[k - 1];
            F = route[k % route.Length];
            double d0 = CalcDistance(A, B) + CalcDistance(C, D) + CalcDistance(E, F);
            double d1 = CalcDistance(A, C) + CalcDistance(B, D) + CalcDistance(E, F);
            double d2 = CalcDistance(A, B) + CalcDistance(C, E) + CalcDistance(D, F);
            double d3 = CalcDistance(A, D) + CalcDistance(E, B) + CalcDistance(C, F);
            double d4 = CalcDistance(F, B) + CalcDistance(C, D) + CalcDistance(E, A);

            if (d0 > d1)
            {
                route = ReverseSwapThreeOpt(route, i, j);
                currentRoute = route;
                return -d0 + d1;
            }
            else if (d0 > d2)
            {
                route = ReverseSwapThreeOpt(route, j, k);
                currentRoute = route;
                return -d0 + d2;
            }
            else if (d0 > d4)
            {
                route = ReverseSwapThreeOpt(route, i, k);
                currentRoute = route;
                return -d0 + d4;
            }
            else if (d0 > d3)
            {
                List<PointF> temp = new List<PointF>();
                //temp.Add(route[0]);//Add starting point
                for (int l = j; l < k; l++)//Add j-k
                {
                    temp.Add(route[l]);
                }
                for (int l = i; l < j; l++)//Add i - j
                {
                    temp.Add(route[l]);
                }
                for (int l = i; l < k; l++)
                {
                    route[l] = temp.First();
                    temp.RemoveAt(0);
                }

                currentRoute = route;
                return -d0 + d3;
            }

            currentRoute = routes;
            return 0;
        }

        private List<int[]> GenSegments(int length)
        {
            List<int[]> segments = new List<int[]>();

            for (int i = 1; i < length; i++)
            {
                for (int j = i + 2; j < length - 2; j++)
                {
                    for (int k = j + 2; k < length - 1; k++)
                    {
                        int[] l = { i, j, k };
                        segments.Add(l);
                    }
                }
            }

            return segments;
        }

        private PointF[] ReverseSwap(PointF[] route, int i, int k)
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

        private PointF[] ReverseSwapThreeOpt(PointF[] route, int i, int k)//Reverse for three opt - includes i
        {
            PointF[] best = route;
            PointF[] newRoute = new PointF[best.Length];
            for (int j = 0; j < i; j++)
            {
                newRoute[j] = best[j];
            }
            int c = 0;
            for (int j = i; j <= k; j++)
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

        private void DisplayLengths(int length, string txt)//displays lengths of hop and total distance restrictions
        {
            float len = Convert.ToSingle(length / (MapWidth / worldMap1.Width));
            lblLengths.Visible = true;
            lblLengths.Text = txt;
            var g = lengthsBox.CreateGraphics();
            g.Clear(Color.White);
            Pen pen = new Pen(Color.DarkRed, 8);
            g.DrawLine(pen, lengthsBox.Width / 2 - len / 2, lengthsBox.Height - 20, lengthsBox.Width / 2 + len / 2
                , lengthsBox.Height - 20);
            System.Threading.Thread.Sleep(100);
        }

        private void RefreshDisplay(int iteration, int temperature)
        {
            lblTime.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";
            lblIter.Text = iteration.ToString();
            lblTemp.Text = temperature.ToString();
            lblDistance.Text = Math.Floor(distance).ToString() + " KM";
        }

        private void SyncDistancesToWindow()
        {
            
            minHopDistanceBar.Maximum = MapWidth;
            maxHopDistanceBar.Maximum = MapWidth;
            minTotDistBar.Maximum = MapWidth * multiplier;
            maxTotDistBar.Maximum = MapWidth * multiplier;

            minHopDistanceBar.Value = 0;
            maxHopDistanceBar.Value = (int)Math.Floor(MapWidth * 0.9);
            minTotDistBar.Value = (int)Math.Floor((MapWidth * multiplier) * 0.01);
            maxTotDistBar.Value = (int)Math.Floor((MapWidth * multiplier) * 0.9);
        }

        private PointF[] ComputeNext()//Swap two random connections
        {
            PointF[] best = bestRoute;
            int n1 = 0;
            int n2;
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

        private async Task<int> DisplayRoute(PointF[] route)//async display for drawing
        {
            /* if (chkPerform.Checked == false)
                 await Task.Run(() => UpdateBackground());
             else*/
            grap.Clear(Color.White);

            //grap.Clear(Color.White);
            List<Task<int>> tasks = new List<Task<int>>();
            if (chkCities.Checked == true)
                tasks.Add(Task.Run(() => DisplayCities()));
            if (chkNumbers.Checked == true)
                tasks.Add(Task.Run(() => DisplayCityNumbers(route)));

            tasks.Add(Task.Run(() => DisplayLines(route)));
            tasks.Add(Task.Run(() => DisplayRestrictions()));

            var results = await Task.WhenAll(tasks);
            return 1;
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
            int bestD = 99999999;
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

                //Check if route is valid and display best distance found
                if (chkTotEnabled.Checked == true && distance > maxTotDistBar.Value || distance < minTotDistBar.Value)
                {
                    int c = (int)CalcDistanceAnyway(bestRoute);
                    if (CalcDistanceAnyway(bestRoute) < bestD)
                        bestD = c;
                    overTotAllowance = true;
                    tries++;
                    if (tries > 2000)
                    {
                        distance = CalcDistanceAnyway(bestRoute);
                        DisplayRoute(bestRoute);
                        MessageBox.Show($"Could not generate route within specified distances! Lowest distance was {Math.Floor(distance)}", "Unable to Complete Action!");
                        stopSignal = true;
                        return;
                    }
                }
            } while (overTotAllowance);
        }

        private double CalcDistance(PointF[] route)//Calculate distance for the whole route and check for restrictions
        {
            double temp = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                for (int r = 0; r < restrictions.GetLength(0); r++)
                {
                    if (numRestrSelector.Value != 0 && (restrictions[r, 0] == route[i] & restrictions[r, 1] == route[i + 1] || restrictions[r, 0] == route[i + 1] & restrictions[r, 1] == route[i]))
                    {
                        return 99999999999999;
                    }
                }
                double chk = CalcDistance(route[i], route[i + 1]);//Calculate the distance
                if (chkHopEnabled.Checked == true && (chk > maxHopDistanceBar.Value || chk < minHopDistanceBar.Value))
                {
                    return 99999999999999;//return an awfuly big number if any hop is longer or shorter than permitted
                }
                else
                    temp += chk;
            }
            if (chkTotEnabled.Checked == true && (temp > maxTotDistBar.Value || temp < minTotDistBar.Value))
                return 99999999999999;//return awfuly big number if the whole route distance is longer than permitted

            return temp;
        }

        private double CalcDistanceAnyway(PointF[] route)
        {
            double temp = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                temp += CalcDistance(route[i], route[i + 1]);//Calculate the distance
            }
            return temp;
        }

        private double CalcDistance(PointF p1, PointF p2, int max, int min)
        {
            // Pythagoras
            double d = Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y)))  *(MapWidth/ worldMap1.Width);
            if (chkHopEnabled.Checked == true)
                if (d > max || d < min)
                    return 9999999999999;
            return d;
        }
        private double CalcDistance(PointF p1, PointF p2)
        {
            // Pythagoras
            double d = Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y))) * (MapWidth / worldMap1.Width);
            if (chkHopEnabled.Checked == true)
                if (d > maxTotDistBar.Value || d < minTotDistBar.Value)
                    return 9999999999999;
            return d;
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
            routes = new List<PointF[]>();
            stopWatch.Stop();
            stopWatch.Restart();
            btnStop.Visible = true;
            stopSignal = false;
            startTime = DateTime.UtcNow;
            iteration = 0;
            totLoops = 0;
            trkProgress.Enabled = false;
            details = new List<int[]>();
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
            DisplayLengths(minHopDistanceBar.Value, "Minimum distance for a single flight");
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
            DisplayLengths(maxHopDistanceBar.Value, "Maximum distance for a single flight");
        }

        private void UpdateTrackBar(int num)
        {
            trkProgress.Maximum = num - 1;
            trkProgress.Value = num - 1;
        }

        private void minTotDistBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMinTot.Text = minTotDistBar.Value.ToString();
            DisplayLengths(minTotDistBar.Value / multiplier, "Minimum distance for a whole journey (* Multiplier)");
        }

        private void maxTotDistBar_ValueChanged(object sender, EventArgs e)
        {
            tbxMaxTot.Text = maxTotDistBar.Value.ToString();
            DisplayLengths(maxTotDistBar.Value / multiplier, "Maximum distance for a whole journey (* Multiplier)");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopSignal = true;
            btnStop.Visible = false;
        }

        private void numCitiesSelector_ValueChanged(object sender, EventArgs e)
        {
            chkKeepMap.Checked = false;
        }

        private void tbAlpha_ValueChanged(object sender, EventArgs e)
        {
            double a = (double)tbAlpha.Value / 100;
            lblSAAlpha.Text = String.Format("{0:F2}", a);

            alpha = (double)tbAlpha.Value / 100;
        }

        private void tbTemp_ValueChanged(object sender, EventArgs e)
        {
            lblSATemp.Text = tbTemp.Value.ToString();
            temperature = tbTemp.Value;
        }

        private void tbEpsilon_ValueChanged(object sender, EventArgs e)
        {
            double a = (double)tbEpsilon.Value / 1000;
            lblSAEpsilon.Text = String.Format("{0:F3}", a);
            epsilon = (double)tbEpsilon.Value / 1000;
        }

        private void minHopDistanceBar_MouseHover(object sender, EventArgs e)
        {
            lengthsBox.Visible = true;
            lblLengths.Visible = true;
            DisplayLengths(minHopDistanceBar.Value, "Minimum distance for a single flight");
        }

        private void minHopDistanceBar_MouseLeave(object sender, EventArgs e)
        {
            lengthsBox.Visible = false;
            lblLengths.Visible = false;
        }

        private void maxHopDistanceBar_MouseHover(object sender, EventArgs e)
        {
            lengthsBox.Visible = true;
            lblLengths.Visible = true;
            DisplayLengths(maxHopDistanceBar.Value, "Maximum distance for a single flight");
        }

        private void lengthsBox_MouseLeave(object sender, EventArgs e)
        {
            lengthsBox.Visible = false;
            lblLengths.Visible = false;
        }

        private void MapView_Resize(object sender, EventArgs e)
        {
            SyncDistancesToWindow();
        }

        private void minTotDistBar_MouseHover(object sender, EventArgs e)
        {
            lengthsBox.Visible = true;
            lblLengths.Visible = true;
            DisplayLengths(minTotDistBar.Value, "Minimum distance for a whole journey (* Multiplier)");
        }

        private void maxTotDistBar_MouseHover(object sender, EventArgs e)
        {
            lengthsBox.Visible = true;
            lblLengths.Visible = true;
            DisplayLengths(maxTotDistBar.Value, "Maximum distance for a whole journey (* Multiplier)");
        }

        private void minTotDistBar_MouseLeave(object sender, EventArgs e)
        {
            lengthsBox.Visible = false;
            lblLengths.Visible = false;
        }

        private void maxTotDistBar_MouseLeave(object sender, EventArgs e)
        {
            lengthsBox.Visible = false;
            lblLengths.Visible = false;
        }

        private void numMult_ValueChanged(object sender, EventArgs e)
        {
            multiplier = (int)numMult.Value;
            SyncDistancesToWindow();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
        }

        private void trkProgress_ValueChanged(object sender, EventArgs e)
        {
            if (trkProgress.Enabled == false)
                return;
            DisplayRoute(routes[trkProgress.Value]);
            lblDistance.Text = details[trkProgress.Value][0].ToString();
            lblTemp.Text = details[trkProgress.Value][1].ToString();
            lblIter.Text = details[trkProgress.Value][2].ToString();
        }
    }
}