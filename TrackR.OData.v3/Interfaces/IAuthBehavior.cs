using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.OData;

namespace TrackR.OData.v3.Interfaces
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
        /// Used internally to add authentication information to the request (usually the header).
        /// </summary>
        /// <param name="message"></param>
        void AddAuthentication(IODataRequestMessage message);
    }
}
