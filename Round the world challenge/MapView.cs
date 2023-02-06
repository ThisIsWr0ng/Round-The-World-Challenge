using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;



namespace Round_the_world_challenge
{
    public partial class MapView : Form
    {
        private readonly Random random;
        private readonly Stopwatch stopWatch;
        private bool stopSignal = false;
        private int iteration = 0;
        private const int MapWidth = 40075;
        private double CostPerKm = 0.3;//Cost of traveliing one KM

        //initialize variables for continents and restrictions
        private Continent C = new Continent();
        private Continent[] continents = null;
        private PointF[,] restrictions = new PointF[0, 0];
        private bool keepMap = false;
        private int multiplier = 0;
        private int minContinentCities;

        //Variables for routes
        private City startLoc = new City(false);
        private City[] bestRoute;
        private double distance;
        private double profit;
        private List<double> profitList;
        private Graphics grap = null;
        private List<City[]> routes = new List<City[]>();
        private List<double[]> details = new List<double[]>();

        //Variables for Simmulated Annealing
        private double alpha = 0.940;
        private double temperature = 3810;
        private double epsilon = 5.450;

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

            //update simulated annealing track bar values
            tbAlpha.Value = (int)(alpha * 1000);
            tbTemp.Value = (int)temperature;
            tbEpsilon.Value = (int)(epsilon * 1000);
            splitContainer2.SplitterDistance = splitContainer2.Width;
           

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //Set simulated annealing settings
            temperature = tbTemp.Value;
            alpha = (double)tbAlpha.Value / 1000;
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

        private async void StartRace(int numCities, int numRestrConn)
        {
            //Reset previous run data
            Reset();

            //Start measuring time
            stopWatch.Start();
            //Generate new map and restrictions
            if (!keepMap || continents == null)
            {
                continents = C.CreateContinents(worldMap1.Width, worldMap1.Height, numCities, 2000, 20000); //Create continents and cities
                GenerateRestrictions(numRestrConn);
            }


            int startTemp = (int)temperature;
            int max = maxTotDistBar.Value;
            int min = minTotDistBar.Value;

            City[] cities = ExtractCities(continents, numCities);//Get the list of cities from continents
            if (!keepMap || startLoc.Location == PointF.Empty)//Select random starting point
                startLoc = cities[random.Next(cities.Length - 1)];
            GenerateRoute(cities);//Generate first route
            routes.Add(bestRoute);//Add it to the list of routes
            profit = ProfitCheck(bestRoute);//calculate initial profit;
            lblStartDist.Text = ((int)distance).ToString() + " KM"; //display initial distance


            //Draw it on a map
            await DisplayRoute(bestRoute);


            //await Task.Run(() => StartAnnealing(max, min));//Run in async
            StartAnnealing(max, min);


            UpdateTrackBar(routes.Count);//populate track bar with all routes
            DisplayRoute(bestRoute);
            stopWatch.Stop();
            lblTime.Text = stopWatch.ElapsedMilliseconds.ToString() + " ms";
            //UpdateDetails(iteration, temperature, 0, 0);//update final route details on gui
            btnStop.Visible = false;
            trkProgress.Enabled = true;

            double chk = CalcDistance(bestRoute);
            if (chk == -2) //If route is over limits
            {
                Font font = new Font("Times New Roman", 30, FontStyle.Bold);
                grap.DrawString("Route Invalid / Change Hop Distances", font, Brushes.Red, (int)Math.Floor(worldMap1.Width * 0.2), 13);
            }
            else if (chk == -1) //If route is over limits
            {
                Font font = new Font("Times New Roman", 30, FontStyle.Bold);
                grap.DrawString("Route Invalid / Used restricted connection", font, Brushes.Red, (int)Math.Floor(worldMap1.Width * 0.2), 13);
            }
            else if (chk == -3) //If route is over limits
            {
                Font font = new Font("Times New Roman", 30, FontStyle.Bold);
                grap.DrawString("Route Invalid / Total distance not met", font, Brushes.Red, (int)Math.Floor(worldMap1.Width * 0.2), 13);
            }
        }

