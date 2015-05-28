using Newtonsoft.Json;
using Omu.ValueInjecter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrackR.Client;
using TrackR.Common;
using TrackR.Common.DeepCloning;
using TrackR.Common.Interfaces;

namespace TrackR.OData.v3
{
    public abstract class ODataTrackRContext<TContext, TEntityBase> : TrackRContext<TEntityBase> 
        where TContext : DataServiceContext
        where TEntityBase : class
    {
        /// <summary>
        /// Context to create a odata query.
        /// </summary>
        public TContext QueryContext { get; set; }


        /// <summary>
        /// Handles different kinds of authentication methods.
        /// </summary>
        public IAuthBehavior AuthBehavior { get; set; }

        /// <summary>
        /// Lock to ensure only one operation.
        /// </summary>
        private readonly object _opLock = new object();

        /// <summary>
        /// Creates a new ODataTrackRContext.
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="trackRUri"></param>
        protected ODataTrackRContext(Uri baseUri, Uri trackRUri)
            : base(trackRUri)
        {
            // Create query context
            var ctor = typeof(TContext).GetConstructors().First();
            BaseUri = baseUri;
            QueryContext = (TContext)ctor.Invoke(new object[] { BaseUri });

            // Authentication
            QueryContext.SendingRequest2 += OnSendingRequest;
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
        /// Executes a query asynchronously and deep injects the object graphs into a type of your choice.
        /// </summary>
        /// <typeparam name="TEntity">Type of object you want your data injected into. Needs a parameterless constructor.</typeparam>
        /// <param name="query">Query, created by the query context.</param>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> LoadManyAsync<TEntity>(IQueryable query) where TEntity : class
        {
            var result = await LoadManyNoTrackingAsync<TEntity>(query);
            TrackMany(result.Cast<TEntityBase>());
            return result;
        }

        /// <summary>
        /// Loads an entity asynchronously with an uri.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public Task<IEnumerable<TEntity>> LoadManyAsync<TEntity>(Uri uri, string method = "GET") where TEntity : class
        {
            return Task.Run(() =>
            {
                lock (_opLock)
                {
                    QueryContext.Format.UseJson();
                    var type = typeof(TEntity).Assembly.GetTypes().First(ty => ty.Name == typeof(TEntity).Name);
                    var executeMethod = QueryContext.GetType().GetMethods().First(m => m.Name == "Execute" && m.IsGenericMethod && m.GetParameters().Count() == 4);
                    executeMethod = executeMethod.MakeGenericMethod(type);
                    var result = executeMethod.Invoke(QueryContext, new object[] { uri, method, false, null });

                    if (!(result is QueryOperationResponse))
                    {
                        throw new InvalidOperationException("Operation in LoadAsync must be a query.");
                    }

                    var response = result as QueryOperationResponse;
                    var t = response.Cast<object>().ToList();
                    var v = t.DeepInject<TEntity>().ToList();
                    TrackMany(v.Cast<TEntityBase>());
                    return v.AsEnumerable();
                }
            });
        }


        /// <summary>
        /// Executes a query asynchronously and deep injects the object graph into a type of your choice.
        /// </summary>
        /// <typeparam name="TEntity">Type of object you want your data injected into. Needs a parameterless constructor.</typeparam>
        /// <param name="query">Query, created by the query context.</param>
        /// <returns></returns>
        public async Task<TEntity> LoadAsync<TEntity>(IQueryable query) where TEntity : class
        {
            var result = await LoadManyAsync<TEntity>(query);
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Loads an entity asynchronously with an uri.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<TEntity> LoadAsync<TEntity>(Uri uri, string method = "POST") where TEntity : class
        {
            return (await LoadManyAsync<TEntity>(uri, method)).FirstOrDefault();
        }


        /// <summary>
        /// Loads a set of entities / complex types, without tracking them.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> LoadManyNoTrackingAsync<TResult>(IQueryable query) where TResult : class
        {
            var uri = (Uri)query.GetType().GetProperty("RequestUri").GetValue(query);
            return LoadManyNoTrackingAsync<TResult>(uri, "GET");
        }

        /// <summary>
        /// Loads a set of entities / complex types, without tracking them.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="uri"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> LoadManyNoTrackingAsync<TResult>(Uri uri, string method = "POST") where TResult : class
        {
            return Task.Run(() =>
            {
                lock (_opLock)
                {
                    QueryContext.Format.UseJson();
                    var type = typeof(TContext).Assembly.GetTypes().First(ty => ty.Name == typeof(TResult).Name);
                    var executeMethod = QueryContext.GetType().GetMethods().First(m => m.Name == "Execute" && m.IsGenericMethod && m.GetParameters().Count() == 4);
                    executeMethod = executeMethod.MakeGenericMethod(type);
                    var result = executeMethod.Invoke(QueryContext, new object[] { uri, method, false, null });

                    if (!(result is QueryOperationResponse))
                    {
                        throw new InvalidOperationException("Operation in LoadAsync must be a query.");
                    }

                    var response = result as QueryOperationResponse;
                    var t = response.Cast<object>().ToList();
                    var v = t.DeepInject<TResult>();
                    return v;
                }
            });
        }

        /// <summary>
        /// Loads an entity / complex type, without tracking it.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<TResult> LoadNoTrackingAsync<TResult>(IQueryable query) where TResult : class
        {
            var result = await LoadManyNoTrackingAsync<TResult>(query);
            return result.FirstOrDefault();
        }


        /// <summary>
        /// Executes an action asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="uri"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> ExecuteActionForManyAsync<TResult>(string uri, object parameters, string method = "POST")
        {
            var url = ToAbsoluteUri(uri, parameters);
            return ExecuteActionForManyAsync<TResult>(url, parameters, method);
        }

        /// <summary>
        /// Executes an action asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="uri"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TResult>> ExecuteActionForManyAsync<TResult>(Uri uri, object parameters, string method = "POST")
        {
            var properties = parameters.GetProps();
            var operationParameters = properties
                .Cast<PropertyDescriptor>()
                .Select(prop => new BodyOperationParameter(prop.Name, prop.GetValue(parameters)) as OperationParameter)
                .ToList();

            var p = operationParameters.ToArray();

            var result = await Task.Run(() => QueryContext.Execute<TResult>(uri, method, false, p));
            var r = result.Cast<TEntityBase>().ToList();
            TrackMany(r);
            return result;
        }

        /// <summary>
        /// Executes an action asynchronously.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="uri"></param>
        /// <param name="parameters"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<TResult> ExecuteActionAsync<TResult>(string uri, object parameters, string method = "POST")
        {
            var result = (await ExecuteActionForManyAsync<TResult>(uri, parameters, method)).ToList();
            return result.FirstOrDefault();
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
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
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
        public async Task HttpGetAsync (string queryPath, object parameters, string method = "GET")
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
        /// Handles authentication and authorization.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSendingRequest(object sender, SendingRequest2EventArgs e)
        {
            if (AuthBehavior != null)
            {
                var header = AuthBehavior.GetHeader();
                e.RequestMessage.SetHeader(header.Item1, header.Item2);
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
                    ContractResolver = new ODataContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
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
                var properties = query.GetProps().ToList();
                var strings = properties
                    .Select(prop => prop.GetValue(query).ToUriParameter(prop.Name))
                    .ToList();

                foreach (var s in strings)
                {
                    Debugger.Log(1, "Test", s);
                }

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
