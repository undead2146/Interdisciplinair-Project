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
        public int? divisions { get; set; }// only for slider
    }
}
