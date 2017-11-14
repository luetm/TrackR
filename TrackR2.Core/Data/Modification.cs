namespace TrackR2.Core.Data
{
    public class Modification
    {
        public string PropertyName { get; }
        public string OldValue { get; }
        public string NewValue { get; }

        public Modification(string propertyName, string oldValue, string newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
