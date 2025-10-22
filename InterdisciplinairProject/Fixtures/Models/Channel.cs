using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Channel
    {
        public string Name { get; set; }
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}