        private async void StartAnnealing(int maxHop, int minHop)
        {
            bool restrictions = false;
            if (numRestrSelector.Value > 0 | chkHopEnabled.Checked | chkTotEnabled.Checked)
                restrictions = true;

            //while the temperature didnt reach epsilon
            while ((temperature >= epsilon) & !stopSignal)//Find better route
            {

                City[] best = bestRoute;
                double profitDelta = profit - ProfitCheck(best);//check if profit is higher once every 
                if (iteration > 40)
                {

                    if (best.Length > numRouteLengthMax.Value)//if route is longer than maximum route length
                        best = RemoveLeastProfitable(best, int.MaxValue);//remove least profitable city
                    else if(best.Length > numRouteLength.Value)//if route is longer than minimum route length
                        best = RemoveLeastProfitable(best, 0);//remove only cities with profit lower than 0
                }
                
                for (int i = 0; i < best.Length - 2; i++)
                {
                    
                    for (int j = i + 1; j < best.Length - 1; j++)
                    {
                        //Calculate delta of lengths between i and j to find crossed connections
                        double distDelta = TwoOptCheck(best, i, j);//check if distance is shorter   
                        

                        if (distDelta < 0 & profitDelta < 0)//If the route is shorter, and profit higher, then swap cities
                        {
                            best = ReverseSwap(best, i, j);
                            if (restrictions)
                            {
                                double check = CalcDistance(best);//calculate distance and check for restrictions
                                if (check < 0)
                                    continue;//return false if restrictions were not met
                                distance = check;//update the distance
                            }
                            distance += distDelta;
                            bestRoute = best;
                            //profit += -profitDelta;


                        }
                        else//if the distance and profit is worse
                        {
                            double proba = random.NextDouble();//calculate probability
                            double prob2 = Math.Exp(-distDelta / temperature);
                            if (proba < prob2)//swap cities if temperature is still high enough
                            {
                                best = ReverseSwap(best, i, j);
                                if (restrictions)
                                {
                                    double check = CalcDistance(best);//calculate distance and check for restrictions
                                    if (check < 0)
                                        continue;//return false if restrictions were not met
                                    distance = check;//update the distance
                                }
                                distance += distDelta;
                                bestRoute = best;
                                //profit += -profitDelta;
                                

                            }
                        }


                    }
                    if (chkPerform.Checked == false)//display live progress
                        if (i % 3 == 1)
                        {
                            await DisplayRoute(bestRoute);
                            UpdateDetails(iteration, temperature, profit, bestRoute.Length); //refresh labels
                        }
                           

                }
                //Log details
                routes.Add(bestRoute);
                double[] d = new double[] { (int)distance, temperature, iteration, profit, best.Length };
                details.Add(d);
                iteration++;//increase iteration
                temperature *= alpha;//Cooling down process
               

            }
            UpdateDetails(iteration, temperature, profit, bestRoute.Length); //refresh labels
        }

        private City[] RemoveLeastProfitable(City[] rt, int minProfit)
        {
            City[] route = rt;
            int index;
            City[] newRoute = new City[route.Length - 1];
            
            for (int p = 0; p < profitList.Count; p++)
            {
                int j = 0;
                route = rt;
                double lowestPr = profitList.Min();
                if (lowestPr > minProfit)//dont remove if city is over the profit limit
                    return rt;
                index = profitList.IndexOf(profitList.Find(x => x.Equals(lowestPr)));
                if (index == route.Length | index == 0)//dont remove starting point
                    profitList[index] = int.MaxValue;
                else
                {
                    for (int i = 0; i < newRoute.Length; i++)
                    {
                        if (i == index)
                            j++;
                        newRoute[i] = route[j];
                        j++;
                    }
                    if (CountContinentCities(newRoute) >= minContinentCities)
                        return newRoute;
                    else
                        profitList[index] = int.MaxValue;//make connection profitable if it cannot be removed due to restrictions
                }
            }
            return rt;//
            
        }
        private int CountContinentCities(City[] route)//count how many cities each continent have on the route
        {
            int[] count = new int[6];
            for (int i = 0; i < route.Length; i++)
            {
                count[route[i].Continent]++;
            }
            return count.Min();
        }

        private double ProfitCheck(City[] route)
        {
            double prof = 0;
            profitList = new List<double>();
            for (int i = 0; i < route.Length -1; i++)
            {
                profitList.Add(route[i + 1].Bid / CalcDistance(route[i].Location, route[i + 1].Location)*CostPerKm  );
                prof += profitList[i];
            }
            return prof;
        }


        private double TwoOptCheck(City[] route, int i, int j)
        {
            int n = route.Length;
            PointF iloc = route[i].Location;
            PointF jloc = route[j].Location;
            PointF ilocNext = route[(i + 1) ].Location;
            PointF jlocNext = route[(j + 1) ].Location;

            return CalcDistance(iloc, jloc) + CalcDistance(ilocNext, jlocNext) 
                   - CalcDistance(iloc, ilocNext) - CalcDistance(jloc, jlocNext);
        }
       
