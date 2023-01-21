using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.OrTools.ConstraintSolver;

/// <summary>
///   Minimal Pickup & Delivery Problem (PDP).
/// </summary>
public class VrpPickupDelivery
{

    public class GoogleDistanceMatrixResponse
    {
        public string Status { get; set; }

        [JsonPropertyName("destination_addresses")]
        public string[] DestinationAddresses { get; set; }

        [JsonPropertyName("origin_addresses")]
        public string[] OriginAddresses { get; set; }

        [JsonPropertyName("rows")]
        public Row[] Rows { get; set; }

        public class Data
        {
            [JsonPropertyName("value")]
            public long Value { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public class Element
        {
            [JsonPropertyName("status")]
            public string Status { get; set; }

            [JsonPropertyName("duration")]
            public Data Duration { get; set; }

            [JsonPropertyName("distance")]
            public Data Distance { get; set; }
        }

        public class Row
        {
            [JsonPropertyName("elements")]
            public Element[] Elements { get; set; }
        }
    }
    class DataModel
    {
        public long[,] DistanceMatrix = {
            { 0, 548, 776, 696, 582, 274, 502, 194, 308, 194, 536, 502, 388, 354, 468, 776, 662 },
            { 548, 0, 684, 308, 194, 502, 730, 354, 696, 742, 1084, 594, 480, 674, 1016, 868, 1210 },
            { 776, 684, 0, 992, 878, 502, 274, 810, 468, 742, 400, 1278, 1164, 1130, 788, 1552, 754 },
            { 696, 308, 992, 0, 114, 650, 878, 502, 844, 890, 1232, 514, 628, 822, 1164, 560, 1358 },
            { 582, 194, 878, 114, 0, 536, 764, 388, 730, 776, 1118, 400, 514, 708, 1050, 674, 1244 },
            { 274, 502, 502, 650, 536, 0, 228, 308, 194, 240, 582, 776, 662, 628, 514, 1050, 708 },
            { 502, 730, 274, 878, 764, 228, 0, 536, 194, 468, 354, 1004, 890, 856, 514, 1278, 480 },
            { 194, 354, 810, 502, 388, 308, 536, 0, 342, 388, 730, 468, 354, 320, 662, 742, 856 },
            { 308, 696, 468, 844, 730, 194, 194, 342, 0, 274, 388, 810, 696, 662, 320, 1084, 514 },
            { 194, 742, 742, 890, 776, 240, 468, 388, 274, 0, 342, 536, 422, 388, 274, 810, 468 },
            { 536, 1084, 400, 1232, 1118, 582, 354, 730, 388, 342, 0, 878, 764, 730, 388, 1152, 354 },
            { 502, 594, 1278, 514, 400, 776, 1004, 468, 810, 536, 878, 0, 114, 308, 650, 274, 844 },
            { 388, 480, 1164, 628, 514, 662, 890, 354, 696, 422, 764, 114, 0, 194, 536, 388, 730 },
            { 354, 674, 1130, 822, 708, 628, 856, 320, 662, 388, 730, 308, 194, 0, 342, 422, 536 },
            { 468, 1016, 788, 1164, 1050, 514, 514, 662, 320, 274, 388, 650, 536, 342, 0, 764, 194 },
            { 776, 868, 1552, 560, 674, 1050, 1278, 742, 1084, 810, 1152, 274, 388, 422, 764, 0, 798 },
            { 662, 1210, 754, 1358, 1244, 708, 480, 856, 514, 468, 354, 844, 730, 536, 194, 798, 0 },
        };

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

    static string SerializeArray(long[,] array)
    {
        string strArray = "{";
        for(var i=0; i<array.GetLength(0); i++)
        {
            strArray += "{";
            for(var j=0;j<array.GetLength(1); j++)
            {
                strArray += array[i, j] + ",";
            }
            strArray += "},\n";
        }
        strArray+= "}";

        return strArray;
    }

    static long[,] GetTimeMatrix(string file)
    {
        var googleData = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(File.ReadAllText(file));

        var timeMatrix = new long[googleData.Rows.Length, googleData.Rows.Length];

        for(var row = 0; row < googleData.Rows.Length; row++)
        {
            for(var col = 0; col < googleData.Rows.Length; col++)
            {
                timeMatrix[row, col] = googleData.Rows[row].Elements[col].Duration.Value / 60; 
            }
        }

        return timeMatrix;
    }

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

        //data.TimeMatrix = GetTimeMatrix("./data1.json");

        //var strArray = SerializeArray(data.TimeMatrix);

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

        Console.ReadKey();
    }
}