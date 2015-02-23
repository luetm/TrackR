
namespace TrackR.Common
{
    /// <summary>
    /// All extensions in one place.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Performs string.Format() on a string object.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string F(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        /// <summary>
        /// Performs string.IsNullOrWhitespace() on a string object.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNullOrWhiteSpace(this string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }
    }
}
