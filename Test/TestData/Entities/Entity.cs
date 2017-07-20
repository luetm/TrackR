using System.ComponentModel;
using PropertyChanged;

namespace TestData.Entities
{
    [AddINotifyPropertyChangedInterface]
    public class Entity : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
