using System.ComponentModel;
using PropertyChanged;

namespace TestData.Entities
{
    [ImplementPropertyChanged]
    public class Entity : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
