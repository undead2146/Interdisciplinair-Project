using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Show.Model
{
    public class Scene
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int Dimmer { get; set; }
    }
}
