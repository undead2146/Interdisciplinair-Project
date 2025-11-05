using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Show.Model
{
    public class Fixture
    {
        [JsonPropertyName("fixtureId")]
        public string? Id { get; set; }

        [JsonPropertyName("instanceId")]
        public string? InstanceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
