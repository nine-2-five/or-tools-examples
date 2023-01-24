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

        /* seems like enough space to solve at least a couple of pickups, based on durations in the time matrix above */
        public long[,] TimeWindows =
        {
            {0,0},            
            {360,360},
            {480,480},
            {720,720}
        };

        public int[][] PickupsDeliveries = {
            new int[] { 1, 6 }, new int[] { 2, 4 },  new int[] { 5,8 },   new int[] { 7, 3 }   
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

            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);

            return data.TimeMatrix[fromNode, toNode];                        
        });

        // Define cost of each arc.
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        // Add time constraint.
        routing.AddDimension(transitCallbackIndex, 1, 3000,
                                true, // start cumul to zero
                                "Time");
        RoutingDimension timeDimension = routing.GetMutableDimension("Time");
        timeDimension.SetGlobalSpanCostCoefficient(100); /* what does this do? */

        // Add time window constraints for each location except depot.
        for (int i = 1; i < data.TimeWindows.GetLength(0); ++i) /* not sure if I should include the first time window instead of skipping the i=0 */
        {
            long index = manager.NodeToIndex(i);
            timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
        }
        // Add time window constraints for each vehicle start node.
        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            long index = routing.Start(i);
            timeDimension.CumulVar(index).SetRange(data.TimeWindows[0, 0], data.TimeWindows[0, 1]);
        }
        // [END time_constraint]

        // Instantiate route start and end times to produce feasible times.
        // [START depot_start_end_times]
        for (int i = 0; i < data.VehicleNumber; ++i)
        {
            routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.Start(i)));
            routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.End(i)));
        }
        // [END depot_start_end_times]

        // Allow to drop nodes.
        long penalty = 100000;
        for (int i = 1; i < data.TimeMatrix.GetLength(0); ++i)
        {
            routing.AddDisjunction(new long[] { manager.NodeToIndex(i) }, penalty);
        }

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

        Console.ReadKey();
    }
}