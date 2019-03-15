using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TrackR.Client;
using TrackR.Common;
using TrackR.Common.Interfaces;

namespace TrackR.WebApi2
{
    public abstract class WebApi2TrackRContext<TEntityBase> : TrackRContext<TEntityBase> where TEntityBase : class
    {
        protected JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.Objects,
            ContractResolver = new JsonObservableCollectionConverter(),
            MaxDepth = 100,
            Culture = CultureInfo.InvariantCulture,
        };

        /// <summary>
        /// 
        /// </summary>
        protected WebApi2TrackRContext()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUri"></param>
        protected WebApi2TrackRContext(Uri baseUri) : base(baseUri)
        {

        }

        /// <summary>
        /// Creates the http client.
        /// </summary>
        /// <returns></returns>
        protected override HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
            };
            return new HttpClient(handler);
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
                throw new ArgumentNullException(nameof(behavior));

            // If the uri given is relative, we transform it to an absolute uri by 
            // appending the relative path to the absolute uri.
            if (!uri.IsAbsoluteUri)
            {
                var builder = new UriBuilder(BaseUri)
                {
                    Path = uri.ToString()
                };
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
        /// <returns></returns>
        private async Task<IEnumerable<TResult>> HttpGetManyAsync<TResult>(string queryPath, object parameters)
        {
            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var uri = ToAbsoluteUri(queryPath, parameters, null);

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }
                AddCustomHeaders(client);

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, content));
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<IEnumerable<TResult>>(json, SerializationSettings);
                return result;
            }
        }

        /// <summary>
        /// Direct query over url.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryPath"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<TResult> HttpGetAsync<TResult>(string queryPath, object parameters)
        {
            using (var client = CreateHttpClient())
            {
                var uri = ToAbsoluteUri(queryPath, parameters, null);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("utf-8"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }

                AddCustomHeaders(client);

                var response = await client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, content));
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TResult>(json, SerializationSettings);
                var entity = result as TEntityBase;
                if (entity != null)
                {
                    Track(entity);
                }
                return result;
            }
        }

        /// <summary>
        /// Direct query over url.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="queryPath"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<TResult> HttpPostMultipartContent<TResult>(string queryPath, MultipartFormDataContent content)
        {
            using (var client = CreateHttpClient())
            {
                var uri = ToAbsoluteUri(queryPath, null, null);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("utf-8"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }
                AddCustomHeaders(client);

                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    Content = content,
                    RequestUri = uri,
                };
                var response = await client.SendAsync(message);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, responseContent));
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TResult>(json, SerializationSettings);
                var entity = result as TEntityBase;
                if (entity != null)
                {
                    Track(entity);
                }
                return result;
            }
        }


        /// <summary>
        /// Gets a TResult from a specified uri.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task<TResult> GetAsync<TResult>(QueryParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            return await HttpGetAsync<TResult>(parameter.Path, parameter.UriParameters);
        }

        /// <summary>
        /// Gets a set of TResult from a specified uri.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> GetManyAsync<TResult>(QueryParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            return HttpGetManyAsync<TResult>(parameter.Path, parameter.UriParameters);
        }


        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task PostAsync(QueryParameter parameter)
        {
            return ExecuteAsync(parameter, "POST");
        }

        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task PutAsync(QueryParameter parameter)
        {
            return ExecuteAsync(parameter, "PUT");
        }

        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task DeleteAsync(QueryParameter parameter)
        {
            return ExecuteAsync(parameter, "DELETE");
        }

        /// <summary>
        /// Executes a PATCH asynchronously.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public Task PatchAsync(QueryParameter parameter)
        {
            return ExecuteAsync(parameter, "PATCH");
        }


        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task<TResult> PostAsync<TResult>(QueryParameter parameter)
        {
            return ExecuteAsync<TResult>(parameter, "POST");
        }

        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task<TResult> PutAsync<TResult>(QueryParameter parameter)
        {
            return ExecuteAsync<TResult>(parameter, "PUT");
        }

        /// <summary>
        /// Posts a query asynchronously.
        /// </summary>
        public Task<TResult> DeleteAsync<TResult>(QueryParameter parameter)
        {
            return ExecuteAsync<TResult>(parameter, "DELETE");
        }

        /// <summary>
        /// Executes a PATCH asynchronously.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public Task<TResult> PatchAsync<TResult>(QueryParameter parameter)
        {
            return ExecuteAsync<TResult>(parameter, "PATCH");
        }



        /// <summary>
        /// Executes an action asynchornously.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="verb"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(QueryParameter parameter, string verb = "GET")
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }
                AddCustomHeaders(client);

                var method = StringToHttpMethod(verb);
                var uri = ToAbsoluteUri(parameter.Path, parameter.UriParameters, null);
                var message = new HttpRequestMessage(method, uri);

                if (verb != "GET")
                {
                    if (parameter.BodyValue != null)
                    {
                        var json = JsonConvert.SerializeObject(parameter.BodyValue);
                        message.Content = new StringContent(json);
                        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        message.Content.Headers.ContentEncoding.Add("utf-8");
                    }
                    if (parameter.BodyRaw != null)
                    {
                        message.Content = new StringContent(parameter.BodyRaw);
                    }
                }

                var response = await client.SendAsync(message);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, content));
                }
            }
        }

        /// <summary>
        /// Executes an action asynchornously.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="verb"></param>
        /// <returns></returns>
        public async Task<TResult> ExecuteAsync<TResult>(QueryParameter parameter, string verb = "GET")
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            using (var client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }
                AddCustomHeaders(client);

                var method = StringToHttpMethod(verb);
                var uri = ToAbsoluteUri(parameter.Path, parameter.UriParameters, null);
                var message = new HttpRequestMessage(method, uri);

                if (verb != "GET")
                {
                    if (parameter.BodyValue != null)
                    {
                        var json = JsonConvert.SerializeObject(parameter.BodyValue);
                        message.Content = new StringContent(json);
                        message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        message.Content.Headers.ContentEncoding.Add("utf-8");
                    }
                    if (parameter.BodyRaw != null)
                    {
                        message.Content = new StringContent(parameter.BodyRaw);
                    }
                }

                var response = await client.SendAsync(message);
                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var exception = TryParseException(content);
                    if (exception == null)
                        throw new WebException("{0}: {1}".FormatStatic(response.StatusCode, content));
                    throw exception;
                }

                var jsonResult = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TResult>(jsonResult, SerializationSettings);
                var entity = result as TEntityBase;
                if (entity != null)
                {
                    Track(entity);
                }
                if (result is IEnumerable enumerable)
                {
                    foreach (var r in enumerable)
                    {
                        if (r is TEntityBase e)
                        {
                            Track(e);
                        }
                    }
                }
                return result;
            }
        }

        private Exception TryParseException(string content)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    TypeNameHandling = TypeNameHandling.None,
                    ContractResolver = new JsonObservableCollectionConverter(),
                    MaxDepth = 100,
                    Culture = CultureInfo.InvariantCulture,
                };
                var error = JsonConvert.DeserializeObject<HttpError>(content, settings);
                return new WebException(StringifyHttpError(error));
            }
            catch /* POKéMON */
            {
                return null;
            }
        }

        private string StringifyHttpError(HttpError error)
        {
            var lines = new List<string>
            {
                "",
                $"Type: {error.ExceptionType}",
                $"Message: {error.Message}",
                $"Exception-Message: {error.ExceptionMessage}",
                $"Message-Detail: {error.MessageDetail}",
                ""
            };

            if (error.InnerException != null)
            {
                lines.Add("Inner Exception:");
                lines.Add(StringifyHttpError(error.InnerException));
            }

            return string.Join("\n", lines);
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

            using (var client = CreateHttpClient())
            {
                var settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.All,
                    Culture = CultureInfo.InvariantCulture,
                    MaxDepth = 100,
                };
                var json = JsonConvert.SerializeObject(entity, settings);

                client.DefaultRequestHeaders.Add("Accept", "application/json; odata=minimalmetadata");
                if (AuthBehavior != null)
                {
                    var authHeader = AuthBehavior.GetHeader();
                    client.DefaultRequestHeaders.Add(authHeader.Item1, authHeader.Item2);
                }
                AddCustomHeaders(client);

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
        public Uri ToAbsoluteUri(string path, object query, string basePath = "api")
        {
            if (query != null)
            {
                var properties = query.GetType().GetProperties().ToList();
                var strings = properties
                    .Select(prop => prop.GetValue(query).ToUriParameter(prop.Name))
                    .ToList();

                if (BaseUri == null)
                {
                    throw new InvalidOperationException("BaseUri is null. Please adjust configuration.");
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

        /// <summary>
        /// Converts a string to the HttpMethod enum.
        /// </summary>
        /// <param name="verb"></param>
        /// <returns></returns>
        private static HttpMethod StringToHttpMethod(string verb)
        {
            HttpMethod method;
            switch (verb)
            {
                case "GET":
                    method = HttpMethod.Get;
                    break;
                case "POST":
                    method = HttpMethod.Post;
                    break;
                case "HEAD":
                    method = HttpMethod.Head;
                    break;
                case "DELETE":
                    method = HttpMethod.Delete;
                    break;
                case "OPTIONS":
                    method = HttpMethod.Options;
                    break;
                case "PUT":
                    method = HttpMethod.Put;
                    break;
                case "TRACE":
                    method = HttpMethod.Trace;
                    break;
                default:
                    method = HttpMethod.Get;
                    break;
            }
            return method;
        }
    }
}
