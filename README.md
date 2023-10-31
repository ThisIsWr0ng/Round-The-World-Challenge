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



