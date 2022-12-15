using System;
using System.Drawing;


namespace Round_the_world_challenge
{
     class Continent
    {
        private double minX { get; set; }
        private double minY { get; set; }
        private double maxX { get; set; }
        private double maxY { get; set; }
        public City[] Cities { get; set; }


        public Continent()
        {
        }

        public Continent[] CreateContinents(int mapWidth, int mapHeight, int cities)
        {
            Random random = new Random();
            int numContinents = 6;
            Continent[] continents = new Continent[numContinents];
            int[] numCities = new int[numContinents];
            int citiesLeft = cities;

            for (int i = 0; i < numContinents; i++)//Distribute cities across the continents
            {
                numCities[i] = random.Next(1, (cities / 6));
                citiesLeft -= numCities[i];
                if (i == numContinents - 1 && citiesLeft > 0)
                {
                    for (int j = 0; j < citiesLeft; j++)
                    {
                        numCities[random.Next(0, 6)]++;
                    }
                    citiesLeft = 0;
                }
            }

            //create continent borders and add random cities
            for (int i = 0; i < numContinents; i++)
            {
                Continent cont = new Continent();
                if (i == 0)//North America
                {
                    cont.minX = mapWidth * 0.08;
                    cont.maxX = mapWidth * 0.21;
                    cont.minY = mapHeight * 0.4;
                    cont.maxY = mapHeight * 0.1;
                }
                else if (i == 1)//South America
                {
                    cont.minX = mapWidth * 0.2;
                    cont.maxX = mapWidth * 0.3;
                    cont.minY = mapHeight * 0.9;
                    cont.maxY = mapHeight * 0.5;
                }
                else if (i == 2)//Europe
                {
                    cont.minX = mapWidth * 0.42;
                    cont.maxX = mapWidth * 0.54;
                    cont.minY = mapHeight * 0.3;
                    cont.maxY = mapHeight * 0.1;
                }
                else if (i == 3)//Africa
                {
                    cont.minX = mapWidth * 0.44;
                    cont.maxX = mapWidth * 0.56;
                    cont.minY = mapHeight * 0.82;
                    cont.maxY = mapHeight * 0.34;
                }
                else if (i == 4)//Asia
                {
                    cont.minX = mapWidth * 0.57;
                    cont.maxX = mapWidth * 0.83;
                    cont.minY = mapHeight * 0.49;
                    cont.maxY = mapHeight * 0.07;
                }
                else//Australasia
                {
                    cont.minX = mapWidth * 0.79;
                    cont.maxX = mapWidth * 0.94;
                    cont.minY = mapHeight * 0.88;
                    cont.maxY = mapHeight * 0.53;
                }
                //Add Cities
                cont.Cities = new City[numCities[i]];
                for (int j = 0; j < numCities[i]; j++)
                {
                    cont.Cities[j] = new City();
                }

                for (int j = 0; j < numCities[i]; j++)
                {
                    PointF city = new PointF(
                        Convert.ToSingle((random.NextDouble() * (cont.maxX - cont.minX) + cont.minX)),
                        Convert.ToSingle((random.NextDouble() * (cont.maxY - cont.minY) + cont.minY))
                        );
               
                    
                    cont.Cities[j].Bid = random.Next(2000, 10000);
                    cont.Cities[j].Location = city;
                    cont.Cities[j].Continent = i;
                }
                continents[i] = cont;
            }
            return continents;
        }

        public City[] GetCities()
        {
            return Cities;
        }
    }
}