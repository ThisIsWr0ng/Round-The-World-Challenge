## Round The World Challenge

This is an University project at level 6

Creating an application for Blue Cow company required an understanding the Traveling Salesman Problem (TSP). The Blue Cow needs software to calculate the shortest round route for 30 – 50 cities around the world for its aeroplane race. This closely resembles a TSP as it focuses on finding an optimised round trip across given locations. TSP is categorised as an NP-Hard problem and was formulated in the 1800s by the Irish mathematician W.R. Hamilton and by the British mathematician Thomas Kirkman. Blue Cow’s software must be able to select only the most profitable route from the pool of around 100 cities, which makes it more complicated than classic TSP.
For the implementation of the software, C# programming language and Windows Forms were chosen for the Graphical User Interface (GUI). Creating a user interface was quick and easy, thanks to drag and drop functionality of the Windows Forms class library.

![image](https://github.com/ThisIsWr0ng/Round-the-world-challenge/assets/99226094/990b9ee8-3808-4921-8087-ad625e69ee69)

![image](https://github.com/ThisIsWr0ng/Round-the-world-challenge/assets/99226094/df567abb-0727-4677-b91f-a27c83c66765)

# Features and Restrictions

The Blue Cow problem required specific features and restrictions to be implemented in the software. The following solutions were implemented for each feature and restriction:

1. Optimising route and maximising income:

* A profit check was performed on each iteration to check if the current route has better profitability than the previous best solution
  
* If the profit was lower, then the solution could still be chosen depending on the probability of the SA algorithm 

2. Start and finish in the same city:

* The route is stored in an array, and the first city was added to the end of the array to make it a round trip; the 2-Opt algorithm was adjusted to not swap the first and last connections in the array.

3.	Given the minimum and maximum number of cities in the tour:

*	The algorithm starts with a random route and optimises it in a few first iterations. Then, if the maximum limit is higher than the route length, connections with the lowest distance-to-bid ratio are removed until the limit is reached. To further optimise the route, connections with negative profit are removed in consequent iterations.

4. Given minimum and maximum distance per flight:

*	An algorithm calculates the distance between connections in each iteration. If any distance is over or under the limit, the CalcDist method returns a negative value.

5. A number of cities visited on each continent:

*	While removing the cities from the list to create a route with the desired length, an algorithm checks if the number of cities on this continent has not reached the minimum allowed. 

6. Given minimum and maximum regarding total distance:

*	The total distance is checked after each iteration. If it does not meet the limits, a solution is not allowed

7.	Flight restrictions between some cities:

*	The calcDist method also checks each connection with the restrictions array and returns a negative value if any record is a match.

# Optimisation
The 2-Opt algorithm was used in this implementation and combined with the Simulated Annealing (SA) technique to achieve the best results in this task as it seemed easy to implement comparing to e.g. Genetic Algorithms; 3-Opt was also considered, but it’s implementation failed on multiple ocasions. The process of heating and cooling metals inspires SA, and the algorithm mimics different heat levels where a state's heat level affects the chance of choosing a different state. SA in this implementation uses the following hyperparameters to adjust its effectiveness:

*β – Beta is a normalising constant. Its choice depends on the expected variation in the probability of choosing a worse solution
*α – Alpha is the cooling down parameter. The lower the number, the faster the cooldown process
*ε – Epsilon decides at which temperature the algorithm stops
*T – Temperature at which the algorithm starts the cooling down process
*n – number of iterations after which the cooling process progresses 
 
2-Opt is a local search algorithm that, with efficient implementation, can turn TSP to O(1) operation, but research shows that the average-case complexity of it can have an upper bound of O(log(n)). While running the software without checking for restrictions results in O(n log n) complexity, worst-case scenario of the software is O(n^2) when all restrictions are checked. As 2-Opt can easily get stuck in the local maxima, SA should help the algorithm to reach global maxima by choosing worse routes with certain probabilities. Implementing these algorithms was a challenging task. 2-Opt checks if swapping two connections will shorten the route and performs the swap if it does. 

![image](https://github.com/ThisIsWr0ng/Round-the-world-challenge/assets/99226094/e00b7267-8fd3-43fd-960a-6283df87121d)

# Tuning Simulated Annealing hyperparameters
SA tuning steps:
1.	Set the initial temperature to be high enough to allow SA to explore in the first iterations
2.	Set Alpha close to 1 so the cooling process is not too slow
3.	Set Epsilon close to 0 to ensure the cooling process won’t stop too early
4.	Taking the average best, worst, and average distance and time out of 10 runs and calculating the delta between the best and worst run
5.	For a set amount of Cities:
5.1.	Generate a first map with bids from each city, and keep it for this number of cities
5.2.	Turn on the data logging option in the advanced options
5.3.	Adjusting Alpha, Temperature, or Epsilon. One parameter per 10 runs
5.3.1.	Checking testing data for an optimal parameter value for iterations, average time, time delta, distance and distance delta
5.3.1.1.	Distance and time delta should be minimal for lower variations in routes
5.3.1.2.	The average distance should be lowest for the best route
5.4.	If the optimal value is found, perform the subsequent ten runs changing another parameter
6.	If optimal values were found for a set amount of cities, increase the number of cities to test if values give optimal solutions (see Appendix 1: Simulated Annealing tuning data).

# Future Improvements

*	First route optimisation for better results
*	Using the Nearest Neighbour algorithm instead of 2-Opt or implementing 3-Opt
*	Allowing to set the minimum and maximum amount bids for cities
*	Using actual locations of cities instead of random generation
*	Adjusting SA parameters based on the number of cities
*	Creating a user manual
*	Creating a login screen, so only authorised users are allowed to change advanced options





