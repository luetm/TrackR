﻿using Newtonsoft.Json;
using Omu.ValueInjecter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TrackR.Client;
using TrackR.Common;
using TrackR.Common.DeepCloning;
using TrackR.Common.Interfaces;

namespace TrackR.WebApi2
{
    public abstract class WebApi2TrackRContext<TEntityBase> : TrackRContext<TEntityBase> where TEntityBase : class
    {
        protected WebApi2TrackRContext(Uri trackRUri) : base(trackRUri)
        {
            BaseUri = trackRUri;
        }

        /// <summary>
        /// Logs the user in with the service.
        /// </summary>
        /// <param name="uri">Uri, either relative to base url or absolute.</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password (cleartext)</param>
        /// <param name="behavior">Authentication behavior, if left null will be defaulted.</param>
        /// <returns>True, when authentication was successful.</returns>
        public async Task<bool> LoginAsync(Uri uri, string username, string password, IAuthBehavior behavior)
        {
            // Check for null.
            if (behavior == null)
                throw new ArgumentNullException("behavior");

            // If the uri given is relative, we transform it to an absolute uri by 
            // appending the relative path to the absolute uri.
            if (!uri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(BaseUri);
                builder.Path = uri.ToString();
                uri = builder.Uri;
            }

            // Create an authentication behavior based on the method
            AuthBehavior = behavior;

            // Login with the given behavior
            var success = await AuthBehavior.Login(username, password, uri.ToString());
            return success;
        }

        /// <summary>
        /// Destroys all authentication information.
        /// </summary>
        public void Logout()
        {
            AuthBehavior = null;
        }

        /// <summary>
        /// Destroys all authentication information asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task LogoutAsync()
        {
            return Task.Run(() =>
            {
                Logout();
            });
        }


        /// <summary>
        /// Direct query over url.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryPath"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TResult>> HttpGetManyAsync<TResult>(string queryPath, object parameters, string method = "GET") where TResult : class
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var uri = ToAbsoluteUri(queryPath, parameters, null);

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, content));
                }

                var json = await response.Content.ReadAsStringAsync();
                var settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    TypeNameHandling = TypeNameHandling.Objects,
                };

                var result = JsonConvert.DeserializeObject<IEnumerable<TResult>>(json, settings);
                return result;
            }
        }

        /// <summary>
        /// Direct query over url.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryPath"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<TResult> HttpGetAsync<TResult>(string queryPath, object parameters, string method = "GET") where TResult : class
        {
            using (var client = new HttpClient())
            {
                var uri = ToAbsoluteUri(queryPath, parameters, null);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, response.Content.ToString()));
                }

                var result = await response.Content.ReadAsAsync<TResult>();
                return (TResult)(typeof(TResult).GetConstructors().Single(x => !x.GetParameters().Any()).Invoke(null)).InjectFrom<DeepCloneInjection>(result);
            }
        }

        /// <summary>
        /// Executes a get request without caring about the result.
        /// </summary>
        /// <param name="queryPath"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task HttpGetAsync(string queryPath, object parameters, string method = "GET")
        {
            using (var client = new HttpClient())
            {
                var uri = ToAbsoluteUri(queryPath, parameters, null);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, response.Content.ToString()));
                }
            }
        }


        /// <summary>
        /// Posts an entity to the server.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="entitySet"></param>
        /// <returns></returns>
        public async Task PostEntity<TEntity>(TEntity entity, string entitySet)
        {
            var uri = ToAbsoluteUri(entitySet, null);

            using (var client = new HttpClient())
            {
                var settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                };
                var json = JsonConvert.SerializeObject(entity, settings);

                client.DefaultRequestHeaders.Add("Accept", "application/json; odata=minimalmetadata");
                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }

                var result = await client.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));

                if (!result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(result.StatusCode, content));
                }
            }
        }

        /// <summary>
        /// Converts to an absolute uri.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public Uri ToAbsoluteUri(string path, object query, string basePath = "odata")
        {
            if (query != null)
            {
                var properties = query.GetType().GetProperties().ToList();
                var strings = properties
                    .Select(prop => prop.GetValue(query).ToUriParameter(prop.Name))
                    .ToList();

                
                var queryString = string.Join("&", strings);
                var builder = new UriBuilder(BaseUri)
                {
                    Query = queryString
                };
                if (basePath != null)
                {
                    builder.Path = basePath + "/" + path;
                }
                else
                {
                    builder.Path = path;
                }

                return builder.Uri;
            }
            else
            {
                var builder = new UriBuilder(BaseUri);
                if (basePath != null)
                {
                    builder.Path = basePath + "/" + path;
                }
                else
                {
                    builder.Path = path;
                }
                return builder.Uri;
            }
        }
    }
}