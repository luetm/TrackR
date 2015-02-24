using Omu.ValueInjecter;

namespace TestData
{
    public static class ObjectExtensions
    {
        public static T Copy<T>(this T source) where T : new()
        {
            var copy = new T();
            copy.InjectFrom(source);
            return copy;
        }
    }
}
