﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using JohnsonControls.Metasys.BasicServices.Interfaces;

namespace JohnsonControls.Metasys.BasicServices
{
    public class TraditionalClient : IMsApiClient
    {
        protected FlurlClient Client;

        protected string AccessToken;

        protected DateTime TokenExpires;

        protected bool RefreshToken;

        protected const int MAX_PAGE_SIZE = 1000;

        /// <summary>
        /// Creates a new TraditionalClient.
        /// </summary>
        /// <remarks>
        /// Takes an optional CultureInfo which is useful for formatting numbers. If not specified,
        /// the user's current culture is used.
        /// </remarks>
        /// <param name="cultureInfo"></param>
        public TraditionalClient(string hostname, bool ignoreCertificateErrors = false, ApiVersion version = ApiVersion.V2, CultureInfo cultureInfo = null)
        {
            var culture = cultureInfo ?? CultureInfo.CurrentCulture;
            AccessToken = null;
            TokenExpires = DateTime.UtcNow;
            FlurlHttp.Configure(settings => settings.OnErrorAsync = HandleFlurlErrorAsync);

            if (ignoreCertificateErrors)
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                HttpClient httpClient = new HttpClient(httpClientHandler);
                httpClient.BaseAddress = new Uri($"https://{hostname}"
                    .AppendPathSegments("api", version));
                Client = new FlurlClient(httpClient);
            }
            else
            {
                Client = new FlurlClient($"https://{hostname}"
                    .AppendPathSegments("api", version));
            }
        }

        private async Task HandleFlurlErrorAsync(HttpCall call)
        {
            if (call.Exception.GetType() != typeof(Flurl.Http.FlurlParsingException))
            {
                string error = $"{call.Exception.Message}";
                if (call.RequestBody != null)
                {
                    error += $", with body: {call.RequestBody.ToString()}";
                }
                await LogErrorAsync(error).ConfigureAwait(false);
            }
            call.ExceptionHandled = true;
        }

