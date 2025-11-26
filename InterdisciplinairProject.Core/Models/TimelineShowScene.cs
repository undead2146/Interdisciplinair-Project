using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdisciplinairProject.Core.Models
{
    public class TimelineShowScene:INotifyPropertyChanged
    {
        public int Id { get; set; }
        public ShowScene ShowScene { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
