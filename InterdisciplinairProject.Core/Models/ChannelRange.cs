using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Core.Models
{
    public class ChannelRange
    {
        // --- UI / internal properties ---
        [JsonIgnore]
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public int MinR { get; set; }

        [JsonIgnore]
        public int MaxR { get; set; }

        // --- JSON mapped properties ---

        // "name": "Range1"
        [JsonPropertyName("name")]
        public string JsonName
        {
            get => Name;
            set => Name = value;
        }

        // "dmxRange": [min, max]
        [JsonPropertyName("dmxRange")]
        public int[] DmxRange
        {
            get => new[] { MinR, MaxR };
            set
            {
                if (value != null && value.Length >= 2)
                {
                    MinR = value[0];
                    MaxR = value[1];
                }
            }
        }
    }
}
