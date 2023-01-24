using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Google.OrTools.ConstraintSolver;
using or_tools_examples;

/// <summary>
///   Minimal Pickup & Delivery Problem (PDP).
/// </summary>
public class VrpPickupDelivery
{
    class DataModel
    {
        public long[,] TimeMatrix = {
            {0,62,78,83,87,61,74,101,40 },
            {65,0,34,116,35,17,93,62,47 },
            {76,30,0,127,55,40,104,65,66 },
            {82,113,128,0,138,112,107,151,103 },
            {90,36,59,141,0,48,118,63,78 },
            {63,17,42,114,44,0,90,71,36 },
            {72,90,106,110,115,89,0,129,80 },
            {100,59,65,151,61,71,128,0,89 },
            {43,49,68,105,76,35,82,91,0 }
        };

        public int[][] PickupsDeliveries = {
            new int[] { 1, 6 }, new int[] { 2, 4 },  new int[] { 5, 1 },   new int[] { 7, 3 }   
        };
        public int VehicleNumber = 4;
        public int Depot = 0;
    };    

    /// <summary>
    ///   Print the solution.
    /// </summary>
    static void PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
                              in Assignment solution)
    {
        Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

        // Inspect solution.
        long totalTime = 0;
        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            Console.WriteLine("Route for Vehicle {0}:", i);
            long routeTime = 0;
            var index = routing.Start(i);
            while (routing.IsEnd(index) == false)
            {
                Console.Write("{0} -> ", manager.IndexToNode((int)index));
                var previousIndex = index;
                index = solution.Value(routing.NextVar(index));
                routeTime += routing.GetArcCostForVehicle(previousIndex, index, 0);
            }
            Console.WriteLine("{0}", manager.IndexToNode((int)index));
            Console.WriteLine("Time of the route: {0}minutes", routeTime);
            totalTime += routeTime;
        }
        Console.WriteLine("Total Time of all routes: {0}mminutes", totalTime);
    }

    public static void Main(String[] args)
    {
        // Instantiate the data problem.
        DataModel data = new DataModel();

        //data.TimeMatrix = Helper.GetTimeMatrix("./data1.json");

        //var strArray = Helper.SerializeArray(data.TimeMatrix);

        //File.WriteAllText("./timeMatrix.txt", strArray);

        try
        {
            // Create Routing Index Manager
            RoutingIndexManager manager =
                new RoutingIndexManager(data.TimeMatrix.GetLength(0), data.VehicleNumber, data.Depot);


            // Create Routing Model.
            RoutingModel routing = new RoutingModel(manager);

            // Create and register a transit callback.
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to
                // distance matrix NodeIndex.
                int fromNode;
                int toNode;
                try
                {
                    fromNode = manager.IndexToNode(fromIndex);
                    toNode = manager.IndexToNode(toIndex);
                    return data.TimeMatrix[fromNode, toNode];
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                return 123;
            });

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Add time constraint.
            routing.AddDimension(transitCallbackIndex, 0, 3000,
                                 true, // start cumul to zero
                                 "Time");
            RoutingDimension timeDimension = routing.GetMutableDimension("Time");
            timeDimension.SetGlobalSpanCostCoefficient(100);

            // Define Transportation Requests.
            Solver solver = routing.solver();
            for (int i = 0; i < data.PickupsDeliveries.GetLength(0); i++)
            {
                long pickupIndex = manager.NodeToIndex(data.PickupsDeliveries[i][0]);
                long deliveryIndex = manager.NodeToIndex(data.PickupsDeliveries[i][1]);
                routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                solver.Add(solver.MakeLessOrEqual(timeDimension.CumulVar(pickupIndex),
                                                  timeDimension.CumulVar(deliveryIndex)));
            }

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            // Solve the problem.
            Assignment solution = routing.SolveWithParameters(searchParameters);

            // Print solution on console.
            PrintSolution(data, routing, manager, solution);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        

        Console.ReadKey();
    }
}