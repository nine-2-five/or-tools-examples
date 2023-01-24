using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace or_tools_examples
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
}
