using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Fixtures.Models
{
    public class Fixture
    {
        public string Name { get; set; }

        [JsonPropertyName("channels")]
        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        public Fixture(string name)
        {
            Name = name;

        }
    }
}
