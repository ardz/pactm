using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Tests.ApiTestClient
{
    public class Client
    {
        private HttpClient _client;
        public string BaseUri;

        public Client(string baseUri = null)
        {
            BaseUri = baseUri ?? "http://localhost:1234";
        }

        /// <summary>
        /// Make a get request and simply return the
        /// response as a string. The expected HttpStatusCode
        /// must also be provided.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="expectedCode"></param>
        /// <returns></returns>
        public string Get(string endpoint, HttpStatusCode expectedCode)
        {
            var result = MakeRequest(HttpMethod.Get, endpoint);

            Assert.True(result.StatusCode == expectedCode);

            var reader = new StreamReader(result.Content.ReadAsStreamAsync().Result);
            var text = reader.ReadToEnd();

            result.Dispose();

            return text;
        }

        /// <summary>
        /// Make a put request and simply return the
        /// response as a string. The expected HttpStatusCode
        /// must also be provided.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="expectedCode"></param>
        /// <returns></returns>

        public string Put(string endpoint, HttpStatusCode expectedCode)
        {
            var result = MakeRequest(HttpMethod.Put, endpoint, new List<string> { "Content-Type", "application/json" });

            Assert.True(result.StatusCode == expectedCode);

            var reader = new StreamReader(result.Content.ReadAsStreamAsync().Result);
            var text = reader.ReadToEnd();

            result.Dispose();

            return text;
        }

        /// <summary>
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public HttpResponseMessage MakeRequest(HttpMethod method, string endpoint, IReadOnlyList<string> headers = null)
        {
            using (_client = new HttpClient { BaseAddress = new Uri(BaseUri) })
            {
                var request = new HttpRequestMessage(method, BaseUri + endpoint);

                if (headers == null)
                {
                    request.Headers.Add("Accept", "application/json");
                }
                else
                {
                    request.Headers.Add(headers[0], headers[1]);
                }

                var response = _client.SendAsync(request);

                return response.Result;
            }
        }
    }
}