using System;
using System.Drawing;

namespace Round_the_world_challenge
{
    public class Continent
    {
        string Name { get; set; }
        double minX { get; set; }
        double minY { get; set; }
        double maxX { get; set; }
        double maxY { get; set; }
        public PointF[] Cities { get; set; }


        public Continent()
        {

        }
        public Continent[] CreateContinents(int mapWidth, int mapHeight, int cities)
        {
            Random random = new Random();
            string[] continentNames = { "North America", "South America", "Europe", "Africa", "Asia", "Australasia" };
            Continent[] continents = new Continent[continentNames.Length];
            int[] numCities = new int[continentNames.Length];
            int citiesLeft = cities;

            for (int i = 0; i < continentNames.Length; i++)//Distribute cities across the map
            {
                numCities[i] = random.Next(1, (cities / 6));
                citiesLeft -= numCities[i];
                if (i == continentNames.Length - 1 && citiesLeft > 0)
                {
                    for (int j = 0; j < citiesLeft; j++)
                    {
                        numCities[random.Next(0, 6)]++;

                    }
                    citiesLeft = 0;
                }
            }

            //create continent borders and add random cities
            for (int i = 0; i < continentNames.Length; i++)
            {
                Continent cont = new Continent();
                cont.Name = continentNames[i];
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
                cont.Cities = new PointF[numCities[i]];
                for (int j = 0; j < numCities[i]; j++)
                {

                    PointF city = new PointF(
                        Convert.ToSingle((random.NextDouble() * (cont.maxX - cont.minX) + cont.minX)),
                        Convert.ToSingle((random.NextDouble() * (cont.maxY - cont.minY) + cont.minY)));
                    cont.Cities[j] = city;

                }
                continents[i] = cont;

            }
            return continents;



        }
        public PointF[] GetCities()
        {
            return Cities;
        }

    }

}
