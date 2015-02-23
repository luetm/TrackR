using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using TestData.Annotations;

namespace TestData
{
    [ImplementPropertyChanged]
    public class Entity : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
