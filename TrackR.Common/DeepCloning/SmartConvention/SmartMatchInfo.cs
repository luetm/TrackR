using System.Reflection;

namespace TrackR.Common.DeepCloning.SmartConvention
{
    public class SmartMatchInfo
    {
        public PropertyInfo SourceProp { get; set; }
        public PropertyInfo TargetProp { get; set; }
        public object Source { get; set; }
        public object Target { get; set; }
    }
}