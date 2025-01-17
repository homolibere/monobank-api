﻿using Monobank.Core.Extensions;
using Monobank.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monobank.Core.Services
{
    public class ClientService
    {
        private const string ClientInfoEndpoint = "personal/client-info";
        private const string StatementEndpoint = "personal/statement";
        private const string WebhookEndpoint = "personal/webhook";
        private const string TokenHeader = "X-Token";
        private readonly HttpClient _httpClient;
        private DateTime _previousRequest = DateTime.Now.AddMinutes(-2);

        public ClientService(HttpClient client, string token)
        {
            _httpClient = client;
            _httpClient.DefaultRequestHeaders.Add(TokenHeader, token);
        }

        public async Task<UserInfo> GetClientInfo()
        {
            var uri = new Uri(ClientInfoEndpoint, UriKind.Relative);
            var response = await _httpClient.GetAsync(uri);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<Error>(responseString);
                throw new Exception(error.Description);
            }
            return JsonSerializer.Deserialize<UserInfo>(responseString);
        }

        public async Task<ICollection<Statement>> GetStatements(DateTime from, DateTime to, string account = "0")
        {
            if (to.ToUnixTime() - from.ToUnixTime() >= 2682000)
            {
                throw new Exception("Time range exceeds allowed. Difference between from and to should less then 31 day + 1 hour");
            }

            if ((DateTime.Now - _previousRequest).TotalSeconds <= 60)
            {
                throw new Exception("Too many requests. Only 1 request per 60 seconds");
            }

            var uri = new Uri($"{StatementEndpoint}/{account}/{from.ToUnixTime()}/{to.ToUnixTime()}", UriKind.Relative);
            var response = await _httpClient.GetAsync(uri);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<Error>(responseString);
                throw new Exception(error.Description);
            }
            _previousRequest = DateTime.Now;
            return JsonSerializer.Deserialize<ICollection<Statement>>(responseString);
        }

        public async Task<bool> SetWebhook(string url)
        {
            // create body containing webhook url
            var body = JsonSerializer.Serialize(new {webHookUrl = url});
            // uri to call
            var uri = new Uri(WebhookEndpoint, UriKind.Relative);
            // set webhook
            var response = await _httpClient.PostAsync(uri, new StringContent(body));

            return response.IsSuccessStatusCode;
        }
    }
}
