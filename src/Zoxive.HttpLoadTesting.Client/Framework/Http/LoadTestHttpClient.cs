﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Zoxive.HttpLoadTesting.Framework.Core;
using Zoxive.HttpLoadTesting.Framework.Model;

namespace Zoxive.HttpLoadTesting.Framework.Http
{
    public class LoadTestHttpClient : ILoadTestHttpClient
    {
        internal readonly IHttpUser HttpUser;
        private readonly CancellationToken _token;

        public LoadTestHttpClient(IHttpUser httpUser, CancellationToken token)
        {
            HttpUser = httpUser;
            _token = token;
            HttpClient = CreateHttpClient();

            TestState = new Dictionary<string, object>();
        }

        private HttpClient HttpClient { get; }

        private HttpClient CreateHttpClient()
        {
            var handler = HttpUser.CreateHttpMessageHandler?.Invoke() ?? new HttpClientHandler();

            var client = new HttpClient(handler)
            {
                Timeout = new TimeSpan(0, 1, 0)
            };

            HttpUser.AlterHttpClient?.Invoke(client);

            return client;
        }

        public IDictionary<string, object> TestState { get; }

        public Task<HttpResponseMessage> Post(string relativePath, HttpContent content, Action<HttpRequestMessage> alterHttpRequestMessagePerRequest = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(relativePath))
            {
                Content = content
            };
            return SendAsync(request, alterHttpRequestMessagePerRequest);
        }

        public Task<HttpResponseMessage> Put(string relativePath, HttpContent content, Action<HttpRequestMessage> alterHttpRequestMessagePerRequest = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, GetUrl(relativePath))
            {
                Content = content
            };
            return SendAsync(request, alterHttpRequestMessagePerRequest);
        }

        public Task<HttpResponseMessage> Get(string relativePath, Action<HttpRequestMessage> alterHttpRequestMessagePerRequest = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(relativePath));

            return SendAsync(request, alterHttpRequestMessagePerRequest);
        }

        public Task<HttpResponseMessage> Delete(string relativePath, Action<HttpRequestMessage> alterHttpRequestMessagePerRequest = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, GetUrl(relativePath));

            return SendAsync(request, alterHttpRequestMessagePerRequest);
        }

        private Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<HttpRequestMessage> alterHttpRequestMessagePerRequest = null)
        {
            HttpUser.AlterHttpRequestMessage?.Invoke(request);

            alterHttpRequestMessagePerRequest?.Invoke(request);

            return HttpClient.SendAsync(request, cancellationToken: _token);
        }

        public IUserLoadTestHttpClient GetClientForUser()
        {
            return new UserLoadTestHttpClient(this, TestState);
        }

        private Uri GetUrl(string relativePath)
        {
            return new Uri(HttpUser.BaseUrl + relativePath);
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}