        private async Task LogErrorAsync(String message)
        {
            await Console.Error.WriteLineAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to login to the given host.
        /// </summary>
        /// <returns>
        /// Access token, expiration date.
        /// </returns>
        public (string Token, DateTime ExpirationDate) TryLogin(string username, string password, bool refresh = true)
        {
            return TryLoginAsync(username, password, refresh).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Attempts to login to the given host asynchronously.
        /// </summary>
        /// <returns>
        /// Access token, expiration date.
        /// </returns>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        /// <exception cref="System.NullReferenceException"></exception>
        public async Task<(string Token, DateTime ExpirationDate)> TryLoginAsync(string username, string password, bool refresh = true)
        {
            this.RefreshToken = refresh;

            var response = await Client.Request("login")
                .PostJsonAsync(new { username, password })
                .ReceiveJson<JToken>()
                .ConfigureAwait(false);

            try
            {
                var accessToken = response["accessToken"];
                var expires = response["expires"];
                this.AccessToken = $"Bearer {accessToken.Value<string>()}";
                this.TokenExpires = expires.Value<DateTime>();
                Client.Headers.Add("Authorization", this.AccessToken);
                if (refresh)
                {
                    ScheduleRefresh();
                }
            }
            catch (System.NullReferenceException)
            {
                await LogErrorAsync("Could not get access token.").ConfigureAwait(false);
                AccessToken = null;
                TokenExpires = DateTime.UtcNow;
            }
            return (Token: this.AccessToken, ExpirationDate: this.TokenExpires);
        }

        /// <summary>
        /// Requests a new access token before current token expires.
        /// </summary>
        /// <returns>
        /// Access token, expiration date.
        /// </returns>
        public (string Token, DateTime ExpirationDate) Refresh()
        {
            return RefreshAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Requests a new access token before current token expires asynchronously.
        /// </summary>
        /// <returns>
        /// Access token, expiration date.
        /// </returns>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        /// <exception cref="System.NullReferenceException"></exception>
        public async Task<(string Token, DateTime ExpirationDate)> RefreshAsync()
        {
            var response = await Client.Request("refreshToken")
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            try
            {
                var accessToken = response["accessToken"];
                var expires = response["expires"];
                this.AccessToken = $"Bearer {accessToken.Value<string>()}";
                this.TokenExpires = expires.Value<DateTime>();
                Client.Headers.Remove("Authorization");
                Client.Headers.Add("Authorization", this.AccessToken);
                if (RefreshToken)
                {
                    ScheduleRefresh();
                }
            }
            catch (System.NullReferenceException)
            {
                await LogErrorAsync("Refresh could not get access token.").ConfigureAwait(false);
                AccessToken = null;
                TokenExpires = DateTime.UtcNow;
            }
            return (Token: this.AccessToken, ExpirationDate: this.TokenExpires);
        }

        /// <summary>
        /// Will call Refresh() a minute before the token expires.
        /// </summary>
        private void ScheduleRefresh()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan delay = TokenExpires - now;
            delay.Subtract(new TimeSpan(0, 1, 0));

            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            int delayms = (int)delay.TotalMilliseconds;

            // If the time in milliseconds is greater than max int delayms will be negative and will not schedule a refresh.
            if (delayms >= 0)
            {
                System.Threading.Tasks.Task.Delay(delayms).ContinueWith(_ => Refresh());
            }
        }

        /// <summary>
        /// Returns the current access token and it's expiration date.
        /// </summary>
        /// <returns>
        /// Access token, expiration date.
        /// </returns>
        public (string Token, DateTime ExpirationDate) GetAccessToken()
        {
            return (Token: this.AccessToken, ExpirationDate: this.TokenExpires);
        }

        /// <summary>
        /// Returns the object identifier (id) of the specified object.
        /// </summary>
        public Guid GetObjectIdentifier(string itemReference)
        {
            return GetObjectIdentifierAsync(itemReference).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns the object identifier (id) of the specified object asynchronously.
        /// </summary>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<Guid> GetObjectIdentifierAsync(string itemReference)
        {
            var response = await Client.Request("objectIdentifiers")
                .SetQueryParam("fqr", itemReference)
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            try
            {
                var str = response.Value<string>();
                var id = new Guid(str);
                return id;
            }
            catch (System.ArgumentNullException)
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Read one attribute value given the Guid of the object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        public Variant ReadProperty(Guid id, string attributeName)
        {
            return ReadPropertyAsync(id, attributeName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Read one attribute value given the Guid of the object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        /// <exception cref="System.NullReferenceException"></exception>
        public async Task<Variant> ReadPropertyAsync(Guid id, string attributeName)
        {
            var response = await Client.Request(new Url("objects")
                .AppendPathSegments(id, "attributes", attributeName))
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            try
            {
                var attribute = response["item"][attributeName];
                return new Variant(id, attribute, attributeName);
            }
            catch (System.NullReferenceException)
            {
                return new Variant(id, null, attributeName);
            }
        }

        /// <summary>
        /// Read many attribute values given the Guids of the objects.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="attributeNames"></param>
        public IEnumerable<(Guid Id, IEnumerable<Variant> Variants)> ReadPropertyMultiple(IEnumerable<Guid> ids,
            IEnumerable<string> attributeNames)
        {
            return ReadPropertyMultipleAsync(ids, attributeNames).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Read many attribute values given the Guids of the objects asynchronously.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="attributeNames"></param>
        public async Task<IEnumerable<(Guid Id, IEnumerable<Variant> Variants)>> ReadPropertyMultipleAsync(IEnumerable<Guid> ids,
            IEnumerable<string> attributeNames)
        {
            if (ids == null || attributeNames == null)
            {
                return null;
            }
            List<(Guid Id, IEnumerable<Variant> Variants)> results = new List<(Guid Id, IEnumerable<Variant> Variants)>();
            var taskList = new List<Task<Variant>>();
            // Prepare Tasks to Read attributes list. In Metasys 11 this will be implemented server side
            foreach (var id in ids)
            {
                foreach (string attributeName in attributeNames)
                {
                    // Much faster reading single property than the entire object, even though we have more server calls
                    taskList.Add(ReadPropertyAsync(id, attributeName));
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            foreach (var id in ids)
            {
                // Get attributes of the specific Id
                List<Task<Variant>> attributeList = taskList.Where(w => w.Result.Id == id).ToList();
                List<Variant> variants = new List<Variant>();
                foreach (var t in attributeList)
                {
                    variants.Add(t.Result); // Prepare variants list
                }
                // Aggregate results
                results.Add((Id: id, Variants: variants));
            }
            return results.AsEnumerable();
        }

        /// <summary>
        /// Read entire object given the Guid of the object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task<(Guid Id, JToken Token)> ReadObjectAsync(Guid id)
        {
            var response = await Client.Request(new Url("objects")
                .AppendPathSegment(id))
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);
            return (Id: id, Token: response);
        }

        /// <summary>
        /// Write a single attribute given the Guid of the object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        /// <param name="newValue"></param>
        /// <param name="priority"></param>
        public void WriteProperty(Guid id, string attributeName, object newValue, string priority = null)
        {
            WritePropertyAsync(id, attributeName, newValue, priority).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Write a single attribute given the Guid of the object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="attributeName"></param>
        /// <param name="newValue"></param>
        /// <param name="priority"></param>
        public async Task WritePropertyAsync(Guid id, string attributeName, object newValue, string priority = null)
        {
            List<(string Attribute, object Value)> list = new List<(string Attribute, object Value)>();
            list.Add((Attribute: attributeName, Value: newValue));
            var item = GetWritePropertyBody(list, priority);

            await WritePropertyRequestAsync(id, item).ConfigureAwait(false);
        }

        /// <summary>
        /// Write to all attributes given the Guids of the objects.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="attributeValues">The (attribute, value) pairs</param>
        /// <param name="priority"></param>
        public void WritePropertyMultiple(IEnumerable<Guid> ids,
            IEnumerable<(string Attribute, object Value)> attributeValues, string priority = null)
        {
            WritePropertyMultipleAsync(ids, attributeValues, priority).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Write many attribute values given the Guids of the objects asynchronously.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="attributeValues">The (attribute, value) pairs</param>
        /// <param name="priority"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        public async Task WritePropertyMultipleAsync(IEnumerable<Guid> ids,
            IEnumerable<(string Attribute, object Value)> attributeValues, string priority = null)
        {
            if (ids == null || attributeValues == null)
            {
                return;
            }

            var item = GetWritePropertyBody(attributeValues, priority);

            var taskList = new List<Task>();

            foreach (var id in ids)
            {
                taskList.Add(WritePropertyRequestAsync(id, item));
            }

            await Task.WhenAll(taskList).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the body for the WriteProperty and WritePropertyMultiple requests.
        /// </summary>
        /// <param name="attributeValues">The (attribute, value) pairs</param>
        /// <param name="priority"></param>
        private Dictionary<string, object> GetWritePropertyBody(
            IEnumerable<(string Attribute, object Value)> attributeValues, string priority)
        {
            Dictionary<string, object> pairs = new Dictionary<string, object>();
            foreach (var attribute in attributeValues)
            {
                pairs.Add(attribute.Attribute, attribute.Value);
            }

            if (priority != null)
            {
                pairs.Add("priority", priority);
            }

            return pairs;
        }

        /// <summary>
        /// Write one or many attribute values in the provided json given the Guid of the object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="body"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task WritePropertyRequestAsync(Guid id, Dictionary<string, object> body)
        {
            var json = new { item = body };
            var response = await Client.Request(new Url("objects")
                .AppendPathSegment(id))
                .PatchJsonAsync(json)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get all available commands given the Guid of the object.
        /// </summary>
        /// <param name="id"></param>
        public IEnumerable<Command> GetCommands(Guid id)
        {
            return GetCommandsAsync(id).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Get all available commands given the Guid of the object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        public async Task<IEnumerable<Command>> GetCommandsAsync(Guid id)
        {
            var token = await Client.Request(new Url("objects")
                .AppendPathSegments(id, "commands"))
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            List<Command> commands = new List<Command>();

            var array = token as JArray;

            if (array != null)
            {
                foreach (JObject command in array)
                {
                    Command c = new Command(command);
                    commands.Add(c);
                }
            }
            else
            {
                await LogErrorAsync("Could not parse response data.").ConfigureAwait(false);
            }
            return commands;
        }

        /// <summary>
        /// Send a command to an object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="command"></param>
        /// <param name="values"></param>
        public void SendCommand(Guid id, string command, IEnumerable<object> values = null)
        {
            SendCommandAsync(id, command, values).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Send a command to an object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="command"></param>
        /// <param name="values"></param>
        public async Task SendCommandAsync(Guid id, string command, IEnumerable<object> values = null)
        {
            if (values == null)
            {
                await SendCommandRequestAsync(id, command, new string[0]).ConfigureAwait(false);
            }
            else
            {
                await SendCommandRequestAsync(id, command, values).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a command to an object asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="command"></param>
        /// <param name="json">The command body</param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task SendCommandRequestAsync(Guid id, string command, IEnumerable<object> values)
        {
            var response = await Client.Request(new Url("objects")
                .AppendPathSegments(id, "commands", command))
                .PutJsonAsync(values)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all network devices.
        /// </summary>
        /// <param name="type">Optional type number as a string</param>
        public IEnumerable<MetasysObject> GetNetworkDevices(string type = null)
        {
            return GetNetworkDevicesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets all network devices asynchronously by requesting each available page.
        /// </summary>
        /// <param name="type">Optional type number as a string</param>
        /// <exception cref="System.NullReferenceException"></exception>
        public async Task<IEnumerable<MetasysObject>> GetNetworkDevicesAsync(string type = null)
        {
            List<MetasysObject> devices = new List<MetasysObject>() { };
            bool hasNext = true;
            int page = 1;

            while (hasNext)
            {
                hasNext = false;
                var response = await GetNetworkDevicesRequestAsync(type, page).ConfigureAwait(false);
                try
                {
                    var list = response["items"] as JArray;
                    foreach (var item in list)
                    {
                        var typeInfo = await GetType(item).ConfigureAwait(false);
                        string description = typeInfo.Description;
                        MetasysObject device = new MetasysObject(item, description);
                        devices.Add(device);
                    }

                    if (!(response["next"] == null || response["next"].Type == JTokenType.Null))
                    {
                        hasNext = true;
                        page++;
                    }
                }
                catch (System.NullReferenceException)
                {
                    await LogErrorAsync("Could not format response.").ConfigureAwait(false);
                }
            }

            return devices;
        }

        /// <summary>
        /// Gets all network devices asynchronously.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="page"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task<JToken> GetNetworkDevicesRequestAsync(string type = null, int page = 1)
        {
            Url url = new Url("networkDevices");
            url.SetQueryParam("page", page);
            if (type != null)
            {
                url.SetQueryParam("type", type);
            }

            var response = await Client.Request(url)
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            return response;
        }

        /// <summary>
        /// Gets all available network device types in (id, description) pairs asynchronously.
        /// </summary>
        public IEnumerable<(int Id, string Description)> GetNetworkDeviceTypes()
        {
            return GetNetworkDeviceTypesAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets all available network device types in (id, description) pairs asynchronously.
        /// </summary>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        /// <exception cref="System.NullReferenceException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<IEnumerable<(int Id, string Description)>> GetNetworkDeviceTypesAsync()
        {
            List<(int Id, string Description)> types = new List<(int Id, string Description)>() { };
            var response = await Client.Request(new Url("networkDevices")
                .AppendPathSegment("availableTypes"))
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            try
            {
                var list = response["items"] as JArray;
                foreach (var item in list)
                {
                    try
                    {
                        var type = await GetType(item).ConfigureAwait(false);
                        if (type.Id != -1)
                        {
                            types.Add(type);
                        }
                    }
                    catch (System.ArgumentNullException)
                    {
                        await LogErrorAsync("Could not format response.").ConfigureAwait(false);
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                await LogErrorAsync("Could not format response.").ConfigureAwait(false);
            }

            return types;
        }

        /// <summary>
        /// Gets the type from a token with a typeUrl by requesting the actual description asynchronously.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="System.NullReferenceException"></exception>        
        /// <exception cref="System.ArgumentNullException"></exception>
        private async Task<(int Id, string Description)> GetType(JToken item)
        {
            try
            {
                var url = item["typeUrl"].Value<string>();
                var typeToken = await GetWithFullUrl(url).ConfigureAwait(false);

                string description = typeToken["description"].Value<string>();
                int type = typeToken["id"].Value<int>();
                return (Id: type, Description: description);
            }
            catch (System.NullReferenceException)
            {
                var task = LogErrorAsync("Could not get type enumeration.");
                return (Id: -1, Description: "");
            }
            catch (System.ArgumentNullException)
            {
                var task = LogErrorAsync("Could not get type enumeration. Token missing required field.");
                return (Id: -1, Description: "");
            }
        }

        /// <summary>
        /// Creates a new Flurl client and gets a resource given the url asynchronously.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task<JToken> GetWithFullUrl(string url)
        {
            using (var temporaryClient = new FlurlClient(new Url(url)))
            {
                temporaryClient.Headers.Add("Authorization", this.AccessToken);
                var item = await temporaryClient.Request()
                    .GetJsonAsync<JToken>()
                    .ConfigureAwait(false);
                return item;
            }
        }

        /// <summary>
        /// Gets all child objects given a parent Guid.
        /// Level indicates how deep to retrieve objects with level of 1 only retrieves immediate children of the parent object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="levels">The depth of the children to retrieve</param>
        public IEnumerable<MetasysObject> GetObjects(Guid id, int levels = 1)
        {
            return GetObjectsAsync(id, levels).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets all child objects recursively given a parent Guid asynchronously by requesting each available page.
        /// Level indicates how deep to retrieve objects with level of 1 only retrieves immediate children of the parent object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="levels">The depth of the children to retrieve</param>
        /// <exception cref="System.NullReferenceException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<IEnumerable<MetasysObject>> GetObjectsAsync(Guid id, int levels = 1)
        {
            if (levels < 1)
            {
                return null;
            }

            List<MetasysObject> objects = new List<MetasysObject>() { };
            bool hasNext = true;
            int page = 1;

            while (hasNext)
            {
                hasNext = false;
                var response = await GetObjectsRequestAsync(id, page).ConfigureAwait(false);

                try
                {
                    var total = response["total"].Value<int>();
                    if (total > 0)
                    {
                        var list = response["items"] as JArray;

                        foreach (var item in list)
                        {
                            var itemInfo = await GetType(item).ConfigureAwait(false);
                            string description = itemInfo.Description;

                            if (levels - 1 > 0)
                            {
                                try
                                {
                                    var str = item["id"].Value<string>();
                                    var objId = new Guid(str);
                                    var children = await GetObjectsAsync(objId, levels - 1).ConfigureAwait(false);
                                    MetasysObject obj = new MetasysObject(item, description, children);
                                    objects.Add(obj);
                                }
                                catch (System.ArgumentNullException)
                                {
                                    MetasysObject obj = new MetasysObject(item, description);
                                    objects.Add(obj);
                                }
                            }
                            else
                            {
                                MetasysObject obj = new MetasysObject(item, description);
                                objects.Add(obj);
                            }
                        }

                        if (!(response["next"] == null || response["next"].Type == JTokenType.Null))
                        {
                            hasNext = true;
                            page++;
                        }
                    }
                }
                catch (System.NullReferenceException)
                {
                    await LogErrorAsync("Could not format response.").ConfigureAwait(false);
                }
            }

            return objects;
        }

        /// <summary>
        /// Gets all child objects given a parent Guid asynchronously with the given page number.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <exception cref="Flurl.Http.FlurlHttpException"></exception>
        private async Task<JToken> GetObjectsRequestAsync(Guid id, int page = 1)
        {
            Url url = new Url("objects")
                .AppendPathSegments(id, "objects")
                .SetQueryParam("page", page);

            var response = await Client.Request(url)
                .GetJsonAsync<JToken>()
                .ConfigureAwait(false);

            return response;
        }
    }
}
