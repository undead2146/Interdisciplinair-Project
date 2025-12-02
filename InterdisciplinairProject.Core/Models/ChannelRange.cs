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
        [JsonPropertyName("minR")]
        public int MinR { get; set; }

        [JsonPropertyName("maxR")]
        public int MaxR { get; set; }
    }
}
