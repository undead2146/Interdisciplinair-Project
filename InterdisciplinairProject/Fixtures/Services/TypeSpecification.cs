using InterdisciplinairProject.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Fixtures.Services
{
    public class TypeSpecification
    {
        public string name { get; set; } = "";
        public string input { get; set; } = "slider"; // "text" | "slider" | "custom"
        public int? min { get; set; }   // slider minimum
        public int? max { get; set; }   // slider maximum

        // 🔹 NEW: ranges defined for this type (by name)
        public List<ChannelRange> ranges { get; set; } = new();
    }
}
