using System;
using System.Threading.Tasks;

namespace TrackR.Common.Interfaces
{
    /// <summary>
    /// Handles authentication for different authentication methods (token, basic, ...)
    /// </summary>
    public interface IAuthBehavior
    {
        /// <summary>
        /// Login using the given method.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<bool> Login(string username, string password, string url);

        /// <summary>
        /// Gets the header for custom (non odata) header attachment.
        /// </summary>
        /// <returns></returns>
        Tuple<string, string> GetHeader();
    }
}