        private City[] ReverseSwap(City[] route, int start, int end)
        {
            City[] best = route;
            City[] newRoute = new City[best.Length];
            for (int j = 0; j <= start; j++)//add beginning of the route up to start of swap
            {
                newRoute[j] = best[j];
            }
            int c = 0;
            for (int j = start + 1; j <= end; j++)//add cities in reverse order up to end position
            {
                newRoute[j] = best[end - c];
                c++;
            }
            for (int j = end + 1; j < route.Length; j++)//add the rest of cities
            {
                newRoute[j] = best[j];
            }


            return newRoute;
        }

        private void DisplayLengths(int length, string txt)//displays lengths of hop and total distance restrictions under the map
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

        private void UpdateDetails(int iteration, double temperature, double profit, int length) //refresh Labels on GUI
        {

            lblIter.Text = iteration.ToString();
            lblTemp.Text = String.Format("{0:F2}", temperature);
            lblDistance.Text = Math.Floor(distance).ToString() + " KM";
            lblProfit.Text = ((int)profit).ToString();
            lblRouteLength.Text = length.ToString();
        }

        private void SyncDistancesToWindow()//update distances as window shrinks and expands
        {
            minHopDistanceBar.Maximum = MapWidth / 10;
            maxHopDistanceBar.Maximum = MapWidth;
            minTotDistBar.Maximum = MapWidth * multiplier;
            maxTotDistBar.Maximum = MapWidth * multiplier;

            minHopDistanceBar.Value = 300;
            maxHopDistanceBar.Value = (int)Math.Floor(MapWidth * 0.9);
            minTotDistBar.Value = (int)Math.Floor((MapWidth * multiplier) * 0.01);
            maxTotDistBar.Value = (int)Math.Floor((MapWidth * multiplier) * 0.9);
        }

        private async Task<int> DisplayRoute(City[] route)//async display for drawing
        {
            grap.Clear(Color.White);//clear panel

            List<Task<int>> tasks = new List<Task<int>>();//store tasks for async operation

            if (chkCities.Checked == true)
                tasks.Add(Task.Run(() => DisplayCities()));//Display cities as dots on a map
            if (chkNumbers.Checked == true)
                tasks.Add(Task.Run(() => DisplayCityNumbers(route)));//display numbers by each city
            if (chkDispCost.Checked == true)
                tasks.Add(Task.Run(() => DisplayCosts(route)));
            tasks.Add(Task.Run(() => DisplayLines(route)));//Display route connection
            tasks.Add(Task.Run(() => DisplayRestrictions()));//display restrictions

            var results = await Task.WhenAll(tasks);//Finish when all tasks are complete
            return 1;
        }

        private async Task<int> DisplayCosts(City[] route)
        {
            Font font = new Font("Times New Roman", 12, FontStyle.Bold);
            Graphics g = worldMap1.CreateGraphics();
            for (int i = 0; i < route.Length - 1; i++)
            {
                g.DrawString(route[i].Bid.ToString(), font, Brushes.Black, route[i].Location);
            }

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
                    g.DrawEllipse(pen, Convert.ToSingle(continents[i].Cities[j].Location.X) - 5, Convert.ToSingle(continents[i].Cities[j].Location.Y) - 5, 10, 10);
                }
            }

            //Display Start Point

