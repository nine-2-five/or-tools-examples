using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace or_tools_examples
{
    internal class Helper
    {
        public static string SerializeArray(long[,] array)
        {
            string strArray = "{";
            for (var i = 0; i < array.GetLength(0); i++)
            {
                strArray += "{";
                for (var j = 0; j < array.GetLength(1); j++)
                {
                    strArray += array[i, j] + ",";
                }
                strArray += "},\n";
            }
            strArray += "}";

            return strArray;
        }

        public static long[,] GetTimeMatrix(string file)
        {
            var googleData = JsonSerializer.Deserialize<GoogleDistanceMatrixResponse>(File.ReadAllText(file));

            var timeMatrix = new long[googleData.Rows.Length, googleData.Rows.Length];

            for (var row = 0; row < googleData.Rows.Length; row++)
            {
                for (var col = 0; col < googleData.Rows.Length; col++)
                {
                    timeMatrix[row, col] = googleData.Rows[row].Elements[col].Duration.Value / 60;
                }
            }

            return timeMatrix;
        }
    }
}
