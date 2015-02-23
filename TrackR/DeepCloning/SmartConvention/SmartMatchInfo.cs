using System.ComponentModel;

namespace TrackR.DeepCloning.SmartConvention
{
    public class SmartMatchInfo
    {
        public PropertyDescriptor SourceProp { get; set; }
        public PropertyDescriptor TargetProp { get; set; }
        public object Source { get; set; }
        public object Target { get; set; }
    }
}