            Pen pen2 = new Pen(Color.LimeGreen, 6);
            pen2.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            g.DrawEllipse(pen2, startLoc.Location.X - 10, startLoc.Location.Y - 10, 20, 20);
            return 1;
        }

        private async Task<int> DisplayCityNumbers(City[] route)
        {
            Font font = new Font("Times New Roman", 18, FontStyle.Bold);
            Graphics g = worldMap1.CreateGraphics();
            for (int i = 0; i < route.Length - 1; i++)
            {
                g.DrawString(i.ToString(), font, Brushes.Black, route[i].Location);
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

        private async Task<int> DisplayLines(City[] route)
        {
            //display current route
            Pen pen4 = new Pen(Color.Blue, 3);
            Graphics g = worldMap1.CreateGraphics();
            g.DrawLines(pen4, ExtractPointFArray(route));
            return 1;
        }
        private PointF[] ExtractPointFArray(City[] route)
        {
            PointF[] temp = new PointF[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                temp[i] = route[i].Location;
            }

            return temp;
        }

        private void GenerateRoute(City[] cities)
        {
            bool overTotAllowance = false;
            int tries = 0;
            int bestD = 999999999;
            do
            {
                distance = 0;
                overTotAllowance = false;

                bestRoute = new City[cities.Length + 1];
                bestRoute[0] = startLoc; //Assign start location to the first field
                City[] temp = cities;
                int[] check = new int[cities.Length];
                check[Array.IndexOf<City>(temp, startLoc)] = 1;//Mark start location as added to the route
                                                                 //
                for (int i = 1; i < bestRoute.Length; i++)
                {
                    City city = new City();
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
                        while (city.Location.IsEmpty);
                        bestRoute[i] = city;
                        check[Array.IndexOf<City>(temp, city)] = 1;
                    }
                    else
                    {
                        bestRoute[i] = startLoc;
                    }
                }
                // calculate distance
                distance = CalcDistance(bestRoute);

            } while (overTotAllowance);
        }

        private double CalcDistance(City[] route)//Calculate distance for the whole route and check for restrictions
        {

            double temp = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                
                double chk = CalcDistance(route[i].Location, route[i + 1].Location, maxHopDistanceBar.Value, minHopDistanceBar.Value);//Calculate the distance
                if (chk == -1)
                {
                    return -2;//return -2 for hop distance restrictions
                }
                else if(chk == -2)
                {
                    return -1;//return -1 for restricted connection
                }
                else
                    temp += chk;
            }
            if (iteration != 0 & chkTotEnabled.Checked == true & (temp > maxTotDistBar.Value || temp < minTotDistBar.Value))
                return -3;//return -3 if the whole route distance is longer than permitted

            return temp;
        }

        private double CalcDistance(PointF p1, PointF p2, int max, int min)//calculate distance with max and min hop restrictions
        {
            // Pythagoras
            double d = Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y))) * (MapWidth / worldMap1.Width);//Calculate distance and convert it to KM
            if (iteration != 0 & chkHopEnabled.Checked == true)
                if (d > max || d < min)
                    return -1;
            if(iteration != 0 & numRestrSelector.Value != 0)
                for (int i = 0; i < restrictions.GetLength(0); i++)
                {
                    if (restrictions[i, 0] == p1)
                        if (restrictions[i, 1] == p2)
                            return -2;

                }
            return d;
        }

        private double CalcDistance(PointF p1, PointF p2)
        {
            // Pythagoras
            double d = Math.Sqrt(((p2.X - p1.X) * (p2.X - p1.X)) + ((p2.Y - p1.Y) * (p2.Y - p1.Y))) * (MapWidth / worldMap1.Width);

            return d;
        }
     

        private City[] ExtractCities(Continent[] continents, int numCities)
        {
            
            List<City> a = new List<City>();
            City[] b = new City[numCities];

            for (int i = 0; i < continents.GetLength(0); i++)
            {
                for (int j = 0; j < continents[i].Cities.Length; j++)
                {
                    a.Add(continents[i].Cities[j]);
                }
            }//extract all cities from their continents to array
            for (int i = 0; i < numCities; i++)
            {
                int c = a.Min(X => X.Bid);
                int index = a.IndexOf(a.Find(x => x.Bid == c));
                b[i] = a[index];
                a.RemoveAt(index);

            }//find cheapest routes

            return b;
        }

        private void GenerateRestrictions(int numRestrConn)
        {
            restrictions = new PointF[numRestrConn, 2];
            for (int i = 0; i < numRestrConn; i++)
            {
                int rndCont = random.Next(continents.Length);
                restrictions[i, 0] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)].Location;
                rndCont = random.Next(continents.Length);
                restrictions[i, 1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)].Location;
                if (restrictions[i, 0] == restrictions[i, 1])
                {
                    rndCont = random.Next(continents.Length);
                    restrictions[i, 1] = continents[rndCont].Cities[random.Next(continents[rndCont].Cities.Length)].Location;
                }
            }
        }

        private void Reset()//Clear previous stuff
        {
            routes = new List<City[]>();
            stopWatch.Stop();
            stopWatch.Restart();
            btnStop.Visible = true;
            stopSignal = false;
            iteration = 0;
            trkProgress.Enabled = false;
            details = new List<double[]>();
            profit = 0;
            minContinentCities = (int)numMinContinents.Value;
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
            trkProgress.Maximum = num - 2;
            trkProgress.Value = num - 2;
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
            double a = (double)tbAlpha.Value / 1000;
            lblSAAlpha.Text = String.Format("{0:F3}", a);

            alpha = (double)tbAlpha.Value / 1000;
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
            UpdateDetails((int)details[trkProgress.Value][2],
            details[trkProgress.Value][1],
            details[trkProgress.Value][3],
            (int)details[trkProgress.Value][4]);
        }

        private void chkAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAdvanced.Checked)
            {
                splitContainer2.SplitterDistance = splitContainer2.Width - 160;
            }
            else
                splitContainer2.SplitterDistance = splitContainer2.Width;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void MapView_FormClosing(object sender, FormClosingEventArgs e)
        {
          
        }
    }
